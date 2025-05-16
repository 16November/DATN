using DoAnTotNghiep.Model;
using DoAnTotNghiep.Services.IService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DoAnTotNghiep.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserAnswerController : ControllerBase
    {
        private readonly IUserAnswerService userAnswerService;

        public UserAnswerController(IUserAnswerService userAnswerService)
        {
            this.userAnswerService = userAnswerService;
        }

        // POST: api/useranswer
        [HttpPost]
        [Route("add")]
        public async Task<IActionResult> AddUserAnswers([FromBody] List<UserAnswer> userAnswers)
        {
            await userAnswerService.AddListUserAnswer(userAnswers);
            return Ok(new { message = "Lưu câu trả lời thành công." });
        }

        // GET: api/useranswer?userId=xxx&examId=xxx
        [HttpGet]
        [Route("getList")]
        public async Task<IActionResult> GetUserAnswers([FromQuery] Guid userId, [FromQuery] Guid examId)
        {
            var result = await userAnswerService.GetListUserAnswer(userId, examId);
            return Ok(result);
        }
    }
}
