using DoAnTotNghiep.Model;
using DoAnTotNghiep.Services.IService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DoAnTotNghiep.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CheatingController : ControllerBase
    {
        private readonly ICheatingService cheatingService;

        public CheatingController(ICheatingService cheatingService)
        {
            this.cheatingService = cheatingService;
        }

        [HttpPost]
        [Route("add")]
        public async Task<IActionResult> AddCheatingEvent([FromBody] CheatingEvent cheatingEvent)
        {
            if (cheatingEvent == null)
            {
                return BadRequest("Cheating event cannot be null.");
            }
            var cheatingId = await cheatingService.AddCheatingEvent(cheatingEvent);
            return Ok(new { CheatingId = cheatingId });
        }

        [HttpPut]
        [Route("update")]
        public async Task<IActionResult> UpdateCheatingEvent([FromBody] CheatingEvent updateCheatingEvent)
        {
            if (updateCheatingEvent == null)
            {
                return BadRequest("Update event cannot be null.");
            }
            await cheatingService.UpdateCheatingEvent(updateCheatingEvent);
            return Ok("Cheating event updated successfully.");
        }

        [HttpGet]
        [Route("getList")]
        public async Task<IActionResult> GetListCheatingEvent([FromQuery] Guid examId)
        {
            if (examId == Guid.Empty)
            {
                return BadRequest("Exam ID cannot be empty.");
            }
            var cheatingEvents = await cheatingService.GetListCheatingEvent(examId);
            return Ok(cheatingEvents);

        }

    }
}
