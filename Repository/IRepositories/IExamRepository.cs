using DoAnTotNghiep.Model;
using System.Threading.Tasks;

namespace DoAnTotNghiep.Repository.IRepositories
{
    public interface IExamRepository
    {
        Task<Exam> AddExamAsync(Exam exam);

        Task UpdatePublishedByExamId(Guid examId, bool isPublished);

        Task UpdateExamByExamId(Guid examId, Exam examUpdate);

        Task DeleteExamByExamIdAsync(Guid examId);

        Task<List<Exam>> GetAllExamByManagerAsync(Guid userId, int page);

        Task<Exam> GetExamByExamIdAsync(Guid examId);

        Task<List<Exam>> GetAllExamByTitle(string title);

        Task SaveChangesAsync();
    }
}
