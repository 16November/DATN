using DoAnTotNghiep.Dto.Response;
using DoAnTotNghiep.Model;
using DoAnTotNghiep.Repository.IRepositories;
using DoAnTotNghiep.Services.IService;
using SequentialGuid;

namespace DoAnTotNghiep.Services.Service
{
    public class CheatingService : ICheatingService
    {
        private readonly ICheatingEventRepository cheatingEventRepository;
        public CheatingService(ICheatingEventRepository cheatingEventRepository)
        {
            this.cheatingEventRepository = cheatingEventRepository;
        }

        public async Task<Guid> AddCheatingEvent(CheatingEvent cheatingEvent)
        {
            cheatingEvent.CheatingId = (Guid)SequentialSqlGuidGenerator.Instance.NewSqlGuid();
            await cheatingEventRepository.AddCheatingEvent(cheatingEvent);
            await cheatingEventRepository.SaveChanges();
            return cheatingEvent.CheatingId;
        }

        public async Task<List<CheatingDto>> GetListCheatingEvent(Guid examId)
        {
            var cheatingEvents = await cheatingEventRepository.GetListCheatingEvent(examId);
            var cheatingDto = new List<CheatingDto>();
            foreach (var cheater in cheatingEvents)
            {
                var cheating = new CheatingDto
                {
                    UserId = cheater.UserId,
                };
                int totalEvents = 0;
                if (cheater.CopyEvent > 3) totalEvents++;
                if (cheater.HiddenEvent > 3) totalEvents++;
                if (cheater.MultiTabEvent > 2) totalEvents++;
                if (cheater.CtrCEvent > 2) totalEvents++;
                if (cheater.PageSwitchEvent > 2) totalEvents++;
                if (cheater.FocusEvent > 3) totalEvents++;
                if (cheater.BlurEvent > 3) totalEvents++;

                if (totalEvents <= 2)
                {
                    cheating.Description = "Chưa có hành vi gian lận";
                } else if (totalEvents >= 3 && totalEvents < 5)
                {
                    cheating.Description = "Có hành vi gian lận";
                } else if (totalEvents >= 5)
                {
                    cheating.Description = "Có hành vi gian lận nghiêm trọng";
                }

                cheatingDto.Add(cheating);
            }
            return cheatingDto;
        }

        public async Task UpdateCheatingEvent(CheatingEvent updateCheatingEvent)
        {
            await cheatingEventRepository.UpdateCheatingEvent(updateCheatingEvent);
            await cheatingEventRepository.SaveChanges();
        }

    }
}
