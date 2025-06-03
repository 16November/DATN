using DoAnTotNghiep.Services.IService;
using DoAnTotNghiep.Services.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace DoAnTotNghiep.Controllers
{
    [ApiController]
    [Route("api")]
    public class TeacherStreamController : ControllerBase
    {
        private readonly ITeacherStreamService _teacherStreamService;
        private readonly IHubContext<NotificationHub> _notificationHub;

        public TeacherStreamController(ITeacherStreamService teacherStreamService,
                                       IHubContext<NotificationHub> notificationHub)
        {
            _teacherStreamService = teacherStreamService;
            _notificationHub = notificationHub;
        }

        // Giáo viên yêu cầu học sinh chia sẻ màn hình
        [HttpPost("teacher/request-share/{userId}")]
        public async Task<IActionResult> RequestShare(Guid userId)
        {
            try
            {
                var session = await _teacherStreamService.RequestStudentToShareAsync(userId);

                var connectionIds = NotificationHub.GetConnectionIdsStatic(userId);
                if (connectionIds == null || connectionIds.Count == 0)
                    return NotFound(new { Message = "Student not connected." });

                foreach (var connId in connectionIds)
                {
                    await _notificationHub.Clients.Client(connId)
                        .SendAsync("ReceiveRequestShare", new { Message = "Teacher yêu cầu bạn chia sẻ màn hình" });
                }

                return Ok(session);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in RequestShare: {ex.Message}");
                return StatusCode(500, new { Message = "Internal server error", Details = ex.Message });
            }
        }

        // Học sinh chấp nhận yêu cầu chia sẻ, trả về session stream
        [HttpPost("student/accept-share/{userId}")]
        public IActionResult AcceptShare(Guid userId)
        {
            try
            {
                var session = _teacherStreamService.GetActiveStreamSessionByUserId(userId);
                if (session == null)
                    return NotFound(new { Message = "Stream session not found." });

                return Ok(session);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AcceptShare: {ex.Message}");
                return StatusCode(500, new { Message = "Internal server error", Details = ex.Message });
            }
        }

        // Giáo viên dừng chia sẻ màn hình
        [HttpPost("teacher/stop-share/{streamId}")]
        public async Task<IActionResult> StopShare(Guid streamId)
        {
            try
            {
                var result = await _teacherStreamService.StopStudentShareAsync(streamId);
                if (result)
                    return Ok(new { Message = "Stopped successfully" });
                else
                    return NotFound(new { Message = "Stream not found or already stopped" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in StopShare: {ex.Message}");
                return StatusCode(500, new { Message = "Internal server error", Details = ex.Message });
            }
        }

        // API lấy session đang active theo userId (kiểm tra trạng thái hoặc lấy info stream)
        [HttpGet("streaming/session")]
        public IActionResult GetStreamSession([FromQuery] Guid userId)
        {
            try
            {
                var session = _teacherStreamService.GetActiveStreamSessionByUserId(userId);
                if (session == null)
                    return NotFound(new { Message = "No active stream session found for this user." });
                return Ok(session);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetStreamSession: {ex.Message}");
                return StatusCode(500, new { Message = "Internal server error", Details = ex.Message });
            }
        }

        // API lấy URL phát lại theo streamId (nếu muốn)
        [HttpGet("streaming/url")]
        public IActionResult GetStreamUrl([FromQuery] Guid streamId)
        {
            try
            {
                var session = _teacherStreamService.GetActiveStreamSessionByStreamId(streamId);
                if (session == null)
                    return NotFound(new { Message = "Stream session not found." });
                return Ok(new { streamUrl = session.PlaybackUrl });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetStreamUrl: {ex.Message}");
                return StatusCode(500, new { Message = "Internal server error", Details = ex.Message });
            }
        }

        // API kiểm tra trạng thái stream (isReady, isActive) cho FE polling
        [HttpGet("streaming/status/{streamId}")]
        public IActionResult GetStreamStatus(Guid streamId)
        {
            var session = _teacherStreamService.GetActiveStreamSessionByStreamId(streamId);
            if (session == null)
                return NotFound(new { Message = "Stream session not found." });

            return Ok(new { isReady = session.IsReady, isActive = session.IsActive });
        }

        // WebSocket nhận stream dữ liệu từ học sinh (nếu middleware chưa xử lý thì dùng)
        [HttpGet("student/{streamId}/ws")]
        public async Task GetStreamWebSocket(Guid streamId)
        {
            try
            {
                if (!HttpContext.WebSockets.IsWebSocketRequest)
                {
                    HttpContext.Response.StatusCode = 400;
                    await HttpContext.Response.WriteAsync("WebSocket request expected.");
                    return;
                }

                var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

                await _teacherStreamService.HandleStudentStreamDataAsync(streamId, webSocket);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling stream data: {ex.Message}");

                if (!HttpContext.Response.HasStarted)
                {
                    HttpContext.Response.StatusCode = 500;
                    await HttpContext.Response.WriteAsync("Internal server error");
                }
            }
        }
    }
}
