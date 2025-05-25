using AutoMapper;
using DoAnTotNghiep.Data;
using DoAnTotNghiep.Dto.Request;
using DoAnTotNghiep.Dto.Response;
using DoAnTotNghiep.Model;
using DoAnTotNghiep.Repository.IRepositories;
using DoAnTotNghiep.Services.IService;
using Microsoft.Identity.Client;
using Microsoft.VisualBasic;
using SequentialGuid;

namespace DoAnTotNghiep.Services.Service
{
    public class UserAnswerService : IUserAnswerService
    {
        private readonly IUserAnswerRepository userAnswerRepository;
        private readonly IMapper mapper;
        private readonly DataContext dataContext;
        private readonly IUserExamRepository userExamRepository;    

        public UserAnswerService(IUserAnswerRepository userAnswerRepository, IMapper mapper, DataContext dataContext
            ,IUserExamRepository userExamRepository)
        {
            this.userAnswerRepository = userAnswerRepository;
            this.mapper = mapper;
            this.dataContext = dataContext;
            this.userExamRepository = userExamRepository;
        }

        public async Task<double> AddListUserAnswer(List<RequestUserAnswer> userAnswers,Guid userId,Guid examId)
        {
            using var transaction = await dataContext.Database.BeginTransactionAsync();
            var userAnswerAdd = mapper.Map<List<UserAnswer>>(userAnswers);
            try
            {
                foreach(var userAnswer in userAnswerAdd)
                {
                    userAnswer.UserAnswerId = (Guid)SequentialSqlGuidGenerator.Instance.NewSqlGuid();
                    userAnswer.UserId = userId;
                    userAnswer.ExamId = examId;
                }
                await userAnswerRepository.AddListUserAnswer(userAnswerAdd);

                //UdpateSubmittedById
                var score = await userExamRepository.UpdateSubmitedById(examId, userId);

                await transaction.CommitAsync();
                await userAnswerRepository.SaveChangesAsync();
                return score;
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
