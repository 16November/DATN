using DoAnTotNghiep.Dto.Request;
using DoAnTotNghiep.Dto.Response;

namespace DoAnTotNghiep.Services.IService
{
    public interface IQuestionService
    {
        Task<List<QuestionUserDto>> GetListQuestionByExamIdUser(Guid examId);

        Task<QuestionDto> GetQuestionDetailByQuestionId(Guid questionId);

        Task<List<QuestionDto>> GetListQuestionByExamId(Guid examId);

        Task AddQuestionAsync(RequestQuestion requestQuestion);

        Task UpdateQuestionAsync(Guid questionId, RequestQuestion questionUpdate);

        Task DeleteQuestion(Guid questionId);

        Task<List<ErrorQuestionAdd>> AddListQuestionAsync(List<RequestQuestion> requestQuestions, Guid examId);
    }
}
