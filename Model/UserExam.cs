using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DoAnTotNghiep.Model
{
    public class UserExam
    {
        [Key]
        public Guid UserExamId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        public virtual User? User { get; set; }

        [Required]
        public Guid ExamId { get; set; }

        public virtual Exam? Exam { get; set; }

        public DateTime? StartTime { get; set; }
        public DateTime? SubmitTime { get; set; }
        public double? Score { get; set; }
        public bool IsSubmitted { get; set; } = false;
        public bool IsStarted { get; set; } = false;
    }
}
