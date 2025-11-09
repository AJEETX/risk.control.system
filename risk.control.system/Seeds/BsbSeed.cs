using System.Text;

using risk.control.system.Data;
using risk.control.system.Models;
namespace risk.control.system.Seeds
{
    public static class BsbSeed
    {
        private static string filePath = @"BSB.csv";
        private static string lookupFilePath = @"BSB2Name.csv";

        public static async Task LoadBsbData(ApplicationDbContext context)
        {
            var lookupList = LoadBsb2Name();
            var list = new List<BsbInfo>();
            var lines = File.ReadAllLines(filePath, Encoding.UTF8).Skip(0); // no header

            foreach (var line in lines) // no header
            {
                var parts = line.Split(',').Select(p => p.Trim().Trim('"')).ToArray(); // remove spaces and quotes

                if (parts.Length < 7) continue;
                var bankName = lookupList.FirstOrDefault(x => x.BSBOwner == parts[1].Trim())?.OwnerName;
                list.Add(new BsbInfo
                {
                    BSB = parts[0].Trim().Replace("-", ""),
                    BankCode = parts[1].Trim().ToUpper(),
                    Bank = bankName,
                    Branch = parts[2].Trim(),
                    Address = parts[3].Trim(),
                    City = parts[4].Trim(),
                    State = parts[5].Trim(),
                    Postcode = parts[6].Trim()
                });
            }
            await context.BsbInfo.AddRangeAsync(list);
            await context.SaveChangesAsync();
        }

        public static List<BsbLookUp> LoadBsb2Name()
        {
            var list = new List<BsbLookUp>();

            var lines = File.ReadAllLines(lookupFilePath, Encoding.UTF8).Skip(1); // skip header
            foreach (var line in lines) // skip header
            {
                var parts = line.Split(','); // tab-separated
                if (parts.Length < 3) continue;

                list.Add(new BsbLookUp
                {
                    BSBOwner = parts[0].Trim(),
                    OwnerName = parts[1].Trim(),
                    BSBPrefix = parts[2].Trim(),
                });
            }

            return list;
        }
    }
}
