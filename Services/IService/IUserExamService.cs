using DoAnTotNghiep.Dto.Response;
using DoAnTotNghiep.Model;

namespace DoAnTotNghiep.Services.IService
{
    public interface IUserExamService
    {
        Task AddUserToExamById(UserExam userExam);

        Task DeleteUserExam(Guid examId, Guid userId);

        Task UpdateStatus(Guid userId, Guid examId, bool IsStarted);

        Task UpdateSubmitedById(Guid examId, Guid userId);

        Task<UserExamDto> GetDetailUserExam(Guid userExamId);

        Task<List<UserExamDto>> GetListUserExam(Guid examId);
    }
}
