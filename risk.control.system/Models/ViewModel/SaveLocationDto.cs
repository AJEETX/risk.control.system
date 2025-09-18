namespace risk.control.system.Models.ViewModel
{
    public class SaveLocationDto
    {
        public long LocationId { get; set; }
        public ChildSelectionDto AgentId { get; set; }
        public List<ChildSelectionDto> FaceIds { get; set; } = new();
        public List<ChildSelectionDto> DocumentIds { get; set; } = new();
        public List<ChildSelectionDto> MediaReports { get; set; } = new();
    }

    public class ChildSelectionDto
    {
        public long Id { get; set; }
        public bool Selected { get; set; }
    }
}
