using DoAnTotNghiep.Services.Streaming;
using Microsoft.AspNetCore.Mvc;
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
            if (userId == Guid.Empty)
            {
                HttpContext.Response.StatusCode = 400;
                await HttpContext.Response.WriteAsync("UserId hợp lệ là bắt buộc.");
                return;
            }

            if (!HttpContext.WebSockets.IsWebSocketRequest)
            {
                HttpContext.Response.StatusCode = 400;
                await HttpContext.Response.WriteAsync("Chỉ chấp nhận WebSocket request.");
                return;
            }

            var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            var connectionId = _webSocketManager.RegisterConnection(webSocket);

            using var cts = new CancellationTokenSource(TimeSpan.FromHours(3));

            try
            {
                _logger.LogInformation("Starting live stream for user {UserId} with connection {ConnectionId}", userId, connectionId);

                using var pipeStream = new PipeStream();

                var streamTask = _webSocketManager.HandleStudentStreamAsync(
                    webSocket, pipeStream, connectionId.ToString(), cts.Token);

                var (success, streamUrl) = await _hlsStreamManager.StartStreamAsync(
                    userId, pipeStream, cts.Token);

                if (!success)
                {
                    _logger.LogError("Failed to start HLS stream for user {UserId}", userId);
                    await webSocket.CloseAsync(WebSocketCloseStatus.InternalServerError, "Không thể khởi tạo stream", cts.Token);
                    return;
                }

                _logger.LogInformation("HLS stream started successfully for user {UserId}. URL: {StreamUrl}", userId, streamUrl);

                await streamTask;

                _logger.LogInformation("Live stream completed for user {UserId}", userId);
            }
            catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
            {
                _logger.LogInformation("Live stream cancelled for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing WebSocket stream for user {UserId}", userId);
            }
            finally
            {
                cts.Cancel();

                try
                {
                    await _hlsStreamManager.StopStreamAsync(userId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error stopping HLS stream for user {UserId}", userId);
                }

                _webSocketManager.RemoveConnection(connectionId);
            }
        }


        [HttpPost("request-view/{userId}")]
        public async Task<IActionResult> RequestView(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return BadRequest("UserId hợp lệ là bắt buộc.");
            }

            try
            {
                var success = await _webSocketManager.RequestViewAsync(userId);

                if (success)
                {
                    _logger.LogInformation("View request sent successfully to user {UserId}", userId);
                    return Ok(new { message = "Yêu cầu xem đã được gửi thành công.", userId });
                }

                _logger.LogWarning("Failed to send view request to user {UserId} - no active connection", userId);
                return NotFound(new { message = "Không tìm thấy kết nối WebSocket hoạt động cho user này.", userId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending view request to user {UserId}", userId);
                return StatusCode(500, new { message = "Lỗi server khi gửi yêu cầu xem.", error = ex.Message });
            }
        }

        [HttpGet("status/{userId}")]
        public IActionResult GetStreamStatus(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return BadRequest("UserId hợp lệ là bắt buộc.");
            }

            try
            {
                var status = _hlsStreamManager.GetStreamStatus(userId);

                return Ok(new
                {
                    userId,
                    isActive = status.IsActive,
                    hasPlaylist = status.HasPlaylist,
                    processRunning = status.ProcessRunning,
                    startTime = status.StartTime,
                    streamUrl = status.IsActive && status.HasPlaylist ?
                        _hlsStreamManager.GetStreamUrl(userId) : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stream status for user {UserId}", userId);
                return StatusCode(500, new { message = "Lỗi server khi lấy trạng thái stream.", error = ex.Message });
            }
        }

        [HttpGet("hls/{userId}/playlist.m3u8")]
        public async Task<IActionResult> GetPlaylist(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return BadRequest("UserId hợp lệ là bắt buộc.");
            }

            try
            {
                var streamPath = _hlsStreamManager.GetStreamPath(userId);

                if (string.IsNullOrEmpty(streamPath) || !System.IO.File.Exists(streamPath))
                {
                    _logger.LogWarning("Playlist not found for user {UserId}", userId);
                    return NotFound("Stream playlist không tồn tại.");
                }

                var content = await System.IO.File.ReadAllTextAsync(streamPath);

                return Content(content, "application/vnd.apple.mpegurl");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting playlist for user {UserId}", userId);
                return StatusCode(500, "Lỗi server khi lấy playlist.");
            }
        }

        [HttpGet("hls/{userId}/{segmentName}")]
        public async Task<IActionResult> GetSegment(Guid userId, string segmentName)
        {
            if (userId == Guid.Empty)
            {
                return BadRequest("UserId hợp lệ là bắt buộc.");
            }

            if (string.IsNullOrWhiteSpace(segmentName))
            {
                return BadRequest("Tên segment hợp lệ là bắt buộc.");
            }

            try
            {
                var status = _hlsStreamManager.GetStreamStatus(userId);

                if (!status.IsActive || !status.HasPlaylist)
                {
                    return NotFound("Stream không hoạt động.");
                }

                // Xây dựng đường dẫn segment
                var streamDirectory = Path.GetDirectoryName(status.FilePath);
                if (string.IsNullOrEmpty(streamDirectory))
                {
                    return NotFound("Không tìm thấy thư mục stream.");
                }

                var segmentPath = Path.Combine(streamDirectory, segmentName);

                // Kiểm tra bảo mật - đảm bảo segment nằm trong thư mục stream
                var fullStreamDirectory = Path.GetFullPath(streamDirectory);
                var fullSegmentPath = Path.GetFullPath(segmentPath);

                if (!fullSegmentPath.StartsWith(fullStreamDirectory))
                {
                    _logger.LogWarning("Potential path traversal attack detected for user {UserId}, segment {SegmentName}", userId, segmentName);
                    return BadRequest("Đường dẫn segment không hợp lệ.");
                }

                if (!System.IO.File.Exists(segmentPath))
                {
                    _logger.LogDebug("Segment {SegmentName} not found for user {UserId}", segmentName, userId);
                    return NotFound("Segment không tồn tại.");
                }

                var segmentData = await System.IO.File.ReadAllBytesAsync(segmentPath);

                return File(segmentData, "video/MP2T");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting segment {SegmentName} for user {UserId}", segmentName, userId);
                return StatusCode(500, "Lỗi server khi lấy segment.");
            }
        }

        [HttpDelete("stop/{userId}")]
        public async Task<IActionResult> StopStream(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return BadRequest("UserId hợp lệ là bắt buộc.");
            }

            try
            {
                var success = await _hlsStreamManager.StopStreamAsync(userId);

                if (success)
                {
                    _logger.LogInformation("Stream stopped successfully for user {UserId}", userId);
                    return Ok(new { message = "Stream đã được dừng thành công.", userId });
                }

                _logger.LogInformation("No active stream to stop for user {UserId}", userId);
                return Ok(new { message = "Không có stream hoạt động để dừng.", userId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping stream for user {UserId}", userId);
                return StatusCode(500, new { message = "Lỗi server khi dừng stream.", error = ex.Message });
            }
        }

        [HttpGet("url/{userId}")]
        public IActionResult GetStreamUrl(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return BadRequest("UserId hợp lệ là bắt buộc.");
            }

            try
            {
                var streamUrl = _hlsStreamManager.GetStreamUrl(userId);

                if (string.IsNullOrEmpty(streamUrl))
                {
                    return NotFound(new { message = "Stream URL không tồn tại hoặc stream không hoạt động.", userId });
                }

                return Ok(new
                {
                    userId,
                    streamUrl,
                    playlistUrl = $"/api/streaming/hls/{userId}/playlist.m3u8"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stream URL for user {UserId}", userId);
                return StatusCode(500, new { message = "Lỗi server khi lấy stream URL.", error = ex.Message });
            }
        }
    }
}