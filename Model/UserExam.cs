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

        public User? User { get; set; }

        [Required]
        public Guid ExamId { get; set; }

        public Exam? Exam { get; set; }

        public DateTime? StartTime { get; set; } 

        public DateTime? SubmitTime { get; set; } // Thời điểm nộp bài

        public double? Score { get; set; } // Điểm số nếu đã chấm

        public bool IsSubmitted { get; set; } = false;

        public bool IsStarted { get; set; } = false;
    }
}
