using System.Data;

namespace risk.control.system.Models.ViewModel
{
    public class PinCodeDetails
    {
        public DataTable DataTable { get; set; }
    }
    public class PinCodeState
    {
        public string Code { get; set; }
        public string District { get; set; }
        public string State { get; set; }
    }
}
