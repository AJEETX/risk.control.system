namespace risk.control.system.Models.ViewModel
{
    public class AudioTranscript
    {
        public string jobName { get; set; }
        public string accountId { get; set; }
        public string status { get; set; }
        public Results results { get; set; }
    }
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class Alternative
    {
        public string confidence { get; set; }
        public string content { get; set; }
    }

    public class AudioSegment
    {
        public int id { get; set; }
        public string transcript { get; set; }
        public string start_time { get; set; }
        public string end_time { get; set; }
        public List<int> items { get; set; }
    }

    public class Item
    {
        public int id { get; set; }
        public string type { get; set; }
        public List<Alternative> alternatives { get; set; }
        public string start_time { get; set; }
        public string end_time { get; set; }
    }

    public class Results
    {
        public List<Transcript> transcripts { get; set; }
        public List<Item> items { get; set; }
        public List<AudioSegment> audio_segments { get; set; }
    }

    public class Transcript
    {
        public string transcript { get; set; }
    }


}
