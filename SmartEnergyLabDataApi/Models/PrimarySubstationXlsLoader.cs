using System.Diagnostics;
using ExcelDataReader;
using HaloSoft.EventLogger;
using SmartEnergyLabDataApi.Data;

namespace SmartEnergyLabDataApi.Models
{
    public class PrimarySubstationXlsLoader
    {
        private DataAccess _da;
        private DistributionNetworkOperator _dno;
        private GeographicalArea _ga;

        public PrimarySubstationXlsLoader(DataAccess da, GeographicalArea ga)
        {
            _da = da;
            _ga = ga;
            _dno = ga.DistributionNetworkOperator;
        }

        public void Load(IFormFile file)
        {
            using (var stream = file.OpenReadStream()) {
                using (var reader = ExcelReaderFactory.CreateCsvReader(stream)) {
                    // Choose one of either 1 or 2:
                    int sheetCount = 1;
                    // 1. Use the reader methods
                    do {
                        // Count of customers
                        if (sheetCount == 1) {
                            readPrimarySubstations(reader);
                        }
                        sheetCount++;
                    } while (reader.NextResult());

                }
            }
        }

        private void readPrimarySubstations(IExcelDataReader reader)
        {
            Logger.Instance.LogInfoEvent("Started reading primary substations");
            // Consume the header
            reader.Read();
            // Read data by row
            while (reader.Read()) {
                var primaryName = reader.GetString(2);
                var primaryId = reader.GetString(3);
                var assetType = reader.GetString(4);
                var latValue = reader.GetString(5);
                var longValue = reader.GetString(6);
                // Ignore anything without these key fields filled in
                if (primaryId == null || string.IsNullOrEmpty(primaryName) || assetType==null || assetType!="Primary") {
                    continue;
                }
                // Add primary substation if not already exists
                PrimarySubstation primarySubstation = _da.Substations.GetPrimarySubstation(primaryId);
                // Create if we haven't got one
                if (primarySubstation != null) {
                    primarySubstation.Name = primaryName;
                    primarySubstation.GISData.Latitude = double.Parse(latValue);
                    primarySubstation.GISData.Longitude = double.Parse(longValue);
                }
            }
        }
    }
}