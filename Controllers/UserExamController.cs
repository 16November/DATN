using DoAnTotNghiep.Dto.Response;
using DoAnTotNghiep.Model;
using DoAnTotNghiep.Services.IService;
using DoAnTotNghiep.Services.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DoAnTotNghiep.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserExamController : ControllerBase
    {
        private readonly IUserExamService _userExamService;

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
        public async Task<IActionResult> DeleteUserFromExam([FromQuery] Guid examId, [FromQuery] Guid userId)
        {
            await _userExamService.DeleteUserExam(examId, userId);
            return Ok("User removed from exam.");
        }

        [HttpPut]
        [Route("updateStatus")]
        public async Task<IActionResult> UpdateStatus([FromQuery] Guid userId, [FromQuery] Guid examId, bool isStarted)
        {
            await _userExamService.UpdateStatus(userId, examId, isStarted);
            return Ok("Status updated.");
        }

        [HttpPut]
        [Route("updateSubmitted")]
        public async Task<IActionResult> UpdateSubmitted([FromQuery] Guid examId, [FromQuery] Guid userId)
        {
            await _userExamService.UpdateSubmitedById(examId, userId);
            return Ok("Submit status updated.");
        }

        [HttpGet]
        [Route("getDetail")]
        public async Task<ActionResult<UserExamDto>> GetUserExamDetail([FromQuery]Guid userExamId)
        {
            var result = await _userExamService.GetDetailUserExam(userExamId);
            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpGet]
        [Route("getList")]
        public async Task<ActionResult<List<UserExamDto>>> GetUserExamList([FromQuery]Guid examId)
        {
            var result = await _userExamService.GetListUserExam(examId);
            return Ok(result);
        }
    }
}
