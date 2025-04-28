using DoAnTotNghiep.Data;
using DoAnTotNghiep.Model;
using DoAnTotNghiep.Repository.IRepositories;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace DoAnTotNghiep.Repository.Repositories
{
    public class ExamRepository : IExamRepository
    {
        private readonly DataContext dataContext;

        public ExamRepository(DataContext dataContext)
        {
            this.dataContext = dataContext;
        }

        //Get ExamId
        public async Task<Exam> GetExamByExamIdAsync(Guid examId)
        {
            var exam = await dataContext.Exams
                        .FirstOrDefaultAsync(x=> x.ExamId == examId);

            if (exam == null)
            {
                throw new KeyNotFoundException("Not find data about Exam ");
            }

            return exam;
        }

        //Ap dung Giang Vien Nguoi Ra de
        public async Task<List<Exam>> GetAllExamByManagerAsync(Guid userId,int page)
        {
            int take = 10;
            int skip = (page - 1) * take;
            var exams = await dataContext.Exams.
                                Where(x => x.CreatedByUserId == userId).
                                OrderBy(p => p.ExamId)
                                .Skip(skip).Take(take).ToListAsync();

            if (exams == null)
            {
                throw new KeyNotFoundException("Not find data about Exam");
            }

            return exams;

        }

        public async Task DeleteExamByExamIdAsync(Guid examId)
        {
            var exam = await dataContext.Exams.
                            Include(x=> x.Questions).FirstOrDefaultAsync(x => x.ExamId == examId);

            if (exam == null)
            {
                throw new KeyNotFoundException("Not find data to Delete");
            }

            if (exam.Questions!.Any())
            {
                throw new InvalidOperationException("Không thể xóa đề thi vì có bài làm liên quan.");
            }
            
            dataContext.Exams.Remove(exam);
        }

        public async Task UpdateExamByExamId (Guid examId , Exam examUpdate)
        {
            var exam = await dataContext.Exams.FirstOrDefaultAsync(x=> x.ExamId == examId);

            if (exam == null)
            {
                throw new KeyNotFoundException("Not find data to Delete");
            }

            exam.Title = examUpdate.Title;
            exam.Description = examUpdate.Description;
            exam.DurationInMinutes = examUpdate.DurationInMinutes;
            exam.StartDay = examUpdate.StartDay;
            exam.UpdatedAt = examUpdate.UpdatedAt;


        }

        public async Task UpdatePublishedByExamId(Guid examId, bool isPublished)
        {
            var exam = await dataContext.Exams.FirstOrDefaultAsync(x => x.ExamId == examId);

            if(exam == null)
            {
                throw new KeyNotFoundException("Not find data to Delete");
            }

            exam.IsPublished = isPublished;


        }

        public async Task AddExamAsync(Exam exam)
        {
            try
            {
                await dataContext.Exams.AddAsync(exam);
            }
            catch (DbUpdateException dbEx)
            {
                throw new Exception("Lỗi khi thêm Exam vào cơ sở dữ liệu.", dbEx);
            }
            catch (Exception ex)
            {
                // Log lỗi chung cho các exception khác
                throw new Exception("Lỗi không xác định khi thêm Exam.", ex); // Ném lỗi
            }

        }

        public async Task<List<Exam>> GetAllExamByTitle(string title)
        {
            var exam = await dataContext.Exams.Where(p => EF.Functions.Collate(p.Title!, "SQL_Latin1_General_CP1_CI_AI").Contains(title.Trim())).ToListAsync();
            if (exam.Count == 0)
            {
                throw new KeyNotFoundException("Not found Exam by this title");
            }

            return exam;
        }
        public async Task SaveChangesAsync()
        {
            await dataContext.SaveChangesAsync();
        }
    }
}
