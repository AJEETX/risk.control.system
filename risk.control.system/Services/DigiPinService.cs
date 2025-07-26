namespace risk.control.system.Services
{
    public static class DigiPinEncoder
    {
        private static readonly char[,] DIGIPIN_GRID =
        {
        { 'F', 'C', '9', '8' },
        { 'J', '3', '2', '7' },
        { 'K', '4', '5', '6' },
        { 'L', 'M', 'P', 'T' }
    };

        private const double MinLat = 2.5;
        private const double MaxLat = 38.5;
        private const double MinLon = 63.5;
        private const double MaxLon = 99.5;

        public static string GetDigiPin(double lat, double lon)
        {
            if (lat < MinLat || lat > MaxLat)
                throw new ArgumentOutOfRangeException(nameof(lat), "Latitude out of range");
            if (lon < MinLon || lon > MaxLon)
                throw new ArgumentOutOfRangeException(nameof(lon), "Longitude out of range");

            double minLat = MinLat, maxLat = MaxLat;
            double minLon = MinLon, maxLon = MaxLon;
            string digiPin = "";

            for (int level = 1; level <= 10; level++)
            {
                double latDiv = (maxLat - minLat) / 4.0;
                double lonDiv = (maxLon - minLon) / 4.0;

                int row = 3 - (int)Math.Floor((lat - minLat) / latDiv);
                int col = (int)Math.Floor((lon - minLon) / lonDiv);

                row = Math.Clamp(row, 0, 3);
                col = Math.Clamp(col, 0, 3);

                digiPin += DIGIPIN_GRID[row, col];

                if (level == 3 || level == 6)
                    digiPin += '-';

                // Update bounds
                maxLat = minLat + latDiv * (4 - row);
                minLat = minLat + latDiv * (3 - row);
                minLon = minLon + lonDiv * col;
                maxLon = minLon + lonDiv;
            }

            return digiPin;
        }

        public static (double Latitude, double Longitude) GetLatLngFromDigiPin(string digiPin)
        {
            string pin = digiPin.Replace("-", "");
            if (pin.Length != 10)
                throw new ArgumentException("Invalid DIGIPIN");

            double minLat = MinLat, maxLat = MaxLat;
            double minLon = MinLon, maxLon = MaxLon;

            foreach (char ch in pin)
            {
                bool found = false;
                int ri = -1, ci = -1;

                for (int r = 0; r < 4 && !found; r++)
                {
                    for (int c = 0; c < 4; c++)
                    {
                        if (DIGIPIN_GRID[r, c] == ch)
                        {
                            ri = r;
                            ci = c;
                            found = true;
                            break;
                        }
                    }
                }

                if (!found)
                    throw new ArgumentException($"Invalid character in DIGIPIN: {ch}");

                double latDiv = (maxLat - minLat) / 4.0;
                double lonDiv = (maxLon - minLon) / 4.0;

                double lat1 = maxLat - latDiv * (ri + 1);
                double lat2 = maxLat - latDiv * ri;
                double lon1 = minLon + lonDiv * ci;
                double lon2 = minLon + lonDiv * (ci + 1);

                minLat = lat1;
                maxLat = lat2;
                minLon = lon1;
                maxLon = lon2;
            }

            double centerLat = (minLat + maxLat) / 2.0;
            double centerLon = (minLon + maxLon) / 2.0;

            return (Math.Round(centerLat, 6), Math.Round(centerLon, 6));
        }
    }
}
