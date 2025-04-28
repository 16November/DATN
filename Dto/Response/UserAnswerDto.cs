using DoAnTotNghiep.Model;
using System.ComponentModel.DataAnnotations;

namespace DoAnTotNghiep.Dto.Response
{
    public class UserAnswerDto
    {
        public Guid UserAnswerId { get; set; }

        public Guid AnswerId { get; set; }

        public DateTime CreatedAt { get; set; }

        public Guid UserId { get; set; }

        public Guid QuestionId { get; set; }

        public Question? Question { get; set; }

        public AnswerDto? AnswerDto { get; set; }
    }
}
