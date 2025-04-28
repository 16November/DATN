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
        public async Task<IActionResult> AddExam([FromBody] RequestExam requestExam)
        {
            await examService.AddExam(requestExam);
            return Ok(new { message = "Exam created successfully" });
        }

        [HttpDelete("{examId}")]
        public async Task<IActionResult> DeleteExam(Guid examId)
        {
            await examService.DeleteExam(examId);
            return Ok(new { message = "Exam deleted successfully" });
        }

        [HttpPut("{examId}")]
        public async Task<IActionResult> UpdateExam(Guid examId, [FromBody] RequestExam requestExam)
        {
            await examService.UpdateExam(examId, requestExam);
            return Ok(new { message = "Exam updated successfully" });
        }

        [HttpGet("manager/{managerId}")]
        public async Task<IActionResult> GetAllExamByManager(Guid managerId, [FromQuery] int page = 1)
        {
            var exams = await examService.GetAllExamByManager(managerId, page);
            return Ok(exams);
        }

        [HttpGet("search")]
        public async Task<IActionResult> GetAllExamByTitle([FromQuery] string title)
        {
            var exams = await examService.GetAllExamByTitle(title);
            return Ok(exams);
        }

        [HttpGet("{examId}")]
        public async Task<IActionResult> GetExamDetailByExamId(Guid examId)
        {
            var exam = await examService.GetExamDetailByExamId(examId);
            return Ok(exam);
        }

        [HttpPatch("{examId}/publish")]
        public async Task<IActionResult> UpdatePublishedStatus(Guid examId, [FromQuery] bool isPublished)
        {
            await examService.UpdatePublishedByExamId(examId, isPublished);
            return Ok(new { message = "Exam publish status updated" });
        }
    }
}
