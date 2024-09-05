using System.Diagnostics;
using System.Text.RegularExpressions;
using ExcelDataReader;
using HaloSoft.DataAccess;
using HaloSoft.EventLogger;
using SmartEnergyLabDataApi.Data;

namespace SmartEnergyLabDataApi.Elsi
{
    public class ElsiXlsmLoader
    {
        private DataAccess _da;
        private List<GenStoreData> _genDataList;
        private List<GenStoreData> _storeDataList;
        public ElsiXlsmLoader(DataAccess da)
        {
            _da = da;
        }

        public string Load(IFormFile file) {
            string msg = "";            
            using (var stream = file.OpenReadStream()) {
                using (var reader = ExcelReaderFactory.CreateReader(stream)) {
                    //
                    //

                    do {
                        var name = reader.Name;
                        //
                        if ( name=="Day") {
                            loadMiscData(reader);
                            loadGenData(reader);
                            loadStoreData(reader);
                            msg+=loadLinkData(reader) + "\n";
                        } else if ( name=="Generation") {
                            msg+=loadGenMiscData(reader) + "\n";
                            msg+=loadGenParameterData(reader) + "\n";
                            msg+=loadGenCapacityData(reader) + "\n";
                        } else if ( name == "Solar") {
                            msg+=loadAvailOrDemandData(reader,ElsiGenDataType.SolarAvail) + "\n";
                        } else if ( name == "Onshore") {
                            msg+=loadAvailOrDemandData(reader,ElsiGenDataType.OnShoreAvail) + "\n";
                        } else if ( name == "Offshore") {
                            msg+=loadAvailOrDemandData(reader,ElsiGenDataType.OffShoreAvail) + "\n";
                        } else if ( name == "Demands") {
                            msg+=loadPeakDemandData(reader)+"\n";
                            msg+=loadAvailOrDemandData(reader,ElsiGenDataType.Demands) + "\n";
                        }

                    } while (reader.NextResult());
                    // Need to reset as profilePeak overlaps with existing table
                    reader.Reset();
                    do {
                        var name = reader.Name;
                        //
                        if ( name == "Demands") {
                            msg+=loadProfilePeakData(reader)+"\n";
                        }

                    } while (reader.NextResult());
                }

            }
            return msg;
        }
        private void loadStoreData(IExcelDataReader reader) {
            moveToStartRow(reader,"StoreName", out int columnIndex);
            readStoreData(reader, columnIndex);
        }

        private void readStoreData(IExcelDataReader reader, int columnIndex) {
            _storeDataList = new List<GenStoreData>();
            while (reader.Read()) {
                var nameStr = reader.GetString(columnIndex);
                if ( string.IsNullOrEmpty(nameStr)) {
                    break;
                }
                var mZoneStr = reader.GetString(columnIndex+1);
                if ( !Enum.TryParse<ElsiMainZone>(mZoneStr,true, out ElsiMainZone mZone)) {
                    throw new Exception($"Unexpected main zone found [{mZoneStr}]");
                }
                var typeStr = reader.GetString(columnIndex+2);
                if ( !TryParseGenType(typeStr,out ElsiGenType type) ) {
                    throw new Exception($"Unexpected Generation type found [{typeStr}]");
                }
                _storeDataList.Add(new GenStoreData() { Zone = mZone, Type = type});
            }

        }

        private void loadMiscData(IExcelDataReader reader) {
            moveToStartRow(reader,"Parameter", out int columnIndex);
            readMiscData(reader, columnIndex);
        }

        private void readMiscData(IExcelDataReader reader, int columnIndex) {
            var mp = _da.Elsi.GetMiscParams();
            while (reader.Read()) {
                var nameStr = reader.GetString(columnIndex);
                if ( string.IsNullOrEmpty(nameStr)) {
                    break;
                }
                if ( nameStr == "Scenario") {
                    break;
                }
                if ( nameStr == "EUCO2") {
                    mp.EU_CO2 = reader.GetDouble(columnIndex+1);
                }
                if ( nameStr == "GBCO2") {
                    mp.GB_CO2 = reader.GetDouble(columnIndex+1);
                }
                if ( nameStr == "VLL") {
                    mp.VLL = reader.GetDouble(columnIndex+1);
                }
            }
        }

        private void loadGenData(IExcelDataReader reader) {
            moveToStartRow(reader,"GenName", out int columnIndex);
            readGenData(reader, columnIndex);
        }

