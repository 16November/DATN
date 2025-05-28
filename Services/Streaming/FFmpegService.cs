using System.Diagnostics;
using System.Text;

namespace DoAnTotNghiep.Services.Streaming
{
    public class FFmpegService
    {
        private readonly ILogger<FFmpegService> _logger;
        private readonly string _ffmpegPath;
        private readonly int _initTimeout = 15000; // 10 giây thay vì 30

        public FFmpegService(ILogger<FFmpegService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _ffmpegPath = configuration["FFmpeg:Path"] ?? "ffmpeg"; // Đường dẫn tới FFmpeg, mặc định là "ffmpeg"
        }

        public async Task<(Process Process, string StreamUrl)> StartHlsProcess(
            Guid studentId,
            Stream inputStream,
            CancellationToken cancellationToken)
        {
            var outputDirectory = Path.Combine(AppContext.BaseDirectory, "HLSStreams", studentId.ToString());
            EnsureDirectoryExists(outputDirectory);

            var processStartInfo = CreateProcessStartInfo(outputDirectory);
            var process = new Process { StartInfo = processStartInfo };
            var streamReady = new TaskCompletionSource<bool>();

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    _logger.LogInformation("FFmpeg: {Data}", e.Data);
                    if (e.Data.Contains("segment") || e.Data.Contains("Opening"))
                    {
                        streamReady.TrySetResult(true);
                    }
                }
            };

            try
            {
                process.Start();
                process.BeginErrorReadLine();

                // Copy input stream to FFmpeg
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var ffmpegInput = process.StandardInput.BaseStream;
                        await inputStream.CopyToAsync(ffmpegInput, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error copying stream data");
                        streamReady.TrySetException(ex);
                    }
                }, cancellationToken);

                // Wait for stream initialization with timeout
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(_initTimeout);

                try
                {
                    await streamReady.Task.WaitAsync(timeoutCts.Token);

                    var streamUrl = $"/streams/{studentId}/stream.m3u8";
                    _logger.LogInformation("Stream started successfully for {StudentId}", studentId);

                    return (process, streamUrl);
                }
                catch (OperationCanceledException)
                {
                    throw new TimeoutException($"Stream initialization timeout after {_initTimeout}ms");
                }
            }
            catch
            {
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
                process.StandardInput.Close();
                await Task.WhenAny(
                    Task.Run(() => process.WaitForExit(3000)),
                    Task.Delay(3000)
                );
                if (!process.HasExited)
                {
                    process.Kill(true);
                }
            }
            finally
            {
                process.Dispose();
            }
        }

        private static string BuildFFmpegArguments(string outputDirectory)
        {
            return "-re " +
                   "-f webm -fflags +discardcorrupt -i pipe:0 " +
                   "-c:v libx264 -preset ultrafast -tune zerolatency " +
                   "-c:a aac -b:a 128k -strict -2 " +
                   "-f hls -hls_time 2 -hls_list_size 5 -hls_flags delete_segments " +
                   "-hls_segment_type mpegts " +
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