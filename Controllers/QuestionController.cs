using DoAnTotNghiep.Dto.Request;
using DoAnTotNghiep.Services.IService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DoAnTotNghiep.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuestionController : ControllerBase
    {
        private readonly IQuestionService questionService;

        public QuestionController(IQuestionService questionService)
        {
            this.questionService = questionService;
        }

        // GET: api/question/exam/{examId}
        [HttpGet("exam/{examId}")]
        public async Task<IActionResult> GetQuestionsByExamId(Guid examId)
        {
            var result = await questionService.GetListQuestionByExamId(examId);
            return Ok(result);
        }

        // GET: api/question/detail/{questionId}
        [HttpGet("detail/{questionId}")]
        public async Task<IActionResult> GetQuestionDetail(Guid questionId)
        {
            var result = await questionService.GetQuestionDetailByQuestionId(questionId);
            return Ok(result);
        }

        // GET: api/question/user/exam/{examId}
        [HttpGet("user/exam/{examId}")]
        public async Task<IActionResult> GetQuestionForUser(Guid examId)
        {
            var result = await questionService.GetListQuestionByExamIdUser(examId);
            return Ok(result);
        }

        // POST: api/question
        [HttpPost]
        public async Task<IActionResult> AddQuestion([FromBody] RequestQuestion request)
        {
            await questionService.AddQuestionAsync(request);
            return Ok(new { message = "Câu hỏi đã được thêm thành công." });
        }

        // PUT: api/question/{questionId}
        [HttpPut("{questionId}")]
        public async Task<IActionResult> UpdateQuestion(Guid questionId, [FromBody] RequestQuestion request)
        {
            await questionService.UpdateQuestionAsync(questionId, request);
            return Ok(new { message = "Câu hỏi đã được cập nhật thành công." });
        }

        // DELETE: api/question/{questionId}
        [HttpDelete("{questionId}")]
        public async Task<IActionResult> DeleteQuestion(Guid questionId)
        {
            await questionService.DeleteQuestion(questionId);
            return Ok(new { message = "Câu hỏi đã được xóa thành công." });
        }
    }
}
