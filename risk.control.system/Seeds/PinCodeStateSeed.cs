using System.Text.RegularExpressions;

using risk.control.system.Data;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Seeds
{
    public static class PinCodeStateSeed
    {
        private static Regex regex = new Regex("\\\"(.*?)\\\"");
        private static readonly Regex officeSuffixRegex = new(@"[\s\(\[]*B[\.\s]*O[\)\]]*$", RegexOptions.IgnoreCase);
        private static string currenciesFilePath = @"lang-currency.csv";
        private static string currenciesNameFilePath = @"currency.csv";
        private static string countriesFilePath = @"countries.csv";
        private static string au_stateWisePincodeFilePath = @"au_postcodes.csv";
        private static string all_india_pincodes = @"india_pincode_full.csv";
        private static string NO_DATA = " NO - DATA ";
        private static List<Currency> currencies = new List<Currency>();
        private static List<Currency> currenciesName = new List<Currency>();

        public static async Task SeedPincode(ApplicationDbContext context, List<PinCodeState> pincodes, Country country)
        {
            // add the states with pincodes
            var states = pincodes.GroupBy(g => new { g.StateName, g.StateCode });
            foreach (var state in states)
            {
                var dbState = new State { Code = state.Key.StateCode, Name = state.Key.StateName, Country = country, Updated = DateTime.Now };
                var stateAdded = await context.State.AddAsync(dbState);

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

        public static async Task<List<Country>> Countries(ApplicationDbContext context)
        {
            //GET ALL COUNTRIES FROM CSV
            string countries_csv = await File.ReadAllTextAsync(countriesFilePath);
            var countries = new List<Country>();

            bool firstRow = true;
            foreach (string row in countries_csv.Split('\n'))
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
                            var countryCode = rowData[1].Trim().ToLower();
                            var currency = currencies.FirstOrDefault(c => c.CountryCode.Trim().ToLower() == countryCode);

                            var currencyName = currenciesName.FirstOrDefault(c => c.CountryCode.Trim().ToLower() == countryCode);

                            var country = new Country
                            {
                                Name = rowData[0] ?? NO_DATA,
                                Code = rowData[1] ?? NO_DATA,
                                ISDCode = int.Parse(rowData[2].Trim()),
                                CurrencyCode = currency?.CurrencyCode ?? currencyName?.CurrencyCode,
                                CurrencyName = currencyName?.CurrencyName,
                                Language = currency?.Language.ToUpper(),
                                Updated = DateTime.Now,
                            };

                            countries.Add(country);
                        }
                    }
                }
            }
            context.Country.AddRange(countries);
            await context.SaveChangesAsync(null, false);
            return countries;
        }

        public static async Task CurrenciesCode(ApplicationDbContext context)
        {
            //GET ALL CURRENCIES FROM CSV
            string currencies_csv = await File.ReadAllTextAsync(currenciesFilePath);

            bool firstRow = true;
            foreach (string row in currencies_csv.Split('\n'))
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
                            var currency = new Currency
                            {
                                CountryCode = rowData[1].Trim('"'),
                                CurrencyCode = rowData[5].Trim('"'),
                                Language = rowData[6].Trim('"'),
                            };
                            currencies.Add(currency);
                        }
                    }
                }
            }
        }
        public static async Task Currencies(ApplicationDbContext context)
        {
            //GET ALL CURRENCIES FROM CSV
            string currencies_csv = await File.ReadAllTextAsync(currenciesNameFilePath);

            bool firstRow = true;
            foreach (string row in currencies_csv.Split('\n'))
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
                            var currency = new Currency
                            {
                                CountryCode = rowData[1].Trim('"'),
                                CurrencyName = rowData[2].Trim('"'),
                                CurrencyCode = rowData[3].Trim('"'),
                            };
                            currenciesName.Add(currency);
                        }
                    }
                }
            }
        }

        public static async Task<List<PinCodeState>> CsvRead_Au(int maxCount = 0)
        {
            var pincodes = new List<PinCodeState>();
            string csvData = await File.ReadAllTextAsync(au_stateWisePincodeFilePath);
            int rowCount = 0;
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
                                Code = rowData[0]?.Trim() ?? NO_DATA,
                                Name = rowData[1]?.Trim() ?? NO_DATA,
                                District = rowData[1]?.Trim() ?? NO_DATA,
                                StateName = rowData[2]?.Trim() ?? NO_DATA,
                                StateCode = rowData[3]?.Trim() ?? NO_DATA,
                                Latitude = rowData[4]?.Trim() ?? NO_DATA,
                                Longitude = rowData[5]?.Trim() ?? NO_DATA,
                            };
                            var isDupicate = pincodes.FirstOrDefault(p => p.Code == pincodeState.Code);
                            pincodes.Add(pincodeState);
                            rowCount++;
                            if (maxCount > 0 && rowCount >= maxCount)
                            {
                                break;
                            }
                        }
                    }
                }
            }
            return pincodes;
        }

        public static async Task<List<PinCodeState>> CsvRead_IndiaAsync()
        {
            try
            {

                var pincodes = new List<PinCodeState>();

                // Read all lines from file
                var lines = await File.ReadAllLinesAsync(all_india_pincodes);

                // Skip header row
                foreach (var line in lines.Skip(1))
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // Split by TAB instead of comma
                    var parts = line.Split(',');

                    if (parts.Length >= 4)
                    {
                        var officeName = officeSuffixRegex.Replace(parts[0].Trim(), "").Trim('"');

                        var pincode = parts[1].Trim('"');
                        var district = parts[2].Trim('"').ToUpperInvariant();
                        var stateName = parts[3].Trim('"').ToUpperInvariant();
                        var stateCode = GetInitials(stateName);

                        pincodes.Add(new PinCodeState
                        {
                            Name = officeName.Replace("B.O", "").Replace("BO", "").Replace("SO", "").Replace("S.O", "").Replace("S.O.", ""),
                            Code = pincode,
                            District = district,
                            StateName = stateName,
                            StateCode = stateCode,
                            Latitude = "N/A",
                            Longitude = "N/A"
                        });
                    }
                }

                return pincodes;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private static string GetInitials(string input)
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