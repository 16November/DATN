using DoAnTotNghiep.Data;
using DoAnTotNghiep.Model;
using DoAnTotNghiep.Repository.IRepositories;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;

namespace DoAnTotNghiep.Repository.Repositories
{
    public class AnswerRepository : IAnswerRepository
    {
        private readonly DataContext dataContext;

        public AnswerRepository(DataContext dataContext)
        {
            this.dataContext = dataContext;
        }

        public async Task AddListAnswerAsync(List<Answer> answer)
        {
            await dataContext.BulkInsertAsync(answer);
        }

        public async Task DeleteListAnswerAsync(Guid questionId)
        {
            var questions = await dataContext.Answers.Where(x => x.QuestionId == questionId).ToListAsync();

            await dataContext.BulkDeleteAsync(questions);
        }

        public async Task SaveChangesAsync()
        {
            await dataContext.SaveChangesAsync();
        }

        public async Task<List<Answer>> GetAnswersAsync(Guid questionId)
        {
            var answers = await dataContext.Answers.Where(x => x.QuestionId == questionId).ToListAsync();

            if(answers.Count == 0)
            {
                throw new KeyNotFoundException("Not found data about question");
            }

            return answers;

        }

        public async Task UpdateListAnswerAsync(List<Answer> answers)
        {
            await dataContext.BulkUpdateAsync(answers);
        }
    }
}
