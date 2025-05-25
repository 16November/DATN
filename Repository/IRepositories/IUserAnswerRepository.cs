using DoAnTotNghiep.Model;

namespace DoAnTotNghiep.Repository.IRepositories
{
    public interface IUserAnswerRepository
    {
        Task AddListUserAnswer(List<UserAnswer> userAnswers);

        Task<List<UserAnswer>> GetUserAnswersByUserId(Guid userId, Guid examId);

        Task SaveChangesAsync();

        
    }
}
