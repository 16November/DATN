using DoAnTotNghiep.Services.Streaming;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DoAnTotNghiep.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StreamingController : ControllerBase
    {
        private readonly Services.Streaming.WebSocketManager webSocketManager;
        private readonly HlsStreamManager hlsStreamManager;

        public StreamingController(Services.Streaming.WebSocketManager webSocketManager, HlsStreamManager hlsStreamManager)
        {
            this.webSocketManager = webSocketManager;
            this.hlsStreamManager = hlsStreamManager;
        }

        [HttpGet("/ws-stream")]
        public async Task<IActionResult> Stream([FromQuery] Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return BadRequest("User ID is required.");
            }

            if (!HttpContext.WebSockets.IsWebSocketRequest)
            {
                return BadRequest("WebSocket request expected.");
            }

            try
            {
                using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                await using var videoStream = new MemoryStream(); // Stream dùng cho FFmpeg
                using var cancelTokenSource = new CancellationTokenSource();

                // Khởi chạy FFmpeg stream song song
                var ffmpegTask = hlsStreamManager.StartStreamAsync(userId, videoStream, cancelTokenSource.Token);

                // Xử lý WebSocket để nhận dữ liệu từ client
                await webSocketManager.HandleStudentStream(webSocket, videoStream, cancelTokenSource);

                // Chờ FFmpeg kết thúc (có thể bị cancel từ client)
                await ffmpegTask;

                return Ok();
            }
            catch (Exception ex)
            {
                // TODO: Có thể log lỗi nếu có logger
                Console.WriteLine($"[Streaming Error] {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing the stream.");
            }
        }
    }
}
