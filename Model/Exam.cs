using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;

namespace DoAnTotNghiep.Model
{
    public class Exam
    {
        [Key]
        public Guid ExamId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Range(1, 1000)]
        public int DurationInMinutes { get; set; } = 60;

        public bool IsPublished { get; set; } = false;
        public DateTime StartDay { get; set; }

        [Required]
        public Guid CreatedByUserId { get; set; }

        [ForeignKey("CreatedByUserId")]
        public virtual User? CreatedBy { get; set; }

        public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
        public virtual ICollection<UserExam> UserExams { get; set; } = new List<UserExam>();
    }
}
