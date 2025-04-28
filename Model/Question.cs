using System.ComponentModel.DataAnnotations;

namespace DoAnTotNghiep.Model
{
    public class Question
    {
        [Key]
        public Guid QuestionId { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public Guid ExamId { get; set; }

        public Exam? Exam { get; set; }

        public ICollection<Answer>? Answers { get; set; }
    }
}
