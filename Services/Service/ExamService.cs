using AutoMapper;
using DoAnTotNghiep.Data;
using DoAnTotNghiep.Repository.IRepositories;
using DoAnTotNghiep.Dto.Request;
using DoAnTotNghiep.Model;
using SequentialGuid;
using System.Net;
using DoAnTotNghiep.Dto.Response;
using DoAnTotNghiep.Services.IService;
using Microsoft.VisualBasic;

namespace DoAnTotNghiep.Services.Service
{
    public class ExamService : IExamService
    {
        private readonly IExamRepository examRepository;
        private readonly IMapper mapper;
        private readonly DataContext dataContext;
        public ExamService(IExamRepository examRepository , IMapper mapper, DataContext dataContext)
        {
            this.examRepository = examRepository;
            this.mapper = mapper;
            this.dataContext = dataContext;
        }

        public void Validate(RequestExam request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request), "RequestExam không được null.");

            if (string.IsNullOrWhiteSpace(request.Title))
                throw new ArgumentException("Tiêu đề (Title) không được để trống.");

            if (request.Title.Length > 200)
                throw new ArgumentException("Tiêu đề (Title) không được vượt quá 200 ký tự.");

            if (!string.IsNullOrEmpty(request.Description) && request.Description.Length > 1000)
                throw new ArgumentException("Mô tả (Description) không được vượt quá 1000 ký tự.");

            if (request.DurationInMinutes < 1 || request.DurationInMinutes > 1000)
                throw new ArgumentException("Thời lượng (DurationInMinutes) phải từ 1 đến 1000 phút.");

            if (request.StartDay < DateTime.UtcNow)
                throw new ArgumentException("Ngày bắt đầu (StartDay) phải lớn hơn hoặc bằng thời gian hiện tại.");

            if (request.CreatedByUserId == Guid.Empty)
                throw new ArgumentException("CreatedByUserId không được để trống.");
        }


        public async Task<ExamDto> AddExam(RequestExam requestExam)
        {
            var examAdd = mapper.Map<Exam>(requestExam);
            examAdd.ExamId = (Guid)SequentialSqlGuidGenerator.Instance.NewSqlGuid();
            examAdd.CreatedAt = DateTime.UtcNow;
            var exam = await examRepository.AddExamAsync(examAdd);
            await examRepository.SaveChangesAsync();
            var examDto = mapper.Map<ExamDto>(exam);
            return examDto;
        }

        public async Task DeleteExam(Guid examId)
        {
            await examRepository.DeleteExamByExamIdAsync(examId);
            await examRepository.SaveChangesAsync();
        }

        public async Task UpdateExam(Guid examId , RequestExam requestExam)
        {
            var exam = mapper.Map<Exam>(requestExam);
            exam.UpdatedAt = DateTime.UtcNow;
            await examRepository.UpdateExamByExamId(examId, exam);
            await examRepository.SaveChangesAsync();
        }

        public async Task<List<ExamDto>> GetAllExamByManager(Guid examId , int page)
        {
            var exams = await examRepository.GetAllExamByManagerAsync(examId, page);
            var examsDto = mapper.Map<List<ExamDto>>(exams);
            return examsDto;

        }

        public async Task<List<ExamDto>> GetAllExamByTitle(string title)
        {
            var exams = await examRepository.GetAllExamByTitle(title);
            var examsDto = mapper.Map<List<ExamDto>>(exams);

            return examsDto;
        }

        public async Task<ExamDto> GetExamDetailByExamId(Guid examId)
        {
            var exam = await examRepository.GetExamByExamIdAsync (examId);
            var examDto = mapper.Map<ExamDto>(exam);
            examDto.CountOfQuestions = exam.Questions.Count;
            return examDto;
        }

        public async Task UpdatePublishedByExamId(Guid examId, bool isPublished)
        {
            await examRepository.UpdatePublishedByExamId (examId, isPublished);
            await examRepository.SaveChangesAsync();

        }

        public async Task<List<ExamDto>> GetAllExam(Guid userId)
        {
            var examsDto = await examRepository.GetAllExamAsync(userId);
            
            return  examsDto;
        }

        public async Task<List<ExamDto>> GetAllExamUser(Guid userId)
        {
            var examsDto = await examRepository.GetAllExamUserAsync(userId);
            return examsDto;
        }

    }
}
