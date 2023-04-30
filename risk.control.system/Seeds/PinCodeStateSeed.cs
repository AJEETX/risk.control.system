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
        private static string stateWisePincodeFilePath = @"pincode-sample.csv";
        public static async Task SeedPincode(ApplicationDbContext context, Country country)
        {

            var pincodes = await CsvRead();

            // add the states with pincodes
            var states = pincodes.GroupBy(g => g.State);
            foreach (var state in states)
            {
                var recordState = new State { Code = state.Key, Name = state.Key, CountryId = country.CountryId };
                var stateAdded = await context.State.AddAsync(recordState);

                var districts = state.GroupBy(g => g.District);

                var pinCodeList = new List<PinCode> { };
                foreach (var district in districts)
                {
                    var districtDetail = new District {  Code = district.Key, Name = district.Key, StateId = stateAdded.Entity.StateId, CountryId = country.CountryId };
                    var districtAdded = await context.District.AddAsync(districtDetail);
                    foreach (var pinCode in district)
                    {
                        var pincodeState = new PinCode
                        {
                            Name = pinCode.Code,
                            DistrictId = districtAdded.Entity.DistrictId,
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
                                Code = rowData[0],
                                District = rowData[1],
                                State = rowData[2].Substring(0, rowData[2].Length - 1)
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
