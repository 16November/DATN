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
            [FromForm] IFormFile screenshoot,
            [FromForm] string userId,
            [FromForm] string examId,
            [FromForm] DateTime timeStamp)
        {
            if (screenshoot == null || screenshoot.Length == 0)
            {
                return BadRequest("Ảnh không hợp lệ");   
            }

            var tempFolder = Path.Combine(webHostEnvironment.ContentRootPath, "TempScreenshoots");
            Directory.CreateDirectory(tempFolder);

            var fileName = $"{userId}_{examId}_{timeStamp:yyyyMMdd_HHmmss}.jpg";
            var filePath = Path.Combine(tempFolder, fileName);
            
            //Lưu ảnh trên ổ đĩa  
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await screenshoot.CopyToAsync(stream);
            }

            return Ok(new { message = "Ảnh đã được nhận thành công", filePath });
        }

        
    }
}
