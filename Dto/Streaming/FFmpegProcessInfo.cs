using System.Diagnostics;
using System.Text.Json.Serialization;

namespace DoAnTotNghiep.Dto.Streaming
{
    public class FFmpegProcessInfo
    {
        public int ProcessId { get; set; }
        public string InputPipePath { get; set; }

        [JsonIgnore] // Bỏ qua Process vì không thể serialize
        public Process Process { get; set; }
    }
}
