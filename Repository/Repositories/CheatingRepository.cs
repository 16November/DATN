using DoAnTotNghiep.Data;
using DoAnTotNghiep.Model;
using DoAnTotNghiep.Repository.IRepositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Update.Internal;

namespace DoAnTotNghiep.Repository.Repositories
{
    public class CheatingRepository : ICheatingEventRepository
    {
        private readonly DataContext dataContext;
        public CheatingRepository(DataContext dataContext)
        {
            this.dataContext = dataContext;
        }

        public async Task AddCheatingEvent(CheatingEvent cheatingEvent)
        {
            await dataContext.CheatingEvents.AddAsync(cheatingEvent);
        }

        public async Task SaveChanges()
        {
            await dataContext.SaveChangesAsync();
        }

        public async Task UpdateCheatingEvent(CheatingEvent update)
        {
            var cheatingEvent = await dataContext.CheatingEvents.FirstOrDefaultAsync(x => x.CheatingId == update.CheatingId && x.UserId == update.UserId);
            if(cheatingEvent == null)
            {
                throw new KeyNotFoundException("Cheating event not found.");
            }

            cheatingEvent.FocusEvent += update.FocusEvent;
            cheatingEvent.BlurEvent += update.BlurEvent;
            cheatingEvent.CopyEvent += update.CopyEvent;
            cheatingEvent.HiddenEvent += update.HiddenEvent;
            dataContext.CheatingEvents.Update(cheatingEvent);
        }

        public async Task<List<CheatingEvent>> GetListCheatingEvent(Guid examId)
        {
            return await dataContext.CheatingEvents
                .Where(x => x.ExamId == examId)
                .ToListAsync();
        }
    }
}