        private void readGenData(IExcelDataReader reader, int columnIndex) {
            _genDataList = new List<GenStoreData>();
            while (reader.Read()) {
                var nameStr = reader.GetString(columnIndex);
                if ( string.IsNullOrEmpty(nameStr)) {
                    break;
                }
                var mZoneStr = reader.GetString(columnIndex+1);
                if ( !Enum.TryParse<ElsiMainZone>(mZoneStr,true, out ElsiMainZone mZone)) {
                    throw new Exception($"Unexpected main zone found [{mZoneStr}]");
                }
                var typeStr = reader.GetString(columnIndex+2);
                if ( !TryParseGenType(typeStr,out ElsiGenType type) ) {
                    throw new Exception($"Unexpected Generation type found [{typeStr}]");
                }
                _genDataList.Add(new GenStoreData() { Zone = mZone, Type = type});
            }

        }
        private string loadGenMiscData(IExcelDataReader reader) {
            moveToStartRow(reader, "£/€", out int columnIndex);
            return readGenMiscData(reader, columnIndex);
        }

        private string readGenMiscData(IExcelDataReader reader, int columnIndex) {
            //
            var mp = _da.Elsi.GetMiscParams();
            var gbpConv = reader.GetDouble(columnIndex+1);
            mp.GBPConv = gbpConv;
            return $"Read GBP to Euro conv [{gbpConv}]";
        }

        private string loadGenParameterData(IExcelDataReader reader) {
            moveToStartRow(reader, "Type", out int columnIndex);
            return readGenParameters(reader, columnIndex);
        }

        private void moveToStartRow(IExcelDataReader reader, string cellName, out int columnIndex) {
            columnIndex = 0;
            while (reader.Read()) {
                for( int i=0;i<reader.FieldCount;i++) {
                    var columnHeader = reader.GetValue(i);
                    if ( columnHeader is string ) {
                        int j =0;
                    }
                    if ( columnHeader is string && (string) columnHeader==cellName) {
                        columnIndex = i;
                        //
                        return;
                    }
                }
            }
            throw new Exception($"Could not find start row to load data [cellName]");
        }

        private string readGenParameters(IExcelDataReader reader, int columnIndex) {
            Logger.Instance.LogInfoEvent("Start reading GenParameters");
            // Caches of existing objects
            var existingData = _da.Elsi.GetGenParameters();
            var objCache = new ObjectCache<GenParameter>(_da, existingData, m=>m.Type.ToString() );
            //
            int numAdded = 0;
            int numUpdated = 0;
            while (reader.Read()) {
                var typeStr = reader.GetString(columnIndex);
                if ( string.IsNullOrEmpty(typeStr)) {
                    break;
                }
                if ( !TryParseGenType(typeStr,out ElsiGenType type) ) {
                    throw new Exception($"Unexpected Generation type found [{typeStr}]");
                }
                var eff = reader.GetDouble(columnIndex+1);
                var eRate = reader.GetDouble(columnIndex+2);
                var forcedDays = reader.GetDouble(columnIndex+4);
                var plannedDays = reader.GetDouble(columnIndex+5);
                var maintenanceCost = reader.GetDouble(columnIndex+6);
                var fuelCosts = reader.GetDouble(columnIndex+7);
                var warmStart = reader.GetDouble(columnIndex+10);
                var wearAndTearStart = reader.GetDouble(columnIndex+11);
                var dataTypeStr = reader.GetString(columnIndex+14);
                //ElsiGenDataType? dataType=null;
                //if ( dataTypeStr != "NA")  {
                //    if ( Enum.TryParse<ElsiGenDataType>(dataTypeStr,true, out ElsiGenDataType dType) ) {
                //        dataType = dType;
                //    } else {
                //        throw new Exception($"Unexpected Generation data type found [{dataTypeStr}]");
                //    }
                //}
                var endVal = reader.GetValue(columnIndex+15);
                double? endurance = null;
                if ( endVal is double ) {
                    endurance = (double) endVal;
                }
                //
                var obj = objCache.GetOrCreate(type.ToString(), out bool created);
                if ( created ) {
                    obj.Type = type;
                    numAdded++;
                } else {
                    numUpdated++;
                }
                obj.Efficiency = eff;
                obj.EmissionsRate = eRate;
                obj.ForcedDays = forcedDays;
                obj.PlannedDays = plannedDays;
                obj.MaintenanceCost = maintenanceCost;
                obj.FuelCost = fuelCosts;
                obj.WarmStart = warmStart;
                obj.WearAndTearStart = wearAndTearStart;
                obj.Endurance = endurance;
            }
            string msg = $"{numAdded} GenParams added, {numUpdated} GenParams updated";
            Logger.Instance.LogInfoEvent($"End reading GenParams, {msg}");
            return msg;
        }

