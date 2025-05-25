using DoAnTotNghiep.Dto.Request;
using DoAnTotNghiep.Dto.Response;
using DoAnTotNghiep.Model;

namespace DoAnTotNghiep.Services.IService
{
    public interface IUserAnswerService
    {
        Task<double> AddListUserAnswer(List<RequestUserAnswer> userAnswers,Guid userId,Guid examId);

        Task<List<UserAnswerDto>> GetListUserAnswer(Guid userId, Guid examId);
    }
}
