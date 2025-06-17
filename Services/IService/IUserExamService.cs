using DoAnTotNghiep.Dto.Request;
using DoAnTotNghiep.Dto.Response;
using DoAnTotNghiep.Model;

namespace DoAnTotNghiep.Services.IService
{
    public interface IUserExamService
    {
        Task AddUserToExamById(UserExam userExam);

        Task DeleteUserExam(Guid examId, Guid userId);

        Task UpdateStatus(Guid userId, Guid examId, bool IsStarted);

        Task<double> UpdateSubmitedById(Guid examId, Guid userId);

        Task<UserExamDto> GetDetailUserExam(Guid userExamId);

        Task<List<StudentExam>> GetListUserExam(Guid examId);

        Task AddUserToExam(RequestUserToExam request, Guid examId);

        Task AddListUserToExam(List<RequestUserToExam> request, Guid examId);

        Task<List<StudentExamInfo>> GetListStudentByExamId(Guid examId);

        Task<Stat> GetStat(Guid userId);
    }
}