        private bool TryParseGenType(string str, out ElsiGenType genType) {
            bool result = true;
            genType = ElsiGenType.Battery;
            if ( str == "Nuclear") {
                genType = ElsiGenType.Nuclear;
            } else if ( str == "Lignite") {
                genType = ElsiGenType.Lignite;
            } else if ( str == "Hard coal") {
                genType = ElsiGenType.HardCoal;
            } else if ( str == "Gas") {
                genType = ElsiGenType.Gas;
            } else if ( str == "Oil") {
                genType = ElsiGenType.Oil;
            } else if ( str == "Hydro-run") {
                genType = ElsiGenType.HydroRun;
            } else if ( str == "Hydro-turbine") {
                genType = ElsiGenType.HydroTurbine;
            } else if ( str == "Hydro-pump") {
                genType = ElsiGenType.HydroPump;
            } else if ( str == "Wind-on-shore") {
                genType = ElsiGenType.WindOnShore;
            } else if ( str == "Wind-off-shore") {
                genType = ElsiGenType.WindOffShore;
            } else if ( str == "Solar-PV") {
                genType = ElsiGenType.SolarPv;
            } else if ( str == "Solar (Thermal)") {
                genType = ElsiGenType.SolarThermal;
            } else if ( str == "Other RES") {
                genType = ElsiGenType.OtherRes;
            } else if ( str == "Other non-RES") {
                genType = ElsiGenType.OtherNonRes;
            } else if ( str == "Biofuels") {
                genType = ElsiGenType.Biofuels;
            } else if ( str == "Curtail") {
                genType = ElsiGenType.Curtail;
            } else if ( str == "Battery") {
                genType = ElsiGenType.Battery;
            } else {
                result = false;
            }

            return result;

        }
        private string loadGenCapacityData(IExcelDataReader reader) {
            moveToStartRow(reader, "Generation", out int columnIndex);
            return readGenCapacities(reader, columnIndex);
        }

        private string readGenCapacities(IExcelDataReader reader, int columnIndex) {
            Logger.Instance.LogInfoEvent("Start reading GenCapacities");
            // Caches of existing objects
            var existingData = _da.Elsi.GetGenCapacities();
            var objCache = new ObjectCache<GenCapacity>(_da, existingData, m=>m.GetKey() );
            //
            int numAdded = 0;
            int numUpdated = 0;
            while (reader.Read()) {
                var nameStr = reader.GetString(columnIndex);                
                if ( string.IsNullOrEmpty(nameStr)) {
                    break;
                }
                var zoneStr = reader.GetString(columnIndex+1);
                zoneStr = zoneStr.Replace("-","_");
                if ( !Enum.TryParse<ElsiZone>(zoneStr,true, out ElsiZone zone) ) {
                    throw new Exception($"Unexpected zone found [{zoneStr}]");
                }
                var mZoneStr = reader.GetString(columnIndex+2);
                if ( !Enum.TryParse<ElsiMainZone>(mZoneStr,true, out ElsiMainZone mZone)) {
                    throw new Exception($"Unexpected main zone found [{mZoneStr}]");
                }
                var typeStr = reader.GetString(columnIndex+3);
                if ( !TryParseGenType(typeStr, out ElsiGenType genType) ) {
                    throw new Exception($"Unexpected Generation type found [{typeStr}]");
                }

                var profileStr = reader.GetString(columnIndex+4);
                if ( !Enum.TryParse<ElsiProfile>(profileStr,true, out ElsiProfile profile)) {
                    throw new Exception($"Unexpected Profile found [{profileStr}]");
                }

                var order = getOrder(mZone,genType);

                int index = columnIndex + 5;
                var obj = objCache.GetOrCreate(GenCapacityMethods.GetKey(zone,genType), out bool created);
                if ( created ) {
                    obj.Zone = zone;
                    obj.GenType = genType;
                    numAdded++;
                } else {
                    numUpdated++;
                }
                obj.Profile = profile;
                obj.MainZone = mZone;
                obj.OrderIndex=order;
                obj.CommunityRenewables = reader.GetDouble(index++);
                obj.TwoDegrees = reader.GetDouble(index++);
                obj.SteadyProgression = reader.GetDouble(index++);
                obj.ConsumerEvolution = reader.GetDouble(index++);
            }
            string msg = $"{numAdded} GenCapacities added, {numUpdated} GenCapacities updated";
            Logger.Instance.LogInfoEvent($"End reading GenCapacities, {msg}");
            return msg;
        }

        private int? getOrder(ElsiMainZone mZone, ElsiGenType genType) {
            List<GenStoreData> list = genType.IsStorage() ? _storeDataList : _genDataList;
            int index=list.FindIndex(m=>m.Zone == mZone && m.Type == genType);
            return index<0 ? null: index;
        }

        private string loadAvailOrDemandData(IExcelDataReader reader, ElsiGenDataType genType) {
            moveToStartRow(reader, "Day", out int columnIndex);
            return readAvailOrDemands(reader, genType, columnIndex);
        }

        private string readAvailOrDemands(IExcelDataReader reader, ElsiGenDataType genType,  int columnIndex) {
            Logger.Instance.LogInfoEvent("Start reading Generations data");

            var genData = _da.Elsi.GetAvailOrDemands(genType);
            var objCache = new ObjectCache<AvailOrDemand>(_da, genData, m=>m.GetKey() );


            // Figure out the period and profile from the column header name
            var columnData = new Dictionary<int,dynamic>();
            for(int index=columnIndex+1; index<reader.FieldCount;index++) {
                var colHeader = reader.GetString(index);
                var cpnts = colHeader.Split(" ");
                if ( cpnts.Length!=2) {
                    throw new Exception($"Unexpected column header [{colHeader}]");
                }
                if ( !Enum.TryParse<ElsiProfile>(cpnts[0],true,out ElsiProfile profile)) {
                    throw new Exception($"Unexpected profile in header [{cpnts[0]}]");
                }
                if ( !Enum.TryParse<ElsiPeriod>(cpnts[1],true,out ElsiPeriod period)) {
                    throw new Exception($"Unexpected period in header [{cpnts[1]}]");
                }
                //
                columnData.Add(index,new {Profile=profile,Period=period});
            }

            //
            int numAdded = 0;
            int numUpdated = 0;
            while (reader.Read()) {
                var dayValue = reader.GetValue(columnIndex);                
                int day;
                if ( dayValue==null) {
                    break;
                } else if ( dayValue is double) {
                    day = (int) (double) dayValue;
                } else {
                    throw new Exception($"Unexpected value of day column [{dayValue}]");
                }
                for( int i=columnIndex+1; i<reader.FieldCount;i++) {
                    var cd = columnData[i];
                    var value = reader.GetDouble(i);
                    var obj = objCache.GetOrCreate(GenerationMethods.GetKey(day,cd.Profile,cd.Period,genType), out bool created);
                    if ( created ) {
                        obj.Profile = cd.Profile;
                        obj.Period = cd.Period;
                        obj.DataType = genType;
                        obj.Day = day;
                        numAdded++;
                    } else {
                        numUpdated++;
                    }
                    obj.Value = value;
                }
            }
            string msg = $"{numAdded} Generations added, {numUpdated} Generations updated for [{genType}]";
            Logger.Instance.LogInfoEvent($"End reading Generations data {msg}");
            return msg;
        }

        private string loadPeakDemandData(IExcelDataReader reader) {
            moveToStartRow(reader, "Zone", out int columnIndex);
            return readPeakDemands(reader, columnIndex);
        }

        private string readPeakDemands(IExcelDataReader reader, int columnIndex) {
            Logger.Instance.LogInfoEvent("Start reading PeakDemand data");

            var pdData = _da.Elsi.GetPeakDemands();
            var objCache = new ObjectCache<PeakDemand>(_da, pdData, m=>m.GetKey());
            //
            int numAdded = 0;
            int numUpdated = 0;
            while (reader.Read()) {
                var zoneStr = reader.GetString(columnIndex);
                if ( string.IsNullOrEmpty(zoneStr)) {
                    break;
                }
                if ( !Enum.TryParse<ElsiMainZone>(zoneStr,true,out ElsiMainZone mainZone)) {
                    throw new Exception($"Unexpected main zone string [{zoneStr}]");
                }
                var profileStr = reader.GetString(columnIndex+1);
                if ( !Enum.TryParse<ElsiProfile>(profileStr,true,out ElsiProfile profile)) {
                    throw new Exception($"Unexpected profile found [{profileStr}]");
                }

                int index = columnIndex + 2;
                var scenarios = new ElsiScenario[] {ElsiScenario.CommunityRenewables,ElsiScenario.TwoDegrees,ElsiScenario.SteadyProgression,ElsiScenario.ConsumerEvolution};
                foreach( var scenario in scenarios) {
                    var value = reader.GetDouble(index++);
                    var obj = objCache.GetOrCreate(PeakDemandMethods.GetKey(mainZone,profile,scenario), out bool created);
                    if ( created ) {
                        obj.MainZone = mainZone;
                        obj.Profile = profile;
                        obj.Scenario = scenario;
                        numAdded++;
                    } else {
                        numUpdated++;
                    }
                    //
                    obj.Peak = value;
                }
            }
            string msg = $"{numAdded} PeakDemands added, {numUpdated} PeakDemands updated";
            Logger.Instance.LogInfoEvent($"End reading PeakDemands {msg}");
            return msg;
        }
        private string loadProfilePeakData(IExcelDataReader reader) {
            moveToStartRow(reader, "Zone", out int columnIndex);
            moveToStartRow(reader, "Zone", out columnIndex);
            return readProfilePeakDemands(reader, columnIndex);
        }

