using System.Text.Json.Serialization;

namespace DoAnTotNghiep.Dto.Streaming
{
    public class StreamSession
    {
        public Guid StreamId { get; set; }
        public Guid UserId { get; set; }
        public Guid? TeacherId { get; set; }
        public DateTime StartTime { get; set; }
        public string PlaybackUrl { get; set; }

        [JsonIgnore] // Bỏ qua thuộc tính này khi serialize JSON
        public FFmpegProcessInfo FFmpegProcessInfo { get; set; }

        public bool IsActive { get; set; }

        public bool IsReady { get; set; }
    }

}
