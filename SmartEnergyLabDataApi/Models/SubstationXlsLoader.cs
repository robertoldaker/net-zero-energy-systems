using System.Diagnostics;
using ExcelDataReader;
using HaloSoft.EventLogger;
using SmartEnergyLabDataApi.Data;

namespace SmartEnergyLabDataApi.Models
{
    public class SubstationXlsLoader
    {
        private DataAccess _da;
        private DistributionNetworkOperator _dno;
        private GeographicalArea _ga;
        private SubstationCache _cache;

        public SubstationXlsLoader(DataAccess da,GeographicalArea ga)
        {
            _da = da;
            _ga = ga;
            _dno = ga.DistributionNetworkOperator;
            _cache = new SubstationCache(_da, _dno);
        }

        public void Load(IFormFile file)
        {            
            using (var stream = file.OpenReadStream()) {
                using (var reader = ExcelReaderFactory.CreateReader(stream)) {
                    // Choose one of either 1 or 2:
                    int sheetCount = 1;
                    // 1. Use the reader methods
                    do {
                        // Count of customers
                        if (sheetCount == 1) {
                            readCountOfCustomers(reader);
                        }
                        if (sheetCount == 2) {
                            readCountOfEACs(reader);
                        }
                        // Count of EAC records
                        else if (sheetCount == 3) {
                            readSumOfEAC(reader);
                        }
                        // HH estimates 
                        else if (sheetCount == 4) {
                            readHHEstimates(reader);
                        }
                        sheetCount++;
                    } while (reader.NextResult());

                }
            }
        }

        private void readCountOfCustomers(IExcelDataReader reader)
        {
            Logger.Instance.LogInfoEvent("Started reading count of customers");
            // Consume the header
            reader.Read();
            // Read data by row
            while (reader.Read()) {
                int col = 0;
                var externalId = reader.GetString(col++);
                var distName = reader.GetString(col++);
                var primaryId = reader.GetString(col++);
                var primaryName = reader.GetString(col++);
                // Ignore anything without these key fields filled in
                if (externalId == null || primaryId == null || string.IsNullOrEmpty(distName)) {
                    continue;
                }
                // Add primary substation if not already exists
                PrimarySubstation primarySubstation = _cache.GetPrimarySubstation(primaryId);
                // Create if we haven't got one
                //??if (primarySubstation == null) {
                //??    primarySubstation = new PrimarySubstation(primaryId, _ga, _dno);
                //??    _da.Substations.Add(primarySubstation);
                //??    _cache.Add(primarySubstation);
                //??}
                //??primarySubstation.Name = primaryName;

                // Add distribution substation
                DistributionSubstation distributionSubstation = _cache.GetDistributionSubstation(externalId);
                // Create and save distribution substation if we haven't got one
                if (distributionSubstation == null) {
                    distributionSubstation = new DistributionSubstation(externalId, primarySubstation);
                    _da.Substations.Add(distributionSubstation);
                    _cache.Add(distributionSubstation);
                }
                distributionSubstation.Name = distName;
                distributionSubstation.PrimarySubstation = primarySubstation;

                // Process customer classifications
                col++; // not sure about this column "<>"
                for (int classNum = 1; classNum <= 8; classNum++) {
                    var customerNum = reader.GetValue(col++);
                    if (customerNum != null) {
                        var ssClass = _cache.GetSubstationClassification(distributionSubstation, classNum);
                        if (ssClass == null) {
                            ssClass = new SubstationClassification(classNum, distributionSubstation);
                            _da.SubstationClassifications.Add(ssClass);
                            _cache.Add(ssClass);
                        }
                        ssClass.NumberOfCustomers = (int)((double)customerNum);
                    }
                }
            }
        }
        private void readCountOfEACs(IExcelDataReader reader)
        {
            Logger.Instance.LogInfoEvent("Started reading count of EACs");
            // Consume the header
            reader.Read();
            // Read data by row
            while (reader.Read()) {
                int col = 0;
                var externalId = reader.GetString(col++);
                var distName = reader.GetString(col++);
                var primaryId = reader.GetString(col++);
                col++;
                // Ignore anythiing without these key fields filled in
                if (externalId == null || primaryId == null || string.IsNullOrEmpty(distName)) {
                    continue;
                }
                DistributionSubstation distributionSubstation = _cache.GetDistributionSubstation(externalId);

                // Process customer classifications
                col++; // not sure about this column "<>"
                for (int classNum = 1; classNum <= 8; classNum++) {
                    var eacNum = reader.GetValue(col++);
                    if (eacNum != null) {
                        var ssClass = _cache.GetSubstationClassification(distributionSubstation, classNum);
                        if (ssClass != null) {
                            ssClass.NumberOfEACs = (int)((double)eacNum);
                        }
                    }
                }
            }
        }

