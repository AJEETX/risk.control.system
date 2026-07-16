namespace risk.control.system.Models.ViewModel
{
    public class ErrorDetailsViewModelList
    {
        public List<ErrorDetailsViewModel>? ErrorDetails { get; set; }
        public string FileName { get; set; } = default!;
    }
    public class ErrorDetailsViewModel
    {
        public DateTime? Timestamp { get; set; }
        public string? Level { get; set; } = default!;
        public string? MessageTemplate { get; set; }
        public string? TraceId { get; set; }
        public string? SpanId { get; set; }
        public ErrorProperties? ErrorProperties { get; set; }
        public string? Exception { get; set; }
    }
    public class EventId
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
    }

    public class ErrorProperties
    {
        public string? Number { get; set; }
        public string? Message { get; set; }
        public string? SourceContext { get; set; }
        public string? ActionId { get; set; }
        public string? ActionName { get; set; }
        public string? RequestId { get; set; }
        public string? RequestPath { get; set; }
        public string? Application { get; set; }
        public EventId? EventId { get; set; }
    }
}
