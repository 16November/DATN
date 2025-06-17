namespace DoAnTotNghiep.Dto.Response
{
    public class StudentExam
    {
        public Guid UserId { get; set; }
        public string MSSV { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public double score { get; set; } = 0;

        public bool IsSubmitted { get; set; } 

        public DateTime SubmitDay { get; set; } 

        public DateTime StartDat { get; set; }
    }
}
