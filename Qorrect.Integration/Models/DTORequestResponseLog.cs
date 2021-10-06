namespace Qorrect.Integration.Models
{
    public class DTORequestResponseLog
    {
        public int ErrorQuestionID { get; set; }
        public string RequestUri { get; set; }
        public string logRequest { get; set; }
        public string logResponse { get; set; }
        public int CourseID { get; set; }
        public int QuestionID { get; set; }
        public string Device { get; set; }
        public string StatusCode { get; set; }
        public string CourseName { get; set; }
    }
}
