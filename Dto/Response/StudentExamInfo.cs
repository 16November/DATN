namespace DoAnTotNghiep.Dto.Response
{
    public class StudentExamInfo
    {
        public Guid UserId { get; set; }

        public string MSSV { get; set; }
        public string FullName { get; set; } =string.Empty;

        public bool IsStarted { get; set; }


    }
}
