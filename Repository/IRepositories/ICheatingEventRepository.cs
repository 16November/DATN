using DoAnTotNghiep.Model;

namespace DoAnTotNghiep.Repository.IRepositories
{
    public interface ICheatingEventRepository
    {
        Task AddCheatingEvent(CheatingEvent cheatingEvent);

        Task SaveChanges();

        Task<List<CheatingEvent>> GetListCheatingEvent(Guid examId);

        Task UpdateCheatingEvent( CheatingEvent updateCheatingEvent);
    }
}