        private void readSumOfEAC(IExcelDataReader reader)
        {
            Logger.Instance.LogInfoEvent("Started reading sum of EACs");
            // Consume the header
            reader.Read();
            // Read data by row
            while (reader.Read()) {
                int col = 0;
                var externalId = reader.GetString(col++);
                col += 3;
                // Get distribution substation from externalId
                DistributionSubstation distributionSubstation = _cache.GetDistributionSubstation(externalId);

                if (distributionSubstation != null) {
                    // Process customer classifications
                    col++; // not sure about this column "<>"
                    for (int classNum = 1; classNum <= 8; classNum++) {
                        var consumptionKwh = reader.GetValue(col++);
                        if (consumptionKwh != null) {
                            var ssClass = _cache.GetSubstationClassification(distributionSubstation, classNum);
                            if (ssClass != null) {
                                ssClass.ConsumptionKwh = (double)consumptionKwh;
                            }
                        }
                    }
                }
            }
        }

        private void readHHEstimates(IExcelDataReader reader)
        {
            Logger.Instance.LogInfoEvent("Started reading HH estimates");
            var ss1 = new Stopwatch();
            ss1.Start();
            // Consume the header
            reader.Read();
            // Read data by row
            int nRows = 1;
            int intervalMins = 30;
            int numData = (24 * 60) / intervalMins;
            var loads = new double[numData];
            while (reader.Read()) {
                int col = 3;
                var externalId = reader.GetString(col++);
                // Get distribution substation from externalId
                // ignore name
                col++;
                DistributionSubstation distributionSubstation = _cache.GetDistributionSubstation(externalId);
                if (distributionSubstation!=null) {
                    nRows++;
                    var month = reader.GetValue(col++);
                    var dayOfWeek = reader.GetString(col++);
                    if (month != null && month.GetType() == typeof(double) && dayOfWeek != null) {
                        int monthNumber = (int)(double)month;
                        Day dow = Day.Saturday;
                        if (string.Compare(dayOfWeek, "Saturday", true) == 0) {
                            dow = Day.Saturday;
                        }
                        else if (string.Compare(dayOfWeek, "Sunday", true) == 0) {
                            dow = Day.Sunday;
                        }
                        else if (string.Compare(dayOfWeek, "weekday", true) == 0) {
                            dow = Day.Weekday;
                        }
                        // See if it already exists
                        var sLoadProfile = _cache.GetSubstationLoadProfile(distributionSubstation, monthNumber, dow);
                        // Create if not
                        if (sLoadProfile == null) {
                            sLoadProfile = new SubstationLoadProfile(distributionSubstation);
                            _da.SubstationLoadProfiles.Add(sLoadProfile);
                            _cache.Add(sLoadProfile);
                        }
                        sLoadProfile.Day = dow;
                        sLoadProfile.MonthNumber = monthNumber;
                        sLoadProfile.IntervalMins = intervalMins; // Half hour
                        // Skip next 2 columns
                        col += 2;
                        sLoadProfile.Data = new double[numData];
                        for (int i = 0; i < 48; i++) {
                            var conKwH = reader.GetValue(col++);
                            if (conKwH != null) {
                                // Convert to Kw
                                loads[i] = (double)conKwH/((double)intervalMins/60);
                                sLoadProfile.Data[i] = loads[i];
                            }
                        }
                    }
                }
                // Just read first row??
                //if ( nRows>200 ) {
                //    break;
                //}
            }
            ss1.Stop();
        }
    }
}