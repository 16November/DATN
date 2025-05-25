using DoAnTotNghiep.Dto.Request;
using DoAnTotNghiep.Services.IService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.InteropServices.Marshalling;

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
            var exam = await examService.AddExam(requestExam);
            return Ok(exam);
        }

        [HttpDelete]
        [Route("delete")]
        public async Task<IActionResult> DeleteExam([FromQuery]Guid examId)
        {
            await examService.DeleteExam(examId);
            return Ok(new { message = "Exam deleted successfully" });
        }

        [HttpPut]
        [Route("update")]
        public async Task<IActionResult> UpdateExam([FromQuery]Guid examId, [FromBody] RequestExam requestExam)
        {
            await examService.UpdateExam(examId, requestExam);
            return Ok(new { message = "Exam updated successfully" });
        }

        [HttpGet]
        [Route("getAllPagnigation")]
        public async Task<IActionResult> GetAllExamByManager([FromQuery]Guid userId, [FromQuery] int page = 1)
        {
            var exams = await examService.GetAllExamByManager(userId, page);
            return Ok(exams);
        }

        [HttpGet]
        [Route("getAll")]
        public async Task<IActionResult> GetAllExam([FromQuery]Guid userId)
        {
            var exams = await examService.GetAllExam(userId);
            return Ok(exams);
        }

        [HttpGet]
        [Route("getAllUser")]
        public async Task<IActionResult> GetAllExamByUser([FromQuery] Guid userId)
        {
            var exams = await examService.GetAllExamUser(userId);
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
