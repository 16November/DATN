using System.ComponentModel.DataAnnotations;

namespace DoAnTotNghiep.Model
{
    public class CheatingEvent
    {
        [Key]
        public Guid CheatingId { get; set; }

        public Guid UserId { get; set; }

        public Guid ExamId { get; set; }

        public int FocusEvent { get; set; }

        public int BlurEvent { get; set; }

        public int CopyEvent { get; set; }

        public int HiddenEvent { get; set; }

        public int MultiTabEvent { get; set; }

        public int CtrCEvent { get; set; }

        public int PageSwitchEvent { get; set; }
    }
}
