using DoAnTotNghiep.Model;

namespace DoAnTotNghiep.Repository.IRepositories
{
    public interface IUserExamRepository
    {
        Task SaveChangesAsync();

        Task AddUserToExamById(UserExam userExam);

        Task UpdateSubmitedById(Guid examId, Guid userId);

        Task UpdateStatusAsync(Guid userId, Guid examId, bool IsStarted);

        Task DeleteUserFromExam(Guid examId, Guid userId);

        Task<List<UserExam>> GetListUserExam(Guid examId);

        Task<UserExam> GetDetailUserExam(Guid userExamId);

        
    }
}
