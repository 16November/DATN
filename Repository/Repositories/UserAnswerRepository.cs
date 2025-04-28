using DoAnTotNghiep.Data;
using DoAnTotNghiep.Model;
using DoAnTotNghiep.Repository.IRepositories;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;


namespace DoAnTotNghiep.Repository.Repositories
{
    public class UserAnswerRepository : IUserAnswerRepository
    {
        private readonly DataContext dataContext;

        public UserAnswerRepository(DataContext dataContext)
        {
            this.dataContext = dataContext;
        }

        public async Task AddListUserAnswer(List<UserAnswer> userAnswers)
        {
           
            await dataContext.BulkInsertAsync(userAnswers, new BulkConfig
            {
                SetOutputIdentity = false
            });
           
        }

        public async Task<List<UserAnswer>> GetUserAnswersByUserId(Guid userId , Guid examId)
        {
            var userAnswers = await dataContext.UserAnswers
                                    .Where(x => x.UserId == userId && x.Answer!.Question!.ExamId == examId)
                                    .Include(x => x.Answer).ThenInclude(q => q!.Question)
                                    .OrderByDescending(x => x.CreatedAt)
                                    .ToListAsync();

            if (!userAnswers.Any())
            {
                throw new KeyNotFoundException("Not found List UserAnswer");
            }

            return userAnswers;
        }
         
        public async Task SaveChangesAsync()
        {
            await dataContext.SaveChangesAsync();
        }
    }
}
