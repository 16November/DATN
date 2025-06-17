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

        [HttpGet]
        [Route("getByExamId")]
        public async Task<IActionResult> GetQuestionsByExamId([FromQuery] Guid examId)
        {
            var result = await questionService.GetListQuestionByExamId(examId);
            return Ok(result);
        }

        [HttpGet]
        [Route("getDetail")]
        public async Task<IActionResult> GetQuestionDetail([FromQuery] Guid questionId)
        {
            var result = await questionService.GetQuestionDetailByQuestionId(questionId);
            return Ok(result);
        }

        [HttpGet]
        [Route("getExamUser")]
        public async Task<IActionResult> GetQuestionForUser([FromQuery] Guid examId)
        {
            var result = await questionService.GetListQuestionByExamIdUser(examId);
            return Ok(result);
        }


        [HttpPost]
        [Route("add")]
        public async Task<IActionResult> AddQuestion([FromBody] RequestQuestion request)
        {
            await questionService.AddQuestionAsync(request);
            return Ok(new { message = "Câu hỏi đã được thêm thành công." });
        }


        [HttpPut]
        [Route("update")]
        public async Task<IActionResult> UpdateQuestion([FromQuery] Guid questionId, [FromBody] RequestQuestion request)
        {
            await questionService.UpdateQuestionAsync(questionId, request);
            return Ok(new { message = "Câu hỏi đã được cập nhật thành công." });
        }


        [HttpDelete]
        [Route("delete")]
        public async Task<IActionResult> DeleteQuestion([FromQuery] Guid questionId)
        {
            await questionService.DeleteQuestion(questionId);
            return Ok(new { message = "Câu hỏi đã được xóa thành công." });
        }

        [HttpPost]
        [Route("addList")]
        public async Task<IActionResult> AddListQuestion([FromBody] List<RequestQuestion> requestQuestions, [FromQuery] Guid examId)
        {
            await questionService.AddListQuestionAsync(requestQuestions, examId);
            return Ok(new { message = "Danh sách câu hỏi đã được thêm thành công." });
        }
    }
}
