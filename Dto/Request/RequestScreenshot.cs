using Microsoft.AspNetCore.Mvc;

namespace DoAnTotNghiep.Dto.Request
{
    public class RequestScreenshot
    {
        [FromForm]
        public IFormFile ScreenShot { get; set; }

        [FromForm]
        public Guid userId { get; set; }

        [FromForm]
        public Guid examId { get; set; }

        [FromForm]
        public DateTime timeStamp { get; set; }

    }
}
