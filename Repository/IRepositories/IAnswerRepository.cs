using DoAnTotNghiep.Model;

namespace DoAnTotNghiep.Repository.IRepositories
{
    public interface IAnswerRepository 
    {
        Task AddListAnswerAsync(List<Answer> answer);

        Task DeleteListAnswerAsync(Guid questionId);

        Task SaveChangesAsync();

        Task<List<Answer>> GetAnswersAsync(Guid questionId);

        Task UpdateListAnswerAsync(List<Answer> answers);
    }
}