        private string readProfilePeakDemands(IExcelDataReader reader, int columnIndex) {
            Logger.Instance.LogInfoEvent("Start reading profile PeakDemand data");

            var pdData = _da.Elsi.GetProfilePeakDemands();
            var objCache = new ObjectCache<PeakDemand>(_da, pdData, m=>m.GetKey());

            //
            int numAdded = 0;
            int numUpdated = 0;
            while (reader.Read()) {
                var profileStr = reader.GetString(columnIndex);
                if ( string.IsNullOrEmpty(profileStr)) {
                    break;
                }
                if ( !Enum.TryParse<ElsiProfile>(profileStr,true,out ElsiProfile profile)) {
                    throw new Exception($"Unexpected zone string [{profileStr}]");
                }
                var value = reader.GetDouble(columnIndex+1);
                var obj = objCache.GetOrCreate(PeakDemandMethods.GetKey(null,profile,null), out bool created);
                if ( created ) {
                    obj.MainZone = null;
                    obj.Profile = profile;
                    obj.Scenario = null;
                    numAdded++;
                } else {
                    numUpdated++;
                }
                //
                obj.Peak = value;
            }
            string msg = $"{numAdded} profile PeakDemands added, {numUpdated} profile PeakDemands updated";
            Logger.Instance.LogInfoEvent($"End reading profile PeakDemands {msg}");
            return msg;
        }

        private string loadLinkData(IExcelDataReader reader) {
            moveToStartRow(reader, "LinkName", out int columnIndex);
            return readLinks(reader, columnIndex);
        }

        private string readLinks(IExcelDataReader reader, int columnIndex) {
            Logger.Instance.LogInfoEvent("Start reading Link data");

            var pdData = _da.Elsi.GetLinks();
            var objCache = new ObjectCache<Link>(_da, pdData, m=>m.Name);
            //
            int numAdded = 0;
            int numUpdated = 0;
            while (reader.Read()) {
                var nameStr = reader.GetString(columnIndex);
                if ( string.IsNullOrEmpty(nameStr)) {
                    break;
                }
                var fromZoneStr = reader.GetString(columnIndex+1);
                if ( !Enum.TryParse<ElsiMainZone>(fromZoneStr,true,out ElsiMainZone fromZone)) {
                    throw new Exception($"Unexpected from zone string [{fromZoneStr}]");
                }
                var toZoneStr = reader.GetString(columnIndex+2);
                if ( !Enum.TryParse<ElsiMainZone>(toZoneStr,true,out ElsiMainZone toZone)) {
                    throw new Exception($"Unexpected to zone string [{toZoneStr}]");
                }
                var capacity = reader.GetDouble(columnIndex+3);
                var revCap = reader.GetDouble(columnIndex+4);
                var loss = reader.GetDouble(columnIndex+5);
                var market = reader.GetBoolean(columnIndex+6);
                var itf = reader.GetDouble(columnIndex+7);
                var itt = reader.GetDouble(columnIndex+8);
                var btf = reader.GetDouble(columnIndex+9);
                var btt = reader.GetDouble(columnIndex+10);

                var obj = objCache.GetOrCreate(nameStr, out bool created);
                if ( created ) {
                    obj.Name = nameStr;
                    numAdded++;
                } else {
                    numUpdated++;
                }
                //
                obj.FromZone = fromZone;
                obj.ToZone = toZone;
                obj.Capacity = capacity;
                obj.RevCap = revCap;
                obj.Loss = loss;
                obj.Market = market;
                obj.ITF = itf;
                obj.ITT = itt;
                obj.BTF = btf;
                obj.BTT = btt;
            }
            string msg = $"{numAdded} Links added, {numUpdated} Links updated";
            Logger.Instance.LogInfoEvent($"End reading Links {msg}");
            return msg;
        }

        private class GenStoreData {
            public ElsiMainZone Zone{ get; set;}
            public ElsiGenType Type {get; set;}
        }

    }
}
