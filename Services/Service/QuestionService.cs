using AutoMapper;
using DoAnTotNghiep.Data;
using DoAnTotNghiep.Dto.Request;
using DoAnTotNghiep.Dto.Response;
using DoAnTotNghiep.Model;
using DoAnTotNghiep.Repository.IRepositories;
using DoAnTotNghiep.Services.IService;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Identity.Client;
using SequentialGuid;
using System.Net;
using System.Transactions;

namespace DoAnTotNghiep.Services.Service
{
    public class QuestionService :IQuestionService
    {
        private readonly IQuestionRepository questionRepository;
        private readonly IAnswerRepository answerRepository;
        private readonly IMapper mapper;
        private readonly DataContext dataContext;

        public QuestionService(IQuestionRepository questionRepository , IMapper mapper,DataContext dataContext
            ,IAnswerRepository answerRepository)
        {
            this.questionRepository = questionRepository;
            this.mapper = mapper;
            this.dataContext = dataContext;
            this.answerRepository = answerRepository;
        }

        public void Validate(RequestQuestion request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request), "Request không được null.");

            if (string.IsNullOrWhiteSpace(request.Content))
                throw new ArgumentException("Nội dung câu hỏi không được để trống.");
           
            if (request.Answers == null || !request.Answers.Any())
                throw new ArgumentException("Phải có ít nhất một đáp án.");

            if (request.Answers.Any(a => string.IsNullOrWhiteSpace(a.Content)))
                throw new ArgumentException("Nội dung đáp án không được để trống.");

            if (!request.Answers.Any(a => a.IsCorrect))
                throw new ArgumentException("Phải có ít nhất một đáp án đúng.");

            //var duplicateIds = request.Answers.GroupBy(a => a.AnswerId)
            //                                  .Where(g => g.Count() > 1)
            //                                  .Select(g => g.Key)
            //                                  .ToList();
            //if (duplicateIds.Any())
            //    throw new ArgumentException("Có đáp án bị trùng AnswerId.");
        }

        public async Task UpdateQuestionAsync(Guid questionId, RequestQuestion questionUpdate)
        {
            Validate(questionUpdate);
            using var transation = await dataContext.Database.BeginTransactionAsync();
            try
            {
                var question = mapper.Map<Question>(questionUpdate);
                Console.WriteLine(question);
                await questionRepository.UpdateQuestionAsync(questionId, question);

                var answers = await answerRepository.GetAnswersAsync(questionId);
                foreach(var answer in answers)
                {
                    var answerUpdate = questionUpdate.Answers!.FirstOrDefault(x=> x.AnswerId == answer.AnswerId);
                    if(answerUpdate == null)
                    {
                        throw new KeyNotFoundException($"{answer.AnswerId}");
                    }
                    answer.Content = answerUpdate.Content;
                    answer.IsCorrect = answerUpdate.IsCorrect;
                }

                await answerRepository.UpdateListAnswerAsync(answers);
                await questionRepository.SaveChangesAsync();
                await transation.CommitAsync();
            }
            catch(Exception)
            {
                await transation.RollbackAsync();
                throw;
            }
        }

        public async Task AddQuestionAsync(RequestQuestion requestQuestion)
        {
            Validate(requestQuestion);

            // TODO: Thêm logic tạo entity Question và Answer, rồi lưu vào DB

            using var transaction = await dataContext.Database.BeginTransactionAsync();

            try
            {
                var question = mapper.Map<Question>(requestQuestion);
                question.QuestionId = (Guid)SequentialSqlGuidGenerator.Instance.NewSqlGuid();

                await questionRepository.AddQuesitonAsync(question);

                var answers = mapper.Map<List<Answer>>(requestQuestion.Answers);
                foreach (var answer in answers)
                {
                    answer.AnswerId = (Guid)SequentialSqlGuidGenerator.Instance.NewSqlGuid();
                    answer.QuestionId = question.QuestionId;
                }

                await answerRepository.AddListAnswerAsync(answers);
                await questionRepository.SaveChangesAsync();
                await transaction.CommitAsync();

            }
            catch(Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }

        }

        public async Task<List<QuestionDto>> GetListQuestionByExamId(Guid examId)
        {
            var questions = await questionRepository.GetListQuestionByExamId(examId);

            var questionDto = mapper.Map<List<QuestionDto>>(questions);
            return questionDto;

        }

        public async Task<QuestionDto> GetQuestionDetailByQuestionId(Guid questionId)
        {
            var question = await questionRepository.GetDetailQuestionAsync(questionId);
            var questionDto = mapper.Map<QuestionDto>(question);
            return questionDto;
        }

        public async Task<List<QuestionUserDto>> GetListQuestionByExamIdUser(Guid examId)
        {
            var questions = await questionRepository.GetListQuestionByExamId(examId);

            var questionsDto = mapper.Map<List<QuestionUserDto>>(questions);
            return questionsDto;
        }

        public async Task DeleteQuestion(Guid questionId)
        {
            await questionRepository.DeleteQuestionAsync(questionId);
            await questionRepository.SaveChangesAsync();
        }

        public async Task<List<ErrorQuestionAdd>> AddListQuestionAsync(List<RequestQuestion> requestQuestions, Guid examId)
        { 
            var questionError = new List<ErrorQuestionAdd>();
            try
            {
                foreach (var question in requestQuestions)
                {
                    try
                    {
                        Validate(question);
                        question.ExamId = examId;
                        await AddQuestionAsync(question);
                    }
                    catch (Exception ex)
                    {
                        var error = new ErrorQuestionAdd
                        {
                            QuestionContent = question.Content,
                        };
                        questionError.Add(error);
                        continue;
                    }

                }

                return questionError;
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi thêm danh sách câu hỏi", ex);
            }
        }
    }
}
