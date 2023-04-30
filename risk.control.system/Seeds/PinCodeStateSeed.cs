using CsvHelper.Configuration;
using risk.control.system.Models.ViewModel;
using risk.control.system.Models;
using System.Globalization;
using CsvHelper;
using risk.control.system.Data;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace risk.control.system.Seeds
{
    public static class PinCodeStateSeed
    {
        private static string stateWisePincodeFilePath = @"pincode-dataset.csv";
        public static async Task SeedPincode(ApplicationDbContext context, Country country)
        {

            var pincodes = await CsvRead();

            // add the states with pincodes
            var states = pincodes.GroupBy(g => g.State);
            foreach (var state in states)
            {
                var pinCodeList = new List<PinCode> { };
                {
                    var recordState = new State { Code = state.Key, Name = state.Key, Country = country };
                    var stateAdded = await context.State.AddAsync(recordState);
                    foreach (var pincode in state)
                    {
                        var pincodeState = new PinCode
                        {
                            Name = pincode.Name,
                            Code = pincode.Code,
                            StateId = stateAdded.Entity.StateId,
                            CountryId = country.CountryId,
                        };
                        pinCodeList.Add(pincodeState);
                    }
                }
                await context.PinCode.AddRangeAsync(pinCodeList);
            }



        }
        private static async Task<List<PinCodeState>> CsvRead()
        {
            var pincodes = new List<PinCodeState>();
            string csvData = await System.IO.File.ReadAllTextAsync(stateWisePincodeFilePath);

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
                            var rowData = row.Split(',');
                            var pincodeState = new PinCodeState
                            {
                                Name = rowData[1],
                                Code = rowData[0],
                                State = rowData[2]
                            };
                            pincodes.Add(pincodeState);
                        }
                    }
                }
            }
            return pincodes;
        }
    }
}
