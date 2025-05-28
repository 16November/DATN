// DoAnTotNghiep.Controllers/TeacherController.cs
using Microsoft.AspNetCore.Mvc;
using DoAnTotNghiep.Services.Streaming;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace DoAnTotNghiep.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TeacherController : ControllerBase
    {
        private readonly Services.Streaming.WebSocketManager _streamingWebSocketManager;
        private readonly ControlWebSocketManager _controlWebSocketManager;
        private readonly HlsStreamManager _hlsStreamManager; // Thêm HlsStreamManager
        private readonly ILogger<TeacherController> _logger;

        public TeacherController(
            Services.Streaming.WebSocketManager streamingWebSocketManager,
            ControlWebSocketManager controlWebSocketManager,
            HlsStreamManager hlsStreamManager, // Inject HlsStreamManager
            ILogger<TeacherController> logger)
        {
            _streamingWebSocketManager = streamingWebSocketManager;
            _controlWebSocketManager = controlWebSocketManager;
            _hlsStreamManager = hlsStreamManager; // Gán hlsStreamManager
            _logger = logger;
        }

        [HttpPost("request-view")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RequestView([FromQuery] Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return BadRequest(new { error = "Yêu cầu User ID hợp lệ" });
            }

            try
            {
                var success = await _streamingWebSocketManager.RequestViewAsync(userId);

                return success
                    ? Ok(new { message = "Yêu cầu xem đã được gửi thành công", userId })
                    : NotFound(new { error = "Không tìm thấy kết nối của học sinh", userId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể xử lý yêu cầu xem cho người dùng {UserId}", userId);
                return StatusCode(500, new { error = "Lỗi máy chủ nội bộ" });
            }
        }

        [HttpPost("start-student-stream")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> StartStudentStream([FromQuery] Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return BadRequest(new { error = "Yêu cầu User ID hợp lệ" });
            }

            try
            {
                // Gửi lệnh start stream đến student
                var commandSent = await _controlWebSocketManager.SendControlCommandAsync(userId, "start_stream");
                if (!commandSent)
                {
                    return NotFound(new { error = "Không tìm thấy kết nối điều khiển của học sinh hoặc kết nối đã đóng", userId });
                }

                _logger.LogInformation("Đã gửi lệnh 'start_stream' đến học sinh {UserId}, đang chờ stream được khởi tạo...", userId);

                // Chờ stream được khởi tạo (polling với timeout)
                var maxWaitTime = TimeSpan.FromSeconds(30); // Chờ tối đa 30 giây
                var pollInterval = TimeSpan.FromMilliseconds(500); // Kiểm tra mỗi 500ms
                var startTime = DateTime.UtcNow;

                string? streamUrl = null;
                while (DateTime.UtcNow - startTime < maxWaitTime)
                {
                    var streamPath = _hlsStreamManager.GetStreamPath(userId);
                    if (streamPath != null)
                    {
                        streamUrl = $"/hls/{userId}/stream.m3u8";
                        break;
                    }

                    await Task.Delay(pollInterval);
                }

                if (streamUrl != null)
                {
                    return Ok(new
                    {
                        message = "Đã gửi lệnh 'start_stream' và stream đã sẵn sàng",
                        userId,
                        streamUrl
                    });
                }
                else
                {
                    _logger.LogWarning("Stream chưa sẵn sàng sau {MaxWaitTime} giây cho học sinh {UserId}", maxWaitTime.TotalSeconds, userId);
                    return Ok(new
                    {
                        message = "Đã gửi lệnh 'start_stream' đến học sinh nhưng stream chưa sẵn sàng. Vui lòng thử lại sau.",
                        userId,
                        streamUrl = (string?)null
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể gửi lệnh 'start_stream' cho người dùng {UserId}", userId);
                return StatusCode(500, new { error = "Lỗi máy chủ nội bộ" });
            }
        }

        [HttpPost("stop-student-stream")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> StopStudentStream([FromQuery] Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return BadRequest(new { error = "Yêu cầu User ID hợp lệ" });
            }

            try
            {
                // Gửi lệnh stop đến student trước
                var commandSent = await _controlWebSocketManager.SendControlCommandAsync(userId, "stop_stream");

                // Dừng stream trên server (bất kể lệnh có được gửi thành công hay không)
                var streamStopped = await _hlsStreamManager.StopStreamAsync(userId);

                if (commandSent || streamStopped)
                {
                    return Ok(new
                    {
                        message = "Đã gửi lệnh 'stop_stream' và dừng stream thành công",
                        userId,
                        commandSent,
                        streamStopped
                    });
                }
                else
                {
                    return NotFound(new
                    {
                        error = "Không tìm thấy kết nối điều khiển hoặc stream của học sinh",
                        userId
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể gửi lệnh 'stop_stream' cho người dùng {UserId}", userId);
                return StatusCode(500, new { error = "Lỗi máy chủ nội bộ" });
            }
        }

        // Thêm endpoint mới để check stream status
        [HttpGet("stream-status")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        public IActionResult GetStreamStatus([FromQuery] Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return BadRequest(new { error = "Yêu cầu User ID hợp lệ" });
            }

            var streamPath = _hlsStreamManager.GetStreamPath(userId);
            var isActive = streamPath != null;
            var streamUrl = isActive ? $"/hls/{userId}/stream.m3u8" : null;

            return Ok(new
            {
                userId,
                isActive,
                streamUrl,
                timestamp = DateTime.UtcNow
            });
        }

        [HttpGet("ws-control")]
        public async Task<IActionResult> ControlWebSocket([FromQuery] Guid userId)
        {
            if (userId == Guid.Empty)
            {
                _logger.LogWarning("Control WebSocket request with empty or invalid User ID.");
                return BadRequest("Yêu cầu User ID hợp lệ.");
            }

            if (!HttpContext.WebSockets.IsWebSocketRequest)
            {
                return BadRequest("Yêu cầu WebSocket được mong đợi.");
            }

            var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            _logger.LogInformation("Control WebSocket accepted for user: {UserId}", userId);

            await _controlWebSocketManager.AddControlConnectionAsync(userId, webSocket);

            var buffer = new byte[1024 * 4];
            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        // Chỉ gọi CloseAsync nếu trạng thái hợp lệ
                        if (webSocket.State == WebSocketState.Open ||
                            webSocket.State == WebSocketState.CloseReceived ||
                            webSocket.State == WebSocketState.CloseSent)
                        {
                            await webSocket.CloseAsync(
                                result.CloseStatus ?? WebSocketCloseStatus.NormalClosure,
                                result.CloseStatusDescription,
                                CancellationToken.None);
                        }

                        _logger.LogInformation("Control WebSocket for user {UserId} closed by client. Code: {CloseCode}, Reason: {CloseReason}",
                            userId, result.CloseStatus, result.CloseStatusDescription);
                        break;
                    }

                    // Xử lý các command từ frontend nếu cần
                }
            }
            catch (WebSocketException ex) when (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
            {
                _logger.LogWarning("Control WebSocket connection prematurely closed for user {UserId}: {Message}", userId, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Control WebSocket for user {UserId}.", userId);
            }
            finally
            {
                await _controlWebSocketManager.RemoveControlConnectionAsync(userId);

                try
                {
                    webSocket.Dispose();
                }
                catch { }
            }

            return new EmptyResult();
        }



        [HttpGet("connections")]
        public IActionResult GetActiveConnections()
        {
            return Ok(new { message = "Tính năng chưa được triển khai" });
        }
    }
}