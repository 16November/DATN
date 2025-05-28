using DoAnTotNghiep.Dto.Response;
using DoAnTotNghiep.Model;

namespace DoAnTotNghiep.Repository.IRepositories
{
    public interface IUserExamRepository
    {
        Task SaveChangesAsync();

        Task AddUserToExamById(UserExam userExam);

        Task<double> UpdateSubmitedById(Guid examId, Guid userId);

        Task UpdateStatusAsync(Guid userId, Guid examId, bool IsStarted);

        Task DeleteUserFromExam(Guid examId, Guid userId);

        Task<List<StudentExam>> GetListUserExam(Guid examId);

        Task<UserExam> GetDetailUserExam(Guid userExamId);

        Task AddListUserToExam(List<UserExam> userExams);

        Task<List<StudentExamInfo>> getListStudent(Guid examId);



    }
}
