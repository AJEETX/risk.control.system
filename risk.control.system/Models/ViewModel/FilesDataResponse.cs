namespace risk.control.system.Models.ViewModel
{
    public class FilesDataResponse
    {
        public int Draw { get; set; }
        public int RecordsTotal { get; set; }
        public int RecordsFiltered { get; set; }
        public bool MaxAssignReadyAllowed { get; set; }
        public object Data { get; set; }
    }

}
