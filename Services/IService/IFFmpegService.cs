using DoAnTotNghiep.Dto.Streaming;
using System.IO.Pipes;

namespace DoAnTotNghiep.Services.IService
{
    public interface IFFmpegService
    {
        string BuildArguments(string inputPipePath, Guid streamId);
        Task<FFmpegProcessInfo> StartFFmpegAsync(string inputPipePath, Guid streamId);
        Task<bool> StopFFmpegAsync(int processId);
        Stream GetFFmpegInputPipeStream(string pipePath);

        Task<NamedPipeServerStream> CreateInputPipeAsync(string pipePath);
    }
}
