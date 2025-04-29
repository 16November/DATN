using DoAnTotNghiep.Dto.Response;
using DoAnTotNghiep.Model;
using DoAnTotNghiep.Services.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DoAnTotNghiep.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserExamController : ControllerBase
    {
        private readonly UserExamService _userExamService;

        public UserExamController(UserExamService userExamService)
        {
            _userExamService = userExamService;
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddUserToExam([FromBody] UserExam userExam)
        {
            await _userExamService.AddUserToExamById(userExam);
            return Ok("User added to exam.");
        }

        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteUserFromExam(Guid examId, Guid userId)
        {
            await _userExamService.DeleteUserExam(examId, userId);
            return Ok("User removed from exam.");
        }

        [HttpPut("update-status")]
        public async Task<IActionResult> UpdateStatus(Guid userId, Guid examId, bool isStarted)
        {
            await _userExamService.UpdateStatus(userId, examId, isStarted);
            return Ok("Status updated.");
        }

        [HttpPut("submit")]
        public async Task<IActionResult> UpdateSubmitted(Guid examId, Guid userId)
        {
            await _userExamService.UpdateSubmitedById(examId, userId);
            return Ok("Submit status updated.");
        }

        [HttpGet("{userExamId}")]
        public async Task<ActionResult<UserExamDto>> GetUserExamDetail(Guid userExamId)
        {
            var result = await _userExamService.GetDetailUserExam(userExamId);
            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpGet("exam/{examId}")]
        public async Task<ActionResult<List<UserExamDto>>> GetUserExamList(Guid examId)
        {
            var result = await _userExamService.GetListUserExam(examId);
            return Ok(result);
        }
    }
}
