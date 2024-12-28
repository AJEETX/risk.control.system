using CsvHelper.Configuration;
using risk.control.system.Models.ViewModel;
using risk.control.system.Models;
using System.Globalization;
using CsvHelper;
using risk.control.system.Data;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text.RegularExpressions;
using Google.Api;

namespace risk.control.system.Seeds
{
    public static class PinCodeStateSeed
    {
        private static string au_stateWisePincodeFilePath = @"au_postcodes.csv";
        private static string in_stateWisePincodeFilePath = @"india_pincodes.csv";
        private static string all_india_pincodes = @"india_pincode.csv";

        //private static string stateWisePincodeFilePath = @"pincode.csv";
        private static string NO_DATA = " NO - DATA ";

        private static Regex regex = new Regex("\\\"(.*?)\\\"");

        public static async Task SeedPincode(ApplicationDbContext context, List<PinCodeState> pincodes, Country country)
        {
            // add the states with pincodes
            var states = pincodes.GroupBy(g => new { g.StateName, g.StateCode });
            foreach (var state in states)
            {
                var recordState = new State { Code = state.Key.StateCode, Name = state.Key.StateName, Country = country, Updated = DateTime.Now };
                var stateAdded = await context.State.AddAsync(recordState);

                var districts = state.GroupBy(g => g.District);

                var pinCodeList = new List<PinCode> { };
                foreach (var district in districts)
                {
                    var districtDetail = new District { Code = district.Key, Name = district.Key, State = stateAdded.Entity, Country = country, Updated = DateTime.Now };
                    var districtAdded = await context.District.AddAsync(districtDetail);
                    foreach (var pinCode in district)
                    {
                        var pincodeState = new PinCode
                        {
                            Name = pinCode.Name,
                            Code = pinCode.Code,
                            Longitude = pinCode.Longitude,
                            Latitude = pinCode.Latitude,
                            District = districtAdded.Entity,
                            State = stateAdded.Entity,
                            Country = country,
                            Updated = DateTime.Now,
                        };
                        pinCodeList.Add(pincodeState);
                    }
                }
                await context.PinCode.AddRangeAsync(pinCodeList);
            }
        }

        public static async Task SeedPincode_India(ApplicationDbContext context)
        {
            var country = new Country
            {
                Name = "INDIA",
                Code = "IND",
                Updated = DateTime.Now,
            };

            var indiaCountry = await context.Country.AddAsync(country);
            var pincodes = await CsvRead_India();

            try
            {
                // add the states with pincodes
                var states = pincodes.GroupBy(g => new { g.StateName, g.StateCode });
                foreach (var state in states)
                {
                    var recordState = new State { Code = state.Key.StateCode, Name = state.Key.StateName, Country = country, Updated = DateTime.Now };
                    var stateAdded = await context.State.AddAsync(recordState);

                    var districts = state.GroupBy(g => g.District);

                    var pinCodeList = new List<PinCode> { };
                    foreach (var district in districts)
                    {
                        var districtDetail = new District { Code = district.Key, Name = district.Key, State = stateAdded.Entity, Country = country, Updated = DateTime.Now };
                        var districtAdded = await context.District.AddAsync(districtDetail);
                        foreach (var pinCode in district)
                        {
                            var pincodeState = new PinCode
                            {
                                Name = pinCode.Name,
                                Code = pinCode.Code,
                                Longitude = pinCode.Longitude,
                                Latitude = pinCode.Latitude,
                                District = districtAdded.Entity,
                                State = stateAdded.Entity,
                                Country = country,
                                Updated = DateTime.Now
                            };
                            pinCodeList.Add(pincodeState);
                        }
                    }
                    await context.PinCode.AddRangeAsync(pinCodeList);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        
        public static async Task<Country> India(ApplicationDbContext context)
        {
            var country = new Country
            {
                Name = "INDIA",
                Code = "IND",
                Updated = DateTime.Now,
            };

            var indiaCountry = await context.Country.AddAsync(country);
            return indiaCountry.Entity;

        }

        public static async Task<Country> Australia(ApplicationDbContext context)
        {

            var country = new Country
            {
                Name = "AUSTRALIA",
                Code = "AU",
                Updated = DateTime.Now,
            };
            var australiaCountry = await context.Country.AddAsync(country);
            return australiaCountry.Entity;

        }

        public static async Task<List<PinCodeState>> CsvRead_Au()
        {
            var pincodes = new List<PinCodeState>();
            string csvData = await File.ReadAllTextAsync(au_stateWisePincodeFilePath);

            bool firstRow = true;
            foreach (string row in csvData.Split('\n'))
            {
                if (!string.IsNullOrEmpty(row))
                {
                    if (!string.IsNullOrEmpty(row))
                    {
                        if (firstRow)
                        {
                            firstRow = false;
                        }
                        else
                        {
                            var output = regex.Replace(row, m => m.Value.Replace(',', '@'));
                            var rowData = output.Split(',').ToList();

                            var pincodeState = new PinCodeState
                            {
                                Code = rowData[0] ?? NO_DATA,
                                Name = rowData[1] ?? NO_DATA,
                                District = rowData[1] ?? NO_DATA,
                                StateName = rowData[2] ?? NO_DATA,
                                StateCode = rowData[3] ?? NO_DATA,
                                Latitude = rowData[4] ?? NO_DATA,
                                Longitude = rowData[5] ?? NO_DATA,
                            };
                            var isDupicate = pincodes.FirstOrDefault(p => p.Code == pincodeState.Code);
                            pincodes.Add(pincodeState);
                        }
                    }
                }
            }
            return pincodes
                .Where(p => p.StateCode == "VIC"
                || p.StateCode == "NSW"
                )?.ToList();
        }

        public static async Task<List<PinCodeState>> CsvRead_India()
        {
            var pincodes = new List<PinCodeState>();
            string csvData = await File.ReadAllTextAsync(all_india_pincodes);

            bool firstRow = true;
            foreach (string row in csvData.Split('\n'))
            {
                if (!string.IsNullOrEmpty(row))
                {
                    if (!string.IsNullOrEmpty(row))
                    {
                        if (firstRow)
                        {
                            firstRow = false;
                        }
                        else
                        {
                            var output = regex.Replace(row, m => m.Value.Replace(',', '@'));
                            var rowData = output.Split(',').ToList();
                            var pincodeState = new PinCodeState
                            {
                                Name = rowData[1] ?? NO_DATA,
                                Code = rowData[2] ?? NO_DATA,
                                District = rowData[3] ?? NO_DATA,
                                StateName = rowData[4] ?? NO_DATA,
                                StateCode = GetInitials(rowData[4]) ?? NO_DATA,
                                Latitude =  NO_DATA,
                                Longitude = NO_DATA,
                            };
                            pincodes.Add(pincodeState);
                        }
                    }
                }
            }
            return pincodes
                .Where(p=>p.StateName.ToLower().StartsWith("haryana")
                || p.StateName.ToLower().StartsWith("delhi")
                //|| p.StateName.ToLower().StartsWith("uttar pradesh")
                )?.ToList();
        }
        static string GetInitials(string input)
        {
            // Trim any extra spaces and split the string into words by space
            string[] words = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            string initials = string.Empty;

            if (words.Length == 1)
            {
                // If only one word, take the first two letters (if available)
                initials = words[0].Substring(0, Math.Min(2, words[0].Length)).ToUpper();
            }
            else if (words.Length == 2)
            {
                // If two words, take the first letter of each word
                initials = words[0].Substring(0, 1).ToUpper() + words[1].Substring(0, 1).ToUpper();
            }
            else if (words.Length > 2)
            {
                // If more than two words, take the first letter of the first two words
                initials = words[0].Substring(0, 1).ToUpper() + words[1].Substring(0, 1).ToUpper();
            }

            return initials;
        }
    }
}