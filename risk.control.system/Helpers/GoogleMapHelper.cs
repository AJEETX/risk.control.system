namespace risk.control.system.Helpers
{
    public static class GoogleMapHelper
    {
        public static string GetStaticMapUrl(
            int mapHeight,
            int mapWidth,
            string startColor,
            string startLbl,
            double startLat,
            double startLong,
            string endColor,
            string endLbl,
            double endLat,
            double endLong,
            string encodedPolyline)
        {
            string apiKey = Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY");

            return string.Format(
                "https://maps.googleapis.com/maps/api/staticmap?size={0}x{1}&markers=color:{2}|label:{3}|{4},{5}&markers=color:{6}|label:{7}|{8},{9}&path=enc:{10}&key={11}",
                mapHeight,
                mapWidth,
                Uri.EscapeDataString(startColor),
                Uri.EscapeDataString(startLbl),
                startLat,
                startLong,
                Uri.EscapeDataString(endColor),
                Uri.EscapeDataString(endLbl),
                endLat,
                endLong,
                Uri.EscapeDataString(encodedPolyline),
                Uri.EscapeDataString(apiKey)
            );
        }
    }
}
