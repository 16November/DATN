using DoAnTotNghiep.Dto.Response;
using DoAnTotNghiep.Model;

namespace DoAnTotNghiep.Services.IService
{
    public interface ICheatingService
    {
        Task<Guid> AddCheatingEvent(CheatingEvent cheatingEvent);

        Task<List<CheatingDto>> GetListCheatingEvent(Guid examId);

        Task UpdateCheatingEvent(CheatingEvent updateCheatingEvent);
    }
}
