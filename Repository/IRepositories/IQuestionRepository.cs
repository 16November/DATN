using DoAnTotNghiep.Model;

namespace DoAnTotNghiep.Repository.IRepositories
{
    public interface IQuestionRepository
    {
        Task AddListQuestionAsync(List<Question> questionAdd);

        Task UpdateQuestionAsync(Guid questionId, Question questionUpdate);

        Task DeleteQuestionAsync(Guid questionId);

        Task<List<Question>> GetListQuestionByExamId(Guid examId);

        Task<Question> GetDetailQuestionAsync(Guid questionId);

        Task AddQuesitonAsync(Question question);

        Task SaveChangesAsync();
    }
}
