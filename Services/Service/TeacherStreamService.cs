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

                // Start FFmpeg process
                string pipePath = $@"\\.\pipe\{pipeName}";
                Console.WriteLine($"Starting FFmpeg for stream {streamId}");
                ffmpegInfo = await _ffmpegService.StartFFmpegAsync(pipePath, streamId);
                session.FFmpegProcessInfo = ffmpegInfo;

                // Wait for pipe connection with timeout
                using var pipeTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(mainCts.Token, pipeTimeoutCts.Token);

                try
                {
                    await pipeServer.WaitForConnectionAsync(combinedCts.Token);
                    Console.WriteLine($"Pipe connected for stream {streamId}");
                }
                catch (OperationCanceledException) when (pipeTimeoutCts.Token.IsCancellationRequested)
                {
                    Console.WriteLine($"Timeout waiting for pipe connection for stream {streamId}");
                    throw new TimeoutException("FFmpeg connection timeout");
                }

                // Monitor playlist file creation bằng polling (bổ sung kiểm tra kích thước file)
                var playlistReady = await MonitorPlaylistCreation(playlistPath, streamId, mainCts.Token);
                if (playlistReady)
                {
                    session.IsReady = true;
                    Console.WriteLine($"Stream {streamId} is ready for playback");
                }
                else
                {
                    Console.WriteLine($"Stream {streamId} playlist not ready after timeout");
                }

                // Forward WebSocket data vào pipe
                await ForwardWebSocketDataToPipeWithBuffer(webSocket, pipeServer, streamId, mainCts.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in HandleStudentStreamDataAsync for stream {streamId}: {ex.Message}");
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

        private async Task<bool> MonitorPlaylistCreation(string playlistPath, Guid streamId, CancellationToken cancellationToken)
        {
            var timeout = TimeSpan.FromSeconds(30);
            var startTime = DateTime.UtcNow;

            while (!cancellationToken.IsCancellationRequested && DateTime.UtcNow - startTime < timeout)
            {
                if (File.Exists(playlistPath))
                {
                    var fileInfo = new FileInfo(playlistPath);
                    if (fileInfo.Length > 0)
                    {
                        Console.WriteLine($"Playlist ready detected by polling for stream {streamId}");
                        return true;
                    }
                }
                await Task.Delay(500, cancellationToken);
            }

            Console.WriteLine($"Timeout waiting for playlist file for stream {streamId}");
            return false;
        }

        private async Task ForwardWebSocketDataToPipeWithBuffer(WebSocket webSocket, NamedPipeServerStream pipeServer,
            Guid streamId, CancellationToken cancellationToken)
        {
            const int bufferFlushSize = 32 * 1024; // 32KB
            const int flushIntervalMs = 200;

            var receiveBuffer = new byte[8192];
            var bufferStream = new MemoryStream();
            var lastFlushTime = DateTime.UtcNow;

            try
            {
                while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), cancellationToken);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Normal closure", CancellationToken.None);
                        break;
                    }
                    else if (result.MessageType == WebSocketMessageType.Binary && result.Count > 0)
                    {
                        // Ghi dữ liệu nhận được vào buffer
                        await bufferStream.WriteAsync(receiveBuffer, 0, result.Count, cancellationToken);

                        // Nếu buffer đủ lớn hoặc quá thời gian flush thì ghi ra pipe
                        var now = DateTime.UtcNow;
                        if (bufferStream.Length >= bufferFlushSize || (now - lastFlushTime).TotalMilliseconds >= flushIntervalMs)
                        {
                            bufferStream.Seek(0, SeekOrigin.Begin);
                            if (pipeServer.IsConnected)
                            {
                                await pipeServer.WriteAsync(bufferStream.GetBuffer(), 0, (int)bufferStream.Length, cancellationToken);
                                await pipeServer.FlushAsync(cancellationToken);
                            }
                            bufferStream.SetLength(0);
                            lastFlushTime = now;
                        }
                    }
                }

                // Flush dữ liệu còn lại khi kết thúc
                if (bufferStream.Length > 0 && pipeServer.IsConnected)
                {
                    bufferStream.Seek(0, SeekOrigin.Begin);
                    await pipeServer.WriteAsync(bufferStream.GetBuffer(), 0, (int)bufferStream.Length, cancellationToken);
                    await pipeServer.FlushAsync(cancellationToken);
                    bufferStream.SetLength(0);
                }
            }
            catch (OperationCanceledException)
            {
                // Hủy bỏ bình thường
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error forwarding WebSocket data with buffer for stream {streamId}: {ex.Message}");
            }
            finally
            {
                bufferStream.Dispose();
            }
        }

        private async Task CleanupStreamResourcesAsync(Guid streamId, StreamSession session,
            NamedPipeServerStream pipeServer, FFmpegProcessInfo ffmpegInfo)
        {
            Console.WriteLine($"Starting cleanup for stream {streamId}");

            if (pipeServer != null)
            {
                try
                {
                    if (pipeServer.IsConnected)
                        pipeServer.Disconnect();
                    pipeServer.Dispose();
                    Console.WriteLine($"Pipe disposed for stream {streamId}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error disposing pipe for stream {streamId}: {ex.Message}");
                }
            }

            if (ffmpegInfo?.Process != null && !ffmpegInfo.Process.HasExited)
            {
                try
                {
                    await _ffmpegService.StopFFmpegAsync(ffmpegInfo.ProcessId);
                    Console.WriteLine($"FFmpeg stopped for stream {streamId}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error stopping FFmpeg for stream {streamId}: {ex.Message}");
                }
            }

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

            const int maxRetries = 5;
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    Directory.Delete(outputFolder, true);
                    Console.WriteLine($"Output directory deleted for stream {streamId}");
                    return;
                }
                catch (IOException) when (i < maxRetries - 1)
                {
                    await Task.Delay(200 * (i + 1));
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
    }
}
