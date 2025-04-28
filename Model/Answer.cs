using System.ComponentModel.DataAnnotations;

namespace DoAnTotNghiep.Model
{
    public class Answer
    {
        [Key]
        public Guid AnswerId { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        [Required]
        public bool IsCorrect { get; set; }

        public Guid QuestionId { get; set; }

        public Question? Question { get; set; }
    }
}
