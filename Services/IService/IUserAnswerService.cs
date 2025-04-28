using DoAnTotNghiep.Dto.Response;
using DoAnTotNghiep.Model;

namespace DoAnTotNghiep.Services.IService
{
    public interface IUserAnswerService
    {
        Task AddListUserAnswer(List<UserAnswer> userAnswers);

        Task<List<UserAnswerDto>> GetListUserAnswer(Guid userId, Guid examId);
    }
}
