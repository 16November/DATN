using DoAnTotNghiep.Data;
using DoAnTotNghiep.Dto.Response;
using DoAnTotNghiep.Model;
using DoAnTotNghiep.Repository.IRepositories;
using EFCore.BulkExtensions;
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

        public async Task<double> UpdateSubmitedById(Guid examId, Guid userId)
        {
            var userExam = await dataContext.UserExams
                .FirstOrDefaultAsync(x => x.ExamId == examId && x.UserId == userId);

            if (userExam == null)
            {
                throw new KeyNotFoundException("UserExam not found");
            }

            userExam.IsSubmitted = true;
            userExam.IsStarted = false;
            var score = await CalculateScoreAsync(userId, examId);
            userExam.Score = score;
            userExam.SubmitTime = DateTime.UtcNow;
            return score;

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

        public async Task<double> CalculateScoreAsync(Guid userId, Guid examId)
        {
            // Lấy tất cả các câu trả lời của người dùng cho bài thi
            var userAnswers = await dataContext.UserAnswers
                .Where(ua => ua.UserId == userId && ua.ExamId == examId)
                .Include(ua => ua.Answer) // Include để truy cập IsCorrect
                .ToListAsync();

            // Tính điểm: 1 điểm cho mỗi câu trả lời đúng

            var countQuestions = await dataContext.Questions.CountAsync(q => q.ExamId == examId);
            double correctCount = userAnswers.Count(ua => ua.Answer != null && ua.Answer.IsCorrect);
            double score = (correctCount / (double)countQuestions) * 10;
            score = Math.Round(score, 2);
            return score;
        }
        public async Task DeleteUserFromExam(Guid examId, Guid userId)
        {
            var userExam = await dataContext.UserExams
                            .FirstOrDefaultAsync(x => x.ExamId == examId && x.UserId == userId);

            if (userExam == null)
            {
                throw new KeyNotFoundException("Không tìm thấy bản ghi UserExam.");
            }

            // Kiểm tra nếu người dùng đã làm bài
            var questionIds = await dataContext.Questions
                .Where(q => q.ExamId == examId)
                .Select(q => q.QuestionId)
                .ToListAsync();

            var hasUserAnswers = await dataContext.UserAnswers
                .AnyAsync(ua => ua.UserId == userId && questionIds.Contains(ua.QuestionId));

            if (hasUserAnswers)
            {
                throw new InvalidOperationException("Không thể xóa người dùng khỏi đề thi vì đã có bài làm.");
            }

            dataContext.UserExams.Remove(userExam);
        }

        public async Task<List<StudentExam>> GetListUserExam(Guid examId)
        {
            var userExam = await dataContext.UserExams
                                .Where(x => x.ExamId == examId)
                                .Join(
                                    dataContext.UserInfos, // bang duoc join userInfo
                                    x => x.UserId, // bang ngoai exam
                                    ui => ui.UserId,
                                    (x, ui) => new StudentExam()
                                    {
                                        FullName = ui.FullName,
                                        MSSV = ui.MSSV,
                                        score = x.Score ?? 0,
                                        IsSubmitted = x.IsSubmitted,
                                    }).ToListAsync();
            if (userExam == null)
            {
                throw new KeyNotFoundException("Không tìm thấy danh sách UserExam.");
            }

            return userExam;
        }

        public async Task<UserExam> GetDetailUserExam(Guid userExamId)
        {
            var userExam = await dataContext.UserExams
                .Include(x => x.User)
                .ThenInclude(u => u!.UserInfo)
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

        public async Task AddListUserToExam(List<UserExam> userExams)
        {
            await dataContext.BulkInsertAsync(userExams, new BulkConfig
            {
                SetOutputIdentity = false,
            });
        }
    }
}
