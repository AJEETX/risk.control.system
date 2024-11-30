namespace risk.control.system.Models.ViewModel
{
    public class PassportOcrData
    {
        public string code { get; set; }
        public string status { get; set; }
        public Data data { get; set; }
    }
    public class Data
    {
        public PassportPosition position { get; set; }
        public Nation nation { get; set; }
        public Ocr ocr { get; set; }
        public Image image { get; set; }
    }

    public class Image
    {
        public string documentFrontSide { get; set; }
    }

    public class Nation
    {
        public string authority { get; set; }
        public string givenNames { get; set; }
        public string name { get; set; }
        public string nationality { get; set; }
        public string placeOfBirth { get; set; }
        public string sex { get; set; }
        public string surname { get; set; }
        public string surnameOfSpouse { get; set; }
    }

    public class Ocr
    {
        public string issuingStateCode { get; set; }
        public string dateOfBirth { get; set; }
        public string dateOfExpiry { get; set; }
        public string sex { get; set; }
        public string documentNumber { get; set; }
        public string documentClassCode { get; set; }
        public string surname { get; set; }
        public string givenNames { get; set; }
        public string name { get; set; }
    }

    public class PassportPosition
    {
        public int left { get; set; }
        public int bottom { get; set; }
        public int right { get; set; }
        public int top { get; set; }
    }

}
