namespace risk.control.system.Models.ViewModel
{
    public class BoardingData
    {
        public string? PersonAddressImage { get; set; }
        public string? PhotoIdMapPath { get; set; }
        public string? PhotoIdPath { get; set; }
        public string? PhotoIdMapUrl { get; set; }

        public string? PanPhotoPath { get; set; }
        public string? PanMapPath { get; set; }
        public string? PanMapUrl { get; set; }
        public string FaceMatchStatus { get; set; }
        public string PersonName { get; set; }
        public string Salutation { get; set; }
        public string PersonContact { get; set; }
        public DateTime BoardingTill { get; set; }
        public DateTime PhotoIdTime { get; set; }
        public string WeatherData { get; set; }
        public string ArrivalAirport { get; set; }
        public string ArrivalAbvr { get; set; }
        public string Class { get; set; }
        public string ClassAdd { get; set; }
        public string PhotoIdRemarks { get; set; }

        public override string ToString()
        {
            return "BoardingData{" +
                    "Flight=" + FaceMatchStatus +
                    ", DepartureAirport=" + PersonName +
                    ", DepartureAbvr=" + Salutation +
                    ", BoardingGate=" + PersonContact +
                    ", BoardingTill=" + BoardingTill +
                    ", DepartureTime=" + PhotoIdTime +
                    ", Arrival=" + WeatherData +
                    ", ArrivalAirport=" + ArrivalAirport +
                    ", ArrivalAbvr=" + ArrivalAbvr +
                    ", Class=" + Class +
                    ", ClassAdd=" + ClassAdd +
                    ", Seat=" + PhotoIdRemarks +
                     "}";
        }
    }
}