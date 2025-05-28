using DoAnTotNghiep.Dto.Request;
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
        private readonly IUserExamService userExamService;

        public UserExamController(IUserExamService userExamService)
        {
            this.userExamService = userExamService;
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddUserToExam([FromBody] RequestUserToExam request, [FromQuery]Guid examId)
        {
            await userExamService.AddUserToExam(request, examId);
            return Ok("User added to exam.");
        }

        [HttpPost("addList")]
        public async Task<IActionResult> AddListUserToExam([FromBody] List<RequestUserToExam> request, [FromQuery] Guid examId)
        {
            await userExamService.AddListUserToExam(request, examId);
            return Ok("Users added to exam.");
        }


        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteUserFromExam([FromQuery] Guid examId, [FromQuery] Guid userId)
        {
            await userExamService.DeleteUserExam(examId, userId);
            return Ok("User removed from exam.");
        }

        [HttpPut]
        [Route("updateStatus")]
        public async Task<IActionResult> UpdateStatus([FromQuery] Guid userId, [FromQuery] Guid examId, [FromQuery] bool isStarted)
        {
            await userExamService.UpdateStatus(userId, examId, isStarted);
            return Ok("Status updated.");
        }

        [HttpPut]
        [Route("updateSubmitted")]
        public async Task<IActionResult> UpdateSubmitted([FromQuery] Guid examId, [FromQuery] Guid userId)
        {
            var score = await userExamService.UpdateSubmitedById(examId, userId);
            return Ok(score);
        }

        [HttpGet]
        [Route("getDetail")]
        public async Task<ActionResult<UserExamDto>> GetUserExamDetail([FromQuery]Guid userExamId)
        {
            var result = await userExamService.GetDetailUserExam(userExamId);
            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpGet]
        [Route("getList")]
        public async Task<ActionResult> GetUserExamList([FromQuery]Guid examId)
        {
            var result = await userExamService.GetListUserExam(examId);
            return Ok(result);
        }

        [HttpGet]
        [Route("getListStudent")]
        public async Task<IActionResult> GetListStudentFromExam([FromQuery]Guid examId)
        {
            var results = await userExamService.GetListStudentByExamId(examId);
            return Ok(results); 
        }
    }
}
