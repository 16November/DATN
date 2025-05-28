using System.Diagnostics;
using System.Text;

namespace DoAnTotNghiep.Services.Streaming
{
    public class FFmpegService
    {
        private readonly ILogger<FFmpegService> _logger;
        private readonly string _ffmpegPath;
        private readonly int _initTimeout = 30000; // Tăng timeout lên 30s

        public FFmpegService(ILogger<FFmpegService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _ffmpegPath = configuration["FFmpeg:Path"] ?? "ffmpeg";
        }

        public async Task<(Process Process, string StreamUrl)> StartHlsProcess(Guid studentId, Stream inputStream, CancellationToken cancellationToken)
        {
            var outputDirectory = Path.Combine(AppContext.BaseDirectory, "HLSStreams", studentId.ToString());
            EnsureDirectoryExists(outputDirectory);

            var processStartInfo = CreateProcessStartInfo(outputDirectory);
            var process = new Process { StartInfo = processStartInfo };
            var streamReady = new TaskCompletionSource<bool>();
            var initializationTimeout = TimeSpan.FromSeconds(30);

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    _logger.LogInformation("FFmpeg: {Data}", e.Data);
                    if (e.Data.Contains("Opening") || e.Data.Contains("segment") || e.Data.Contains("muxer") || e.Data.Contains("hls"))
                    {
                        streamReady.TrySetResult(true);
                    }
                    if (e.Data.Contains("error") || e.Data.Contains("failed"))
                    {
                        _logger.LogError("FFmpeg error detected: {Error}", e.Data);
                        streamReady.TrySetException(new InvalidOperationException($"FFmpeg error: {e.Data}"));
                    }
                }
            };

            try
            {
                process.Start();
                process.BeginErrorReadLine();

                var copyTask = Task.Run(async () =>
                {
                    try
                    {
                        using var ffmpegInput = process.StandardInput.BaseStream;
                        var buffer = new byte[65536];
                        int bytesRead;
                        while ((bytesRead = await inputStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                        {
                            await ffmpegInput.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                            await ffmpegInput.FlushAsync(cancellationToken);
                        }
                        ffmpegInput.Close();
                        _logger.LogInformation("Finished copying stream data to FFmpeg for student {StudentId}", studentId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error copying stream data for student {StudentId}", studentId);
                        streamReady.TrySetException(ex);
                    }
                }, cancellationToken);

                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(initializationTimeout);

                await streamReady.Task.WaitAsync(timeoutCts.Token);

                var streamUrl = $"/streams/{studentId}/stream.m3u8";
                _logger.LogInformation("Stream started successfully for student {StudentId}", studentId);
                return (process, streamUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start FFmpeg process for student {StudentId}", studentId);
                await StopProcessAsync(process);
                throw;
            }
        }


        private ProcessStartInfo CreateProcessStartInfo(string outputDirectory)
        {
            return new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = BuildFFmpegArguments(outputDirectory),
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
        }

        private async Task StopProcessAsync(Process process)
        {
            if (process == null || process.HasExited) return;

            try
            {
                // Đóng stdin trước để FFmpeg có thể kết thúc gracefully
                try
                {
                    process.StandardInput?.Close();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error closing FFmpeg stdin");
                }

                // Chờ process kết thúc một cách tự nhiên
                var waitTask = Task.Run(() => process.WaitForExit(5000));
                var timeoutTask = Task.Delay(5000);

                var completedTask = await Task.WhenAny(waitTask, timeoutTask);

                if (completedTask == timeoutTask && !process.HasExited)
                {
                    _logger.LogWarning("FFmpeg process did not exit gracefully, forcing kill");
                    process.Kill(true);

                    // Chờ thêm một chút sau khi kill
                    await Task.Delay(1000);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping FFmpeg process");
            }
            finally
            {
                try
                {
                    process.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disposing FFmpeg process");
                }
            }
        }

        private static string BuildFFmpegArguments(string outputDirectory)
        {
            // Tối ưu tham số FFmpeg cho streaming realtime
            return "-re " +
                   "-f webm -fflags +genpts+discardcorrupt -i pipe:0 " +
                   "-c:v libx264 -preset ultrafast -tune zerolatency " +
                   "-profile:v baseline -level 3.0 " +
                   "-pix_fmt yuv420p " +
                   "-c:a aac -b:a 128k -ar 44100 -ac 2 " +
                   "-f hls -hls_time 2 -hls_list_size 5 " +
                   "-hls_flags delete_segments+round_durations+split_by_time " +
                   "-hls_segment_type mpegts " +
                   "-hls_allow_cache 0 " +
                   $"-hls_segment_filename \"{Path.Combine(outputDirectory, "segment_%03d.ts")}\" " +
                   $"\"{Path.Combine(outputDirectory, "stream.m3u8")}\"";
        }

        private void EnsureDirectoryExists(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    _logger.LogInformation("Created directory: {Path}", path);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create directory: {Path}", path);
                throw new IOException($"Unable to create directory: {path}", ex);
            }
        }
    }
}