namespace risk.control.system.Models
{
    public class PassportOutPut
    {
        public SourceOutput source_output { get; set; } = default!;
    }

    public class PassportResult
    {
        public string action { get; set; } = default!;
        public DateTime completed_at { get; set; }
        public DateTime created_at { get; set; }
        public string group_id { get; set; } = default!;
        public string request_id { get; set; } = default!;
        public PassportOutPut result { get; set; } = default!;
        public string status { get; set; } = default!;
        public string task_id { get; set; } = default!;
        public string type { get; set; } = default!;
    }

    public class SourceOutput
    {
        public object application_date { get; set; } = default!;
        public object date_of_birth { get; set; } = default!;
        public object file_number { get; set; } = default!;
        public object name { get; set; } = default!;
        public object passport_status { get; set; } = default!;
        public string status { get; set; } = default!;
        public object surname { get; set; } = default!;
    }
}
