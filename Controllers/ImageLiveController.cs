using DoAnTotNghiep.Dto.Request;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DoAnTotNghiep.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImageLiveController : ControllerBase
    {
        public readonly IWebHostEnvironment webHostEnvironment;
        public ImageLiveController(IWebHostEnvironment webHostEnvironment)
        {
            this.webHostEnvironment = webHostEnvironment;

        }

        [HttpPost("imageReceive")]
        public async Task<IActionResult> ReceiveScreenshot(
            [FromForm] RequestScreenshot request) 
        {
            if (request.ScreenShot == null || request.ScreenShot.Length == 0)
            {
                return BadRequest("Ảnh không hợp lệ");
            }

            var tempFolder = Path.Combine(webHostEnvironment.ContentRootPath, "TempScreenshoots");
            Directory.CreateDirectory(tempFolder);

            var fileName = $"{request.userId.ToString()}_{request.examId.ToString()}_{request.timeStamp:yyyyMMdd_HHmmss}.jpg";
            var filePath = Path.Combine(tempFolder, fileName);

            //Lưu ảnh trên ổ đĩa  
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await request.ScreenShot.CopyToAsync(stream);
            }

            return Ok(new { message = "Ảnh đã được nhận thành công", filePath });
        }


    }
}
