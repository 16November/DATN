using DoAnTotNghiep.Dto.Streaming;
using DoAnTotNghiep.Services.IService;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace DoAnTotNghiep.Services.ServiceImplement
{
    public class TeacherStreamService : ITeacherStreamService
    {
        private readonly IFFmpegService _ffmpegService;
        private readonly ConcurrentDictionary<Guid, StreamSession> _activeStreams = new();
        private readonly SemaphoreSlim _fileAccessSemaphore = new(1, 1); // Để đồng bộ hóa truy cập file

        public TeacherStreamService(IFFmpegService ffmpegService)
        {
            _ffmpegService = ffmpegService;
        }

        public Task<StreamSession> RequestStudentToShareAsync(Guid userId)
        {
            var streamId = Guid.NewGuid();
            var session = new StreamSession
            {
                StreamId = streamId,
                UserId = userId,
                StartTime = DateTime.UtcNow,
                PlaybackUrl = $"/live/{streamId}/playlist.m3u8",
                IsActive = true,
                IsReady = false,
                FFmpegProcessInfo = null
            };
            _activeStreams[streamId] = session;
            Console.WriteLine($"Created stream session {streamId} for user {userId}");
            return Task.FromResult(session);
        }

        public async Task HandleStudentStreamDataAsync(Guid streamId, WebSocket webSocket)
        {
            if (!_activeStreams.TryGetValue(streamId, out var session) || session == null || !session.IsActive)
            {
                Console.WriteLine($"Stream session {streamId} not found or inactive.");
                if (webSocket.State == WebSocketState.Open)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Stream session not found", CancellationToken.None);
                }
                return;
            }

            Console.WriteLine($"Starting stream handling for {streamId}");

            NamedPipeServerStream pipeServer = null;
            FFmpegProcessInfo ffmpegInfo = null;
            CancellationTokenSource mainCts = new CancellationTokenSource();

            try
            {
                // Create output directory
                string outputFolder = Path.Combine("wwwroot", "live", streamId.ToString());
                Directory.CreateDirectory(outputFolder);

                string pipeName = $"stream_{streamId}";
                string playlistPath = Path.Combine(outputFolder, "playlist.m3u8");

                // Create pipe
                Console.WriteLine($"Creating named pipe: {pipeName}");
                pipeServer = new NamedPipeServerStream(
                    pipeName,
                    PipeDirection.Out,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous | PipeOptions.WriteThrough);

                // Start FFmpeg process FIRST
                string pipePath = $@"\\.\pipe\{pipeName}";
                Console.WriteLine($"Starting FFmpeg for stream {streamId}");
                ffmpegInfo = await _ffmpegService.StartFFmpegAsync(pipePath, streamId);
                session.FFmpegProcessInfo = ffmpegInfo;

                // Wait for pipe connection with timeout
                using var pipeTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    mainCts.Token,
                    pipeTimeoutCts.Token);

                try
                {
                    Console.WriteLine($"Waiting for FFmpeg pipe connection for stream {streamId}...");
                    await pipeServer.WaitForConnectionAsync(combinedCts.Token);
                    Console.WriteLine($"FFmpeg pipe connected successfully for stream {streamId}");
                }
                catch (OperationCanceledException) when (pipeTimeoutCts.Token.IsCancellationRequested)
                {
                    Console.WriteLine($"Timeout waiting for FFmpeg pipe connection for stream {streamId}");
                    throw new TimeoutException("FFmpeg connection timeout after 30 seconds");
                }

                // Start WebSocket data forwarding immediately after pipe connection
                var forwardingTask = ForwardWebSocketDataToPipeWithBuffer(webSocket, pipeServer, streamId, mainCts.Token);

                // Monitor playlist creation với improved logic
                var playlistReady = await MonitorPlaylistCreationImproved(playlistPath, streamId, mainCts.Token);
                if (playlistReady)
                {
                    session.IsReady = true;
                    Console.WriteLine($"Stream {streamId} is ready for playback");
                }
                else
                {
                    Console.WriteLine($"Stream {streamId} playlist not ready after timeout");
                }

                // Wait for forwarding to complete
                await forwardingTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in HandleStudentStreamDataAsync for stream {streamId}: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                session.IsReady = false;

                if (webSocket.State == WebSocketState.Open)
                {
                    try
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.InternalServerError,
                            "Internal server error", CancellationToken.None);
                    }
                    catch { }
                }
            }
            finally
            {
                mainCts.Cancel();
                await CleanupStreamResourcesAsync(streamId, session, pipeServer, ffmpegInfo);
            }
        }

        private async Task<bool> MonitorPlaylistCreationImproved(string playlistPath, Guid streamId, CancellationToken cancellationToken)
        {
            var timeout = TimeSpan.FromSeconds(120);
            var checkInterval = 300; // Giảm thời gian giữa các lần kiểm tra
            var minSegments = 1; // Chỉ cần ít nhất 1 đoạn để bắt đầu
            var maxRetries = 3;

            string outputFolder = Path.GetDirectoryName(playlistPath);
            int lastSegmentCount = 0;
            int consecutiveValidChecks = 0;

            // Khởi tạo startTime để theo dõi thời gian bắt đầu giám sát
            var startTime = DateTime.UtcNow;

            Console.WriteLine($"Stream {streamId}: Đang giám sát playlist tại {outputFolder}");

            while (!cancellationToken.IsCancellationRequested && DateTime.UtcNow - startTime < timeout)
            {
                bool fileAccessSuccess = false;

                for (int retry = 0; retry < maxRetries && !fileAccessSuccess; retry++)
                {
                    try
                    {
                        await _fileAccessSemaphore.WaitAsync(cancellationToken);

                        if (File.Exists(playlistPath))
                        {
                            var fileInfo = new FileInfo(playlistPath);
                            var tsFiles = Directory.GetFiles(outputFolder, "*.ts");

                            if (tsFiles.Length != lastSegmentCount)
                            {
                                Console.WriteLine($"Stream {streamId}: Tìm thấy {tsFiles.Length} đoạn, kích thước playlist={fileInfo.Length} bytes");
                                lastSegmentCount = tsFiles.Length;
                            }

                            if (fileInfo.Length > 0)
                            {
                                string playlistContent = await ReadPlaylistContent(playlistPath);

                                if (!string.IsNullOrEmpty(playlistContent))
                                {
                                    bool isValidPlaylist = playlistContent.Contains("#EXTM3U") &&
                                                           playlistContent.Contains("#EXT-X-VERSION") &&
                                                           tsFiles.Length >= minSegments;

                                    if (isValidPlaylist)
                                    {
                                        consecutiveValidChecks++;
                                        Console.WriteLine($"Stream {streamId}: Kiểm tra playlist hợp lệ {consecutiveValidChecks}/2, số đoạn={tsFiles.Length}");

                                        if (consecutiveValidChecks >= 2)
                                        {
                                            return true;
                                        }
                                    }
                                    else
                                    {
                                        consecutiveValidChecks = 0;
                                        Console.WriteLine($"Stream {streamId}: Nội dung playlist không hợp lệ");
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine($"Stream {streamId}: Tệp playlist trống");
                            }
                        }

                        fileAccessSuccess = true;
                    }
                    catch (IOException ioEx) when (retry < maxRetries - 1)
                    {
                        Console.WriteLine($"Stream {streamId}: Lỗi IO (cố gắng {retry + 1}): {ioEx.Message}");
                        await Task.Delay(200 * (retry + 1), cancellationToken);
                    }
                    catch (UnauthorizedAccessException uaEx) when (retry < maxRetries - 1)
                    {
                        Console.WriteLine($"Stream {streamId}: Lỗi quyền truy cập (cố gắng {retry + 1}): {uaEx.Message}");
                        await Task.Delay(200 * (retry + 1), cancellationToken);
                    }
                    finally
                    {
                        _fileAccessSemaphore.Release();
                    }
                }

                await Task.Delay(checkInterval, cancellationToken);
            }

            Console.WriteLine($"Stream {streamId}: Hết thời gian giám sát playlist sau {timeout.TotalSeconds}s");
            return false;
        }


        private async Task<string> ReadPlaylistContent(string playlistPath)
        {
            try
            {
                using var fileStream = new FileStream(playlistPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(fileStream, System.Text.Encoding.UTF8);
                return await reader.ReadToEndAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi đọc nội dung playlist: {ex.Message}");
                return null;
            }
        }


        private async Task ForwardWebSocketDataToPipeWithBuffer(WebSocket webSocket, NamedPipeServerStream pipeServer,
            Guid streamId, CancellationToken cancellationToken)
        {
            const int bufferFlushSize = 16 * 1024; // Giảm buffer size để flush nhanh hơn
            const int flushIntervalMs = 100; // Giảm interval để responsive hơn
            const int receiveBufferSize = 4096; // Giảm receive buffer

            var receiveBuffer = new byte[receiveBufferSize];
            var bufferStream = new MemoryStream();
            var lastFlushTime = DateTime.UtcNow;
            long totalBytesReceived = 0;

            Console.WriteLine($"Stream {streamId}: Starting WebSocket data forwarding");

            try
            {
                while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), cancellationToken);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Console.WriteLine($"Stream {streamId}: WebSocket close requested");
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Normal closure", CancellationToken.None);
                        break;
                    }
                    else if (result.MessageType == WebSocketMessageType.Binary && result.Count > 0)
                    {
                        totalBytesReceived += result.Count;

                        // Log periodically để monitor
                        if (totalBytesReceived % (1024 * 1024) == 0) // Mỗi 1MB
                        {
                            Console.WriteLine($"Stream {streamId}: Received {totalBytesReceived / 1024 / 1024}MB total");
                        }

                        // Ghi dữ liệu nhận được vào buffer
                        await bufferStream.WriteAsync(receiveBuffer, 0, result.Count, cancellationToken);

                        // Flush conditions
                        var now = DateTime.UtcNow;
                        bool shouldFlush = bufferStream.Length >= bufferFlushSize ||
                                         (now - lastFlushTime).TotalMilliseconds >= flushIntervalMs ||
                                         !result.EndOfMessage; // Flush immediately nếu message chưa kết thúc

                        if (shouldFlush && pipeServer.IsConnected)
                        {
                            try
                            {
                                bufferStream.Seek(0, SeekOrigin.Begin);
                                await pipeServer.WriteAsync(bufferStream.GetBuffer(), 0, (int)bufferStream.Length, cancellationToken);
                                await pipeServer.FlushAsync(cancellationToken);
                                bufferStream.SetLength(0);
                                lastFlushTime = now;
                            }
                            catch (IOException pipeEx)
                            {
                                Console.WriteLine($"Stream {streamId}: Pipe write error: {pipeEx.Message}");
                                break;
                            }
                        }
                    }
                }

                // Flush dữ liệu còn lại khi kết thúc
                if (bufferStream.Length > 0 && pipeServer.IsConnected)
                {
                    try
                    {
                        bufferStream.Seek(0, SeekOrigin.Begin);
                        await pipeServer.WriteAsync(bufferStream.GetBuffer(), 0, (int)bufferStream.Length, cancellationToken);
                        await pipeServer.FlushAsync(cancellationToken);
                        Console.WriteLine($"Stream {streamId}: Final flush completed, total received: {totalBytesReceived} bytes");
                    }
                    catch (IOException pipeEx)
                    {
                        Console.WriteLine($"Stream {streamId}: Final flush error: {pipeEx.Message}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"Stream {streamId}: WebSocket forwarding cancelled");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error forwarding WebSocket data for stream {streamId}: {ex.Message}");
            }
            finally
            {
                bufferStream.Dispose();
                Console.WriteLine($"Stream {streamId}: WebSocket data forwarding completed");
            }
        }

        private async Task CleanupStreamResourcesAsync(Guid streamId, StreamSession session,
            NamedPipeServerStream pipeServer, FFmpegProcessInfo ffmpegInfo)
        {
            Console.WriteLine($"Starting cleanup for stream {streamId}");

            // Cleanup pipe first
            if (pipeServer != null)
            {
                try
                {
                    if (pipeServer.IsConnected)
                    {
                        pipeServer.Disconnect();
                        Console.WriteLine($"Pipe disconnected for stream {streamId}");
                    }
                    pipeServer.Dispose();
                    Console.WriteLine($"Pipe disposed for stream {streamId}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error disposing pipe for stream {streamId}: {ex.Message}");
                }
            }

            // Stop FFmpeg
            if (ffmpegInfo?.Process != null && !ffmpegInfo.Process.HasExited)
            {
                try
                {
                    // Đợi một chút để FFmpeg có thể finalize file
                    await Task.Delay(1000);
                    await _ffmpegService.StopFFmpegAsync(ffmpegInfo.ProcessId);
                    Console.WriteLine($"FFmpeg stopped for stream {streamId}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error stopping FFmpeg for stream {streamId}: {ex.Message}");
                }
            }

            // Wait a bit more before cleanup directory
            await Task.Delay(2000);
            await CleanupOutputDirectory(streamId);

            if (session != null)
            {
                session.IsActive = false;
                session.IsReady = false;
            }

            _activeStreams.TryRemove(streamId, out _);
            Console.WriteLine($"Stream {streamId} cleanup completed");
        }

        private async Task CleanupOutputDirectory(Guid streamId)
        {
            string outputFolder = Path.Combine("wwwroot", "live", streamId.ToString());
            if (!Directory.Exists(outputFolder)) return;

            const int maxRetries = 10; // Tăng số lần thử
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    // Thử xóa từng file trước
                    var files = Directory.GetFiles(outputFolder);
                    foreach (var file in files)
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch (Exception fileEx)
                        {
                            Console.WriteLine($"Error deleting file {file}: {fileEx.Message}");
                        }
                    }

                    // Sau đó xóa thư mục
                    Directory.Delete(outputFolder, true);
                    Console.WriteLine($"Output directory deleted for stream {streamId}");
                    return;
                }
                catch (IOException) when (i < maxRetries - 1)
                {
                    Console.WriteLine($"Directory cleanup retry {i + 1} for stream {streamId}");
                    await Task.Delay(500 * (i + 1));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting output directory for stream {streamId}: {ex.Message}");
                    return;
                }
            }
        }

        public async Task<bool> StopStudentShareAsync(Guid streamId)
        {
            if (_activeStreams.TryGetValue(streamId, out var session))
            {
                session.IsActive = false;
                await CleanupStreamResourcesAsync(streamId, session, null, session.FFmpegProcessInfo);
                Console.WriteLine($"Stream {streamId} stopped manually");
                return true;
            }
            return false;
        }

        public StreamSession GetActiveStreamSessionByUserId(Guid userId) =>
            _activeStreams.Values.FirstOrDefault(s => s.UserId == userId && s.IsActive);

        public StreamSession GetActiveStreamSessionByStreamId(Guid streamId) =>
            _activeStreams.TryGetValue(streamId, out var session) && session.IsActive ? session : null;

        public void Dispose()
        {
            _fileAccessSemaphore?.Dispose();
        }
    }
}