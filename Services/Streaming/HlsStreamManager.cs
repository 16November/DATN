using System.Collections.Concurrent;
using System.Diagnostics;

namespace DoAnTotNghiep.Services.Streaming
{
    public class HlsStreamManager : IDisposable
    {
        private readonly ConcurrentDictionary<Guid, StreamInfo> _activeStreams = new();
        private readonly FFmpegService _ffmpegService;
        private readonly ILogger<HlsStreamManager> _logger;
        private readonly string _baseStreamPath;
        private readonly SemaphoreSlim _semaphore;
        private bool _disposed = false;

        public HlsStreamManager(
            FFmpegService ffmpegService,
            ILogger<HlsStreamManager> logger,
            IConfiguration configuration)
        {
            _ffmpegService = ffmpegService;
            _logger = logger;
            _baseStreamPath = configuration["Streaming:BasePath"] ?? Path.Combine(AppContext.BaseDirectory, "streams");
            Directory.CreateDirectory(_baseStreamPath);
            _semaphore = new SemaphoreSlim(10); // Giới hạn 10 stream đồng thời
        }

        public async Task<(bool Success, string? Url)> StartStreamAsync(
            Guid studentId,
            Stream inputStream,
            CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                // Cleanup existing stream
                await StopStreamAsync(studentId);

                var streamPath = Path.Combine(_baseStreamPath, studentId.ToString());
                Directory.CreateDirectory(streamPath);

                var (process, streamUrl) = await _ffmpegService.StartHlsProcess(
                    studentId,
                    inputStream,
                    cancellationToken);

                // Verify stream creation with better checks
                var m3u8Path = Path.Combine(streamPath, "stream.m3u8");
                var timeout = DateTime.UtcNow.AddSeconds(10);
                var streamReady = false;

                while (DateTime.UtcNow < timeout && !streamReady)
                {
                    if (File.Exists(m3u8Path))
                    {
                        try
                        {
                            // Verify m3u8 content
                            var content = await File.ReadAllTextAsync(m3u8Path, cancellationToken);
                            if (content.Contains("#EXTM3U") && content.Contains(".ts"))
                            {
                                streamReady = true;
                            }
                        }
                        catch (IOException)
                        {
                            // File might be locked by FFmpeg
                            await Task.Delay(100, cancellationToken);
                            continue;
                        }
                    }
                    await Task.Delay(500, cancellationToken);
                }

                if (!streamReady)
                {
                    _logger.LogWarning("Stream initialization timeout for student {StudentId}", studentId);
                    await StopStreamAsync(studentId);
                    return (false, null);
                }

                var streamInfo = new StreamInfo(
                    Process: process,
                    StartTime: DateTime.UtcNow,
                    StreamUrl: streamUrl,
                    OutputDirectory: streamPath
                );

                if (_activeStreams.TryAdd(studentId, streamInfo))
                {
                    // Start monitoring process
                    _ = MonitorProcessAsync(studentId, process, cancellationToken);
                    _logger.LogInformation("Stream started successfully for student {StudentId}", studentId);
                    return (true, GetPublicStreamUrl(streamUrl));
                }

                await StopStreamAsync(studentId);
                return (false, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start stream for student {StudentId}", studentId);
                return (false, null);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private string GetPublicStreamUrl(string streamUrl)
        {
            // Normalize URL format
            return streamUrl.Replace('\\', '/').TrimStart('/');
        }

        public string? GetStreamPath(Guid studentId)
        {
            if (_activeStreams.TryGetValue(studentId, out var streamInfo))
            {
                var m3u8Path = Path.Combine(_baseStreamPath, studentId.ToString(), "stream.m3u8");
                if (File.Exists(m3u8Path) && !streamInfo.Process.HasExited)
                {
                    return m3u8Path;
                }
                // Cleanup if stream is dead
                _ = StopStreamAsync(studentId);
            }
            return null;
        }

        private async Task MonitorProcessAsync(Guid studentId, Process process, CancellationToken cancellationToken)
        {
            try
            {
                await Task.Run(() => process.WaitForExit(), cancellationToken);
                _logger.LogInformation("FFmpeg process exited for student {StudentId}", studentId);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Process monitoring cancelled for student {StudentId}", studentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FFmpeg process error for student {StudentId}", studentId);
            }
            finally
            {
                await CleanupStreamAsync(studentId);
            }
        }

        private async Task CleanupStreamAsync(Guid studentId)
        {
            if (_activeStreams.TryRemove(studentId, out var streamInfo))
            {
                await SafeKillProcess(streamInfo.Process);

                // Cleanup files với delay để đảm bảo process đã hoàn toàn dừng
                await Task.Delay(1000);

                if (Directory.Exists(streamInfo.OutputDirectory))
                {
                    try
                    {
                        await Task.Run(() => Directory.Delete(streamInfo.OutputDirectory, true));
                        _logger.LogInformation("Cleaned up stream directory for student {StudentId}: {Directory}",
                            studentId, streamInfo.OutputDirectory);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete stream folder for student {StudentId}: {Directory}",
                            studentId, streamInfo.OutputDirectory);
                    }
                }
            }
        }

