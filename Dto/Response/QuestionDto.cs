using DoAnTotNghiep.Model;
using System.ComponentModel.DataAnnotations;

namespace DoAnTotNghiep.Dto.Response
{
    public class QuestionDto
    {
        public Guid QuestionId { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public Guid ExamId { get; set; }

        public List<AnswerDto>? AnswersDto { get; set; }
    }
}
