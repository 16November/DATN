using AutoMapper;
using DoAnTotNghiep.Data;
using DoAnTotNghiep.Dto.Response;
using DoAnTotNghiep.Model;
using DoAnTotNghiep.Repository.IRepositories;
using DoAnTotNghiep.Services.IService;
using Microsoft.VisualBasic;
using SequentialGuid;

namespace DoAnTotNghiep.Services.Service
{
    public class UserAnswerService : IUserAnswerService
    {
        private readonly IUserAnswerRepository userAnswerRepository;
        private readonly IMapper mapper;
        private readonly DataContext dataContext;

        public UserAnswerService(IUserAnswerRepository userAnswerRepository, IMapper mapper, DataContext dataContext)
        {
            this.userAnswerRepository = userAnswerRepository;
            this.mapper = mapper;
            this.dataContext = dataContext;
        }

        public async Task AddListUserAnswer(List<UserAnswer> userAnswers)
        {
            using var transaction = await dataContext.Database.BeginTransactionAsync();
            try
            {
                foreach(var userAnswer in userAnswers)
                {
                    userAnswer.UserAnswerId = (Guid)SequentialSqlGuidGenerator.Instance.NewSqlGuid();
                }
                await userAnswerRepository.AddListUserAnswer(userAnswers);
                await transaction.CommitAsync();
            }
            catch(Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }

        }

        public async Task<List<UserAnswerDto>> GetListUserAnswer(Guid userId,Guid examId)
        {
            var userAnswers = await userAnswerRepository.GetUserAnswersByUserId(userId, examId);

            var userAnswersDto = mapper.Map<List<UserAnswerDto>>(userAnswers);

            return userAnswersDto;
        }
    }
}
