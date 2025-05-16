using DoAnTotNghiep.Data;
using DoAnTotNghiep.Model;
using DoAnTotNghiep.Repository.IRepositories;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;

namespace DoAnTotNghiep.Repository.Repositories
{
    public class QuestionRepository : IQuestionRepository
    {
        private readonly DataContext dataContext;

        public QuestionRepository(DataContext dataContext)
        {
            this.dataContext = dataContext;
        }

        public async Task AddListQuestionAsync(List<Question> questionAdd )
        {
            using var transaction = await dataContext.Database.BeginTransactionAsync();

            try
            {
                await dataContext.BulkInsertAsync(questionAdd, new BulkConfig
                {
                    SetOutputIdentity = false,
                });
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task UpdateQuestionAsync(Guid questionId , Question questionUpdate)
        {
            var question = await dataContext.Questions.FirstOrDefaultAsync(x => x.QuestionId == questionId);
            if (question == null)
            {
                throw new KeyNotFoundException("Not find data about question");
            }

            question.Content = questionUpdate.Content;
            question.ImageUrl = questionUpdate.ImageUrl;

            await dataContext.SaveChangesAsync();
        }

        public async Task DeleteQuestionAsync(Guid questionId)
        {
            var question = await dataContext.Questions
                        .Include(q => q.Answers) 
                        .FirstOrDefaultAsync(x => x.QuestionId == questionId);

            if (question == null)
            {
                throw new KeyNotFoundException("Không tìm thấy dữ liệu về câu hỏi.");
            }

            // Kiểm tra xem có UserAnswer liên quan không
            bool hasUserAnswer = await dataContext.UserAnswers
                .AnyAsync(ua => ua.QuestionId == questionId);

            if (hasUserAnswer)
            {
                throw new InvalidOperationException("Không thể xóa câu hỏi vì đã có bài làm liên quan.");
            }

            dataContext.Questions.Remove(question);
            await dataContext.SaveChangesAsync();
        }

        public async Task<List<Question>> GetListQuestionByExamId(Guid examId)
        {
            var questions = await dataContext.Questions.Include(x=> x.Answers).Where(x => x.ExamId == examId).ToListAsync();

            if (!questions.Any())
            {
                throw new KeyNotFoundException("Not found data about Question");
            }

            return questions;
        }

        public async Task<Question> GetDetailQuestionAsync(Guid questionId)
        {
            var question = await dataContext.Questions.Include(x=> x.Answers).FirstOrDefaultAsync(x => x.QuestionId == questionId);

            if(question == null)
            {
                throw new KeyNotFoundException("Not found data about Question");
            }

            return question;
        }

        public async Task SaveChangesAsync()
        {
            await dataContext.SaveChangesAsync();
        }

        public async Task AddQuesitonAsync(Question question)
        {
            await dataContext.Questions.AddAsync(question);  
        }
    }
}
 