using DoAnTotNghiep.Services.Streaming;
using Microsoft.AspNetCore.Mvc;
using System.IO.Pipelines;
using System.Net.WebSockets;

namespace DoAnTotNghiep.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StreamingController : ControllerBase
    {
        private readonly Services.Streaming.WebSocketManager _webSocketManager;
        private readonly HlsStreamManager _hlsStreamManager;
        private readonly ILogger<StreamingController> _logger;

        // Khởi tạo controller với các dịch vụ được inject
        public StreamingController(
            Services.Streaming.WebSocketManager webSocketManager,
            HlsStreamManager hlsStreamManager,
            ILogger<StreamingController> logger)
        {
            _webSocketManager = webSocketManager ?? throw new ArgumentNullException(nameof(webSocketManager));
            _hlsStreamManager = hlsStreamManager ?? throw new ArgumentNullException(nameof(hlsStreamManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet("ws-stream")]
        public async Task StartLiveStream([FromQuery] Guid userId)
        {
            if (userId == Guid.Empty || !HttpContext.WebSockets.IsWebSocketRequest)
            {
                HttpContext.Response.StatusCode = 400;
                await HttpContext.Response.WriteAsync("Yêu cầu WebSocket hợp lệ với userId.");
                return;
            }

            var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            var connectionId = _webSocketManager.RegisterConnection(webSocket);
            var cts = new CancellationTokenSource(TimeSpan.FromHours(2));
            bool streamStarted = false;

            try
            {
                var pipe = new Pipe(); // Dùng System.IO.Pipelines

                // Bắt đầu đọc từ WebSocket và ghi vào pipe.Writer
                _ = _webSocketManager.HandleStudentStreamAsync(
                    webSocket,
                    pipe.Writer.AsStream(), // Ghi vào Writer
                    connectionId,
                    cts.Token);

                // Gọi FFmpeg và truyền pipe.Reader.AsStream() vào
                var (success, streamUrl) = await _hlsStreamManager.StartStreamAsync(
                    userId,
                    pipe.Reader.AsStream(), // Đọc từ Reader
                    cts.Token);

                if (!success)
                {
                    _webSocketManager.RemoveConnection(connectionId);
                    return;
                }

                streamStarted = true;

                // WebSocket sẽ tiếp tục xử lý cho tới khi đóng kết nối
                while (!cts.Token.IsCancellationRequested && webSocket.State == WebSocketState.Open)
                {
                    await Task.Delay(1000, cts.Token); // chỉ giữ kết nối
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xử lý stream WebSocket cho user {UserId}", userId);
            }
            finally
            {
                cts.Dispose();
                if (streamStarted)
                {
                    await _hlsStreamManager.StopStreamAsync(userId);
                }
                _webSocketManager.RemoveConnection(connectionId);
            }
        }



        [HttpGet("stream")]
        public IActionResult GetStream([FromQuery] Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return BadRequest(new { error = "Yêu cầu User ID hợp lệ." });
            }

            var streamPath = _hlsStreamManager.GetStreamPath(userId);
            if (string.IsNullOrEmpty(streamPath))
            {
                _logger.LogWarning("Không tìm thấy luồng cho người dùng {UserId}", userId);
                return NotFound(new { error = "Không tìm thấy luồng." });
            }

            // Trả về physical file với mime type phù hợp cho HLS
            return PhysicalFile(streamPath, "application/x-mpegURL");
        }

        [HttpDelete("stream/{userId}")]
        public async Task<IActionResult> StopStream([FromRoute] Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return BadRequest(new { error = "Yêu cầu User ID hợp lệ." });
            }

            var stopped = await _hlsStreamManager.StopStreamAsync(userId);
            if (stopped)
            {
                _logger.LogInformation("Luồng cho người dùng {UserId} đã dừng thành công.", userId);
                return Ok(new { message = "Luồng đã dừng thành công." });
            }
            else
            {
                _logger.LogInformation("Cố gắng dừng luồng cho người dùng {UserId}, nhưng không tìm thấy luồng nào đang hoạt động.", userId);
                return NotFound(new { error = "Không tìm thấy luồng hoặc luồng đã dừng." });
            }
        }
    }
}