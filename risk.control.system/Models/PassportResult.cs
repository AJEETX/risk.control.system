namespace risk.control.system.Models
{
    public class PassportOutPut
    {
        public SourceOutput source_output { get; set; }
    }

    public class PassportResult
    {
        public string action { get; set; }
        public DateTime completed_at { get; set; }
        public DateTime created_at { get; set; }
        public string group_id { get; set; }
        public string request_id { get; set; }
        public PassportOutPut result { get; set; }
        public string status { get; set; }
        public string task_id { get; set; }
        public string type { get; set; }
    }

    public class SourceOutput
    {
        public object application_date { get; set; }
        public object date_of_birth { get; set; }
        public object file_number { get; set; }
        public object name { get; set; }
        public object passport_status { get; set; }
        public string status { get; set; }
        public object surname { get; set; }
    }
}
