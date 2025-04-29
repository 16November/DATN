using DoAnTotNghiep.Data;
using DoAnTotNghiep.Model;
using DoAnTotNghiep.Repository.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DoAnTotNghiep.Repository.Repositories
{
    public class UserExamRepository : IUserExamRepository
    {
        private readonly DataContext dataContext;

        public UserExamRepository(DataContext dataContext)
        {
            this.dataContext = dataContext;
        }

        public async Task AddUserToExamById(UserExam userExam)
        {
            await dataContext.UserExams.AddAsync(userExam);
        }

        public async Task UpdateSubmitedById(Guid examId, Guid userId)
        {
            var userExam = await dataContext.UserExams
                .FirstOrDefaultAsync(x => x.ExamId == examId && x.UserId == userId);

            if (userExam == null)
            {
                throw new KeyNotFoundException("UserExam not found");
            }

            userExam.IsSubmitted = true;
        }

        public async Task UpdateStatusAsync(Guid userId, Guid examId, bool isStarted)
        {
            var userExam = await dataContext.UserExams
                .FirstOrDefaultAsync(x => x.UserId == userId && x.ExamId == examId);

            if (userExam == null)
            {
                throw new KeyNotFoundException("UserExam not found");
            }

            userExam.IsStarted = isStarted;
        }

        public async Task DeleteUserFromExam(Guid examId, Guid userId)
        {
            var userExam = await dataContext.UserExams
                .FirstOrDefaultAsync(x => x.ExamId == examId && x.UserId == userId);

            if (userExam == null)
            {
                throw new KeyNotFoundException("UserExam not found");
            }

            dataContext.UserExams.Remove(userExam);
        }

        public async Task<List<UserExam>> GetListUserExam(Guid examId)
        {
            var userExams = await dataContext.UserExams
                .Where(x => x.ExamId == examId)
                .ToListAsync();

            if (userExams.Count == 0)
            {
                throw new KeyNotFoundException("UserExam not found");
            }

            return userExams;
        }

        public async Task<UserExam> GetDetailUserExam(Guid userExamId)
        {
            var userExam = await dataContext.UserExams
                .Include(x=>x.User)
                .ThenInclude(u=> u!.UserInfo)
                .FirstOrDefaultAsync(x => x.UserExamId == userExamId);

            if (userExam == null)
            {
                throw new KeyNotFoundException("UserExam not found");
            }

            return userExam;
        }

        public async Task SaveChangesAsync()
        {
            await dataContext.SaveChangesAsync();
        }
    }

}
