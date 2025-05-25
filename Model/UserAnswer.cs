using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnTotNghiep.Model
{
    public class UserAnswer
    {
        [Key]
        public Guid UserAnswerId { get; set; }

        [Required]
        public Guid AnswerId { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid QuestionId { get; set; }

        public Guid ExamId { get; set; }
        [ForeignKey("QuestionId")]
        public Question? Question { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [ForeignKey("AnswerId")]
        public Answer? Answer { get; set; }
    }
}
