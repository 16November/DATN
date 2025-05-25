namespace DoAnTotNghiep.Dto.Response
{
    public class StudentExam
    {
        public string MSSV { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public double score { get; set; } = 0;

        public bool IsSubmitted { get; set; } 
    }
}
