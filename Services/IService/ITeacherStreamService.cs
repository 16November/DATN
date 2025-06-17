using DoAnTotNghiep.Dto.Streaming;
using System.Net.WebSockets;

namespace DoAnTotNghiep.Services.IService
{
    public interface ITeacherStreamService
    {
        Task<StreamSession> RequestStudentToShareAsync(Guid userId);
        Task<bool> StopStudentShareAsync(Guid streamId);
        Task HandleStudentStreamDataAsync(Guid streamId, WebSocket webSocket);

        // Interface
        StreamSession? GetActiveStreamSessionByUserId(Guid userId);
        StreamSession? GetActiveStreamSessionByStreamId(Guid streamId);

        Task<Guid> getStudentIdByStreamId(Guid streamId);


    }
}
