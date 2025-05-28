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
        private readonly Timer _healthCheckTimer;
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

            // Timer để kiểm tra sức khỏe của streams
            _healthCheckTimer = new Timer(PerformHealthCheck, null,
                TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        }

        public async Task<(bool Success, string? Url)> StartStreamAsync(
            Guid studentId,
            Stream inputStream,
            CancellationToken cancellationToken)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(HlsStreamManager));

            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                // Cleanup existing stream first
                await StopStreamAsync(studentId);

                var streamPath = Path.Combine(_baseStreamPath, studentId.ToString());

                // Tạo thư mục mới và dọn dẹp nếu có
                if (Directory.Exists(streamPath))
                {
                    try
                    {
                        Directory.Delete(streamPath, true);
                        await Task.Delay(500, cancellationToken); // Chờ OS cleanup
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not clean existing directory for student {StudentId}", studentId);
                    }
                }

                Directory.CreateDirectory(streamPath);

                // Tạo buffer stream để tránh input stream bị đóng sớm
                var bufferedStream = await BufferInputStreamAsync(inputStream, cancellationToken);

                var (process, streamUrl) = await _ffmpegService.StartHlsProcess(
                    studentId,
                    bufferedStream,
                    cancellationToken);

                // Verify stream creation với timeout và retry logic
                var streamReady = await VerifyStreamCreationAsync(streamPath, cancellationToken);

                if (!streamReady)
                {
                    _logger.LogWarning("Stream verification failed for student {StudentId}", studentId);
                    await SafeKillProcess(process);
                    bufferedStream?.Dispose();
                    return (false, null);
                }

                var streamInfo = new StreamInfo(
                    Process: process,
                    StartTime: DateTime.UtcNow,
                    StreamUrl: streamUrl,
                    OutputDirectory: streamPath,
                    InputBuffer: bufferedStream
                );

                if (_activeStreams.TryAdd(studentId, streamInfo))
                {
                    // Start monitoring process
                    _ = Task.Run(() => MonitorProcessAsync(studentId, process, cancellationToken));
                    _logger.LogInformation("Stream started successfully for student {StudentId}", studentId);
                    return (true, GetPublicStreamUrl(streamUrl));
                }

                await SafeKillProcess(process);
                bufferedStream?.Dispose();
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

        private async Task<MemoryStream> BufferInputStreamAsync(Stream inputStream, CancellationToken cancellationToken)
        {
            var bufferStream = new MemoryStream();

            try
            {
                // Copy với buffer lớn để tối ưu performance
                var buffer = new byte[65536]; // 64KB buffer
                int totalBytes = 0;
                int bytesRead;

                while ((bytesRead = await inputStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                {
                    await bufferStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                    totalBytes += bytesRead;
                }

                bufferStream.Position = 0;
                _logger.LogInformation("Input stream buffered successfully. Total size: {Size} bytes", totalBytes);

                return bufferStream;
            }
            catch (Exception ex)
            {
                bufferStream?.Dispose();
                _logger.LogError(ex, "Failed to buffer input stream");
                throw;
            }
        }

        private async Task<bool> VerifyStreamCreationAsync(string streamPath, CancellationToken cancellationToken)
        {
            var m3u8Path = Path.Combine(streamPath, "stream.m3u8");
            var timeout = DateTime.UtcNow.AddSeconds(15);
            var retryCount = 0;
            const int maxRetries = 30;

            while (DateTime.UtcNow < timeout && retryCount < maxRetries && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (File.Exists(m3u8Path))
                    {
                        var fileInfo = new FileInfo(m3u8Path);
                        if (fileInfo.Length > 0)
                        {
                            var content = await File.ReadAllTextAsync(m3u8Path, cancellationToken);
                            if (content.Contains("#EXTM3U") && content.Contains("#EXT-X-VERSION"))
                            {
                                _logger.LogInformation("Stream verification successful: {Path}", m3u8Path);
                                return true;
                            }
                        }
                    }
                }
                catch (IOException ex)
                {
                    _logger.LogDebug("File locked during verification (retry {Retry}): {Error}", retryCount, ex.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error during stream verification (retry {Retry})", retryCount);
                }

                retryCount++;
                await Task.Delay(500, cancellationToken);
            }

            _logger.LogWarning("Stream verification timeout or failed after {Retries} retries", retryCount);
            return false;
        }

        private string GetPublicStreamUrl(string streamUrl)
        {
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
                _ = Task.Run(() => StopStreamAsync(studentId));
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

                // Dispose input buffer
                streamInfo.InputBuffer?.Dispose();

                // Cleanup files với retry logic
                await Task.Delay(2000); // Chờ process hoàn toàn dừng

                if (Directory.Exists(streamInfo.OutputDirectory))
                {
                    var retryCount = 0;
                    var maxRetries = 3;

                    while (retryCount < maxRetries)
                    {
                        try
                        {
                            Directory.Delete(streamInfo.OutputDirectory, true);
                            _logger.LogInformation("Cleaned up stream directory for student {StudentId}: {Directory}",
                                studentId, streamInfo.OutputDirectory);
                            break;
                        }
                        catch (Exception ex)
                        {
                            retryCount++;
                            _logger.LogWarning(ex, "Failed to delete stream folder for student {StudentId} (attempt {Retry}/{MaxRetries}): {Directory}",
                                studentId, retryCount, maxRetries, streamInfo.OutputDirectory);

                            if (retryCount < maxRetries)
                            {
                                await Task.Delay(1000 * retryCount); // Exponential backoff
                            }
                        }
                    }
                }
            }
        }

        public string? GetStreamUrl(Guid studentId)
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
                _ = Task.Run(() => StopStreamAsync(studentId));
            }
            return null;
        }

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
            if (_activeStreams.ContainsKey(studentId))
            {
                _logger.LogInformation("Stopping stream for student {StudentId}", studentId);
                await CleanupStreamAsync(studentId);
                return true;
            }

            _logger.LogDebug("No active stream to stop for student {StudentId}", studentId);
            return false;
        }

        private static async Task SafeKillProcess(Process process)
        {
            if (process == null) return;

            try
            {
                if (!process.HasExited)
                {
                    // Thử đóng stdin trước
                    try
                    {
                        process.StandardInput?.Close();

                        // Chờ process tự kết thúc
                        var waitTask = Task.Run(() => process.WaitForExit(3000));
                        var timeoutTask = Task.Delay(3000);

                        var completedTask = await Task.WhenAny(waitTask, timeoutTask);

                        if (completedTask == timeoutTask && !process.HasExited)
                        {
                            process.Kill(true);
                            await Task.Delay(1000);
                        }
                    }
                    catch
                    {
                        // Force kill if graceful shutdown fails
                        try
                        {
                            process.Kill(true);
                            await Task.Delay(1000);
                        }
                        catch { }
                    }
                }
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

        private void PerformHealthCheck(object? state)
        {
            if (_disposed) return;

            var deadStreams = new List<Guid>();

            foreach (var kvp in _activeStreams)
            {
                var studentId = kvp.Key;
                var streamInfo = kvp.Value;

                try
                {
                    // Kiểm tra process đã chết
                    if (streamInfo.Process.HasExited)
                    {
                        deadStreams.Add(studentId);
                        continue;
                    }

                    // Kiểm tra file m3u8 có tồn tại và valid
                    var m3u8Path = Path.Combine(streamInfo.OutputDirectory, "stream.m3u8");
                    if (!File.Exists(m3u8Path))
                    {
                        deadStreams.Add(studentId);
                        continue;
                    }

                    // Kiểm tra stream quá cũ (hơn 2 giờ)
                    if (DateTime.UtcNow - streamInfo.StartTime > TimeSpan.FromHours(2))
                    {
                        _logger.LogInformation("Stopping old stream for student {StudentId}", studentId);
                        deadStreams.Add(studentId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error during health check for student {StudentId}", studentId);
                    deadStreams.Add(studentId);
                }
            }

            // Cleanup dead streams
            foreach (var studentId in deadStreams)
            {
                _ = Task.Run(() => StopStreamAsync(studentId));
            }

            if (deadStreams.Count > 0)
            {
                _logger.LogInformation("Health check completed. Cleaned up {Count} dead streams", deadStreams.Count);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            _healthCheckTimer?.Dispose();

            // Stop all active streams
            var stopTasks = _activeStreams.Keys.Select(StopStreamAsync);
            Task.WaitAll(stopTasks.ToArray(), TimeSpan.FromSeconds(10));

            _semaphore?.Dispose();
        }

        private record StreamInfo(Process Process, DateTime StartTime, string StreamUrl, string OutputDirectory, MemoryStream? InputBuffer);

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