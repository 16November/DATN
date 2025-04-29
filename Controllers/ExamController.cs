using DoAnTotNghiep.Dto.Request;
using DoAnTotNghiep.Services.IService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DoAnTotNghiep.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExamController : ControllerBase
    {
        private readonly IExamService examService;

        public ExamController(IExamService examService)
        {
            this.examService = examService;
        }

        [HttpPost]
        [Route("add")]
        public async Task<IActionResult> AddExam([FromBody] RequestExam requestExam)
        {
            await examService.AddExam(requestExam);
            return Ok(new { message = "Exam created successfully" });
        }

        [HttpDelete]
        [Route("delete/{id:guid}")]
        public async Task<IActionResult> DeleteExam([FromRoute]Guid examId)
        {
            await examService.DeleteExam(examId);
            return Ok(new { message = "Exam deleted successfully" });
        }

        [HttpPut]
        [Route("updateExam/{id:guid}")]
        public async Task<IActionResult> UpdateExam([FromRoute]Guid examId, [FromBody] RequestExam requestExam)
        {
            await examService.UpdateExam(examId, requestExam);
            return Ok(new { message = "Exam updated successfully" });
        }

        [HttpGet]
        [Route("getAll")]
        public async Task<IActionResult> GetAllExamByManager([FromQuery]Guid userId, [FromQuery] int page = 1)
        {
            var exams = await examService.GetAllExamByManager(userId, page);
            return Ok(exams);
        }

        [HttpGet]
        [Route("search")]
        public async Task<IActionResult> GetAllExamByTitle([FromQuery] string title)
        {
            var exams = await examService.GetAllExamByTitle(title);
            return Ok(exams);
        }

        [HttpGet]
        [Route("getDetail")]
        public async Task<IActionResult> GetExamDetailByExamId([FromQuery]Guid examId)
        {
            var exam = await examService.GetExamDetailByExamId(examId);
            return Ok(exam);
        }

        [HttpPut]
        [Route("updatePublished")]
        public async Task<IActionResult> UpdatePublishedStatus([FromQuery]Guid examId, [FromQuery] bool isPublished)
        {
            await examService.UpdatePublishedByExamId(examId, isPublished);
            return Ok(new { message = "Exam publish status updated" });
        }
    }
}
