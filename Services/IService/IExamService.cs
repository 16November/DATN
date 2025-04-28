using DoAnTotNghiep.Dto.Request;
using DoAnTotNghiep.Dto.Response;

namespace DoAnTotNghiep.Services.IService
{
    public interface IExamService
    {
        Task AddExam(RequestExam requestExam);

        Task DeleteExam(Guid examId);

        Task UpdateExam(Guid examId, RequestExam requestExam);

        Task<List<ExamDto>> GetAllExamByManager(Guid examId, int page);

        Task<List<ExamDto>> GetAllExamByTitle(string title);

        Task<ExamDto> GetExamDetailByExamId(Guid examId);

        Task UpdatePublishedByExamId(Guid examId, bool isPublished);

    }
}