        public string GetStreamUrl(Guid studentId)
        {
            if (_activeStreams.TryGetValue(studentId, out var streamInfo))
            {
                if (!streamInfo.Process.HasExited)
                {
                    var m3u8Path = Path.Combine(streamInfo.OutputDirectory, "stream.m3u8");
                    if (File.Exists(m3u8Path) && new FileInfo(m3u8Path).Length > 0)
                    {
                        return GetPublicStreamUrl(streamInfo.StreamUrl);
                    }
                }
                // Cleanup dead stream
                _ = StopStreamAsync(studentId);
            }
            return null;
        }

        // Thêm method để check stream status chi tiết hơn
        public StreamStatus GetStreamStatus(Guid studentId)
        {
            if (!_activeStreams.TryGetValue(studentId, out var streamInfo))
            {
                return new StreamStatus
                {
                    IsActive = false,
                    HasPlaylist = false,
                    ProcessRunning = false,
                    StartTime = null,
                    FilePath = null
                };
            }

            var m3u8Path = Path.Combine(streamInfo.OutputDirectory, "stream.m3u8");
            var hasPlaylist = File.Exists(m3u8Path) && new FileInfo(m3u8Path).Length > 0;
            var processRunning = !streamInfo.Process.HasExited;

            return new StreamStatus
            {
                IsActive = true,
                HasPlaylist = hasPlaylist,
                ProcessRunning = processRunning,
                StartTime = streamInfo.StartTime,
                FilePath = hasPlaylist ? m3u8Path : null
            };
        }

        public async Task<bool> StopStreamAsync(Guid studentId)
        {
            if (_activeStreams.TryGetValue(studentId, out var streamInfo))
            {
                _logger.LogInformation("Stopping stream for student {StudentId}", studentId);
                await SafeKillProcess(streamInfo.Process);
                await CleanupStreamAsync(studentId);
                return true;
            }

            _logger.LogDebug("No active stream to stop for student {StudentId}", studentId);
            return false;
        }

        private static async Task SafeKillProcess(Process process)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                    await Task.Delay(1000); // Chờ process kết thúc
                }
            }
            catch (Exception)
            {
                // Ignore kill errors
            }
            finally
            {
                try
                {
                    process.Dispose();
                }
                catch { }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            // Stop all active streams
            var tasks = _activeStreams.Keys.Select(StopStreamAsync);
            Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(10));

            _semaphore?.Dispose();
        }

        private record StreamInfo(Process Process, DateTime StartTime, string StreamUrl, string OutputDirectory);

        public class StreamStatus
        {
            public bool IsActive { get; set; }
            public bool HasPlaylist { get; set; }
            public bool ProcessRunning { get; set; }
            public DateTime? StartTime { get; set; }
            public string? FilePath { get; set; }
        }
    }
}