using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Common;
using System.Text;
using SmartEnergyLabDataApi.Models;

namespace SmartEnergyLabDataApi.Elsi
{
    public class ElsiData {
        private int _year = 2019;
        private DataAccess _da;
        private string[] _seasons = new string[] {
            "W","W","A","A","A","S","S","S","A","A","A","W"
        };
        private DatasetInfo _datasetInfo;
        private IList<GenCapacity> _genCaps;
        private IList<AvailOrDemand> _demands;
        private Dictionary<ElsiGenDataType,IList<AvailOrDemand>> _availabilities;
        private IList<GenParameter> _genParameters;
        private IList<Link> _links;
        private IList<PeakDemand> _peakDemands;
        private Dictionary<ElsiMainZone,Dictionary<ElsiGenType,List<GenCapacity>>> _genCapDict;
        //
        private Dictionary<ElsiGenType,GenParameter> _genParamsDict;
        private Dictionary<ElsiMainZone,PeakDemand> _peakDemandsDict;
        private Dictionary<int,Dictionary<ElsiProfile,Dictionary<ElsiPeriod,AvailOrDemand>>> _demandsDict;
        public ElsiData(DataAccess da, DatasetInfo datasetInfo, ElsiScenario scenario) {
            //
            Scenario = scenario;
            _da = da;
            _datasetInfo = datasetInfo;
        }

        public void SetDay(int day) {
            if ( day<1 || day>365 ) {
                throw new Exception("Day out of range [1-365]");
            }
            Day = day;
            var dt = new DateTime(_year,1,1) + new TimeSpan(Day-1,0,0,0);
            Month = dt.Month-1;

            //
            //??_genCaps = _da.Elsi.GetGenCapacities();
            _genCaps = _datasetInfo.GenCapacityInfo.Data;
            //??_genParameters = _da.Elsi.GetGenParameters();
            _genParameters = _datasetInfo.GenParameterInfo.Data;
            //??_peakDemands = _da.Elsi.GetPeakDemands();
            _peakDemands = _datasetInfo.PeakDemandInfo.Data;
            //??_links = _da.Elsi.GetLinks();
            _links = _datasetInfo.LinkInfo.Data;

            _demands = _da.Elsi.GetAvailOrDemands(ElsiGenDataType.Demands);
            //
            _availabilities = new Dictionary<ElsiGenDataType, IList<AvailOrDemand>>();
            populateAvailabilities(_da,ElsiGenDataType.SolarAvail);
            populateAvailabilities(_da,ElsiGenDataType.OnShoreAvail);
            populateAvailabilities(_da,ElsiGenDataType.OffShoreAvail);
            populateAvailabilities(_da,ElsiGenDataType.Demands);
            //
            populateGenCapDict();
            populateGenParamsDict();
            populatePeakDemandsDict();
            populateDemandsDict();
            //
            Param = _datasetInfo.MiscParamsInfo.Data[0];
            //
            Per = new PeriodData(this);
            //
            ZDem = new ZDemData(this);
            //
            Gen = new GenData(this);
            //
            Store = new StorageData(this);
            //
            Links = new LinkData(this);
            //
            Availabilities = new AvailabilityData(this);
        }

        public string Print() {
            var lw = new LineWriter();
            lw.WriteLine("ZDem table");
            lw.WriteLine($"{"Zone",-8} {"AnnualPk",14} {"Pk",14} {"Pl",14} {"So",14} {"Pu",14} {"Tr",14} {"Market",-14}");
            foreach( var zd in ZDem.Items) {
                lw.WriteLine($"{zd.Zone,-8} {zd.AnnualPeak,14:f5} {zd.GetDemand(ElsiPeriod.Pk),14:f5} {zd.GetDemand(ElsiPeriod.Pl),14:f5} {zd.GetDemand(ElsiPeriod.So),14:f5} {zd.GetDemand(ElsiPeriod.Pu),14:f5} {zd.GetDemand(ElsiPeriod.Tr),14:f5} {zd.Market}");
            }            
            lw.WriteLine("");
            lw.WriteLine("Gen table");
            lw.WriteLine($"Zone\t{"Type",-12}\t{"Capacity",10}\t{"Wavail",9}\t{"Oavail",9}\t{"Co2",8}\t{"£/Co2",8}\t{"VarCost",8}\t{"Price",8}\t{"Bid",8}\t{"Offer",8}");
            foreach( var g in Gen.Items) {
                lw.WriteLine($"{g.Zone}\t{g.Type,-12}\t{g.Capacity,10:f1}\t{g.Wavail*100,8:f2}%\t{g.Oavail*100,8:f2}%\t{g.Emissions,8:f3}\t{g.EmissionsCost,8:c2}\t{g.VarCost,8:c2}\t{g.Price,8:c2}\t{g.Bid,8:c2}\t{g.Offer,8:c2}");
            }
            lw.WriteLine("");
            lw.WriteLine("Store table");
            lw.WriteLine($"{"Zone",-5} {"Type",-10} {"Capacity",8} {"MaxPump",8} {"Wavail",8} {"Oavail",8} {"CycleEff",8} {"End",-5} {"Bid",6} {"Offer",6}");
            foreach( var s in Store.Items) {
                lw.WriteLine($"{s.Zone,-5} {s.Type,-10} {s.Capacity,8:f1} {s.MaxPump,8:f1} {s.Wavail*100,7:f2}% {s.Oavail*100,7:f2}% {s.CycleEff*100,7:f0}% {s.Endurance,-5:f0} {s.Bid*100,5:f0}% {s.Offer*100,5:f0}%");
            }
            lw.WriteLine("");
            lw.WriteLine("Links table");
            lw.WriteLine($"{"LinkName",-8} {"From",-6} {"To",-6} {"Capacity",10} {"RevCap",10} {"Loss",6} {"Market",-6} {"ITF",7} {"ITT",7} {"BTF",7} {"BTT",7}");
            foreach( var l in Links.Rows) {
                lw.WriteLine($"{l.Name,-8} {l.FromZone,-6} {l.ToZone,-6} {l.Capacity,10:f1} {l.RevCap,10:f1} {l.Loss*100,5:f2}% {l.Market,-6} {l.ITF,7:c2} {l.ITT,7:c2} {l.BTF,7:c2} {l.BTT,7:c2}");
            }

            lw.WriteLine("");

            return lw.GetLinesAsString();
        }

        private void writeLine(string str) {
            Console.WriteLine(str);
        }

        private void populatePeakDemandsDict() {
            // Peak demands
            _peakDemandsDict = new Dictionary<ElsiMainZone, PeakDemand>();
            foreach( var pd in _peakDemands) {
                if ( pd.MainZone!=null) {
                    ElsiMainZone mZone = (ElsiMainZone) pd.MainZone;
                    if ( !_peakDemandsDict.ContainsKey(mZone) ) {
                        _peakDemandsDict.Add(mZone,pd);
                    }
                }
            }
        }

        public PeakDemand GetPeakDemand(ElsiMainZone mainZone) {
            return _peakDemandsDict[mainZone];
        }

        private void populateDemandsDict() {
            _demandsDict = new Dictionary<int, Dictionary<ElsiProfile, Dictionary<ElsiPeriod, AvailOrDemand>>>();
            // Demands
            foreach( var d in _demands) {
                if ( !_demandsDict.ContainsKey(d.Day) ) {
                    _demandsDict.Add(d.Day, new Dictionary<ElsiProfile, Dictionary<ElsiPeriod, AvailOrDemand>>());
                }
                //
                if (!_demandsDict[d.Day].ContainsKey(d.Profile)) {
                    _demandsDict[d.Day].Add(d.Profile,new Dictionary<ElsiPeriod, AvailOrDemand>());
                }
                //
                if (!_demandsDict[d.Day][d.Profile].ContainsKey(d.Period)) {
                    _demandsDict[d.Day][d.Profile].Add(d.Period,d);
                } else {
                    throw new Exception("Unexpected demand found");
                }
            }
        }

        public Dictionary<ElsiPeriod,AvailOrDemand> GetDemandDict(ElsiProfile profile) {
            return _demandsDict[Day][profile];
        }

        private void populateGenCapDict() {
            _genCapDict = new Dictionary<ElsiMainZone, Dictionary<ElsiGenType, List<GenCapacity>>>();
            foreach( var gc in _genCaps) {
                if ( !_genCapDict.ContainsKey(gc.MainZone) ) {
                    _genCapDict.Add(gc.MainZone,new Dictionary<ElsiGenType, List<GenCapacity>>());
                }
                if ( !_genCapDict[gc.MainZone].ContainsKey(gc.GenType)) {
                    _genCapDict[gc.MainZone].Add(gc.GenType,new List<GenCapacity>());
                }
                _genCapDict[gc.MainZone][gc.GenType].Add(gc);
            }
        }

        public IList<GenCapacity> GetGenCapacity(ElsiMainZone mainZone, ElsiGenType genType) {
            
            return _genCapDict[mainZone][genType];
        }

        private void populateGenParamsDict() {
            _genParamsDict = new Dictionary<ElsiGenType, GenParameter>();
            foreach( var gp in _genParameters) {
                if (!_genParamsDict.ContainsKey(gp.Type)) {
                    _genParamsDict.Add(gp.Type,gp);
                }
            }
        }

        public GenParameter GetGenParameter(ElsiGenType genType) {
            return _genParamsDict[genType];
        }

        private void populateAvailabilities(DataAccess da, ElsiGenDataType genType) {
            var avails = da.Elsi.GetAvailOrDemands(genType);
            _availabilities.Add(genType,avails);
        }

        public int Day {get; private set;}
        public int Month { get; private set; }
        public int Year {
            get {
                return _year;
            }
        }
        public string Season {
            get {
                return _seasons[Month];
            }
        }

        public ElsiProfile GetProfile(ElsiMainZone mainZone) {
            if ( mainZone == ElsiMainZone.BE) {
                return ElsiProfile.BE;
            } else if ( mainZone == ElsiMainZone.DE) {
                return ElsiProfile.DE;
            } else if ( mainZone == ElsiMainZone.DKe) {
                return ElsiProfile.DKe;
            } else if ( mainZone == ElsiMainZone.DKw) {
                return ElsiProfile.DKw;
            } else if ( mainZone == ElsiMainZone.FR) {
                return ElsiProfile.FR;
            } else if ( mainZone.ToString().StartsWith("GB")) {
                return ElsiProfile.GB;
            } else if ( mainZone == ElsiMainZone.IE ) {
                return ElsiProfile.IE;
            } else if ( mainZone == ElsiMainZone.NI ) {
                return ElsiProfile.NI;
            } else if ( mainZone == ElsiMainZone.NL ) {
                return ElsiProfile.NL;
            } else if ( mainZone == ElsiMainZone.NO ) {
                return ElsiProfile.NO;
            } else {
                throw new Exception($"Unexpected ElsiMainZone {mainZone}");
            }
        }

        public ElsiScenario Scenario {get; private set;}
        public MiscParams Param {get; private set;}
        public PeriodData Per {get; private set;}
        public ZDemData ZDem {get; private set;}
        public GenData Gen {get; private set;}
        public StorageData Store {get; private set;}
        public LinkData Links {get; private set;}
        public AvailabilityData Availabilities {get; private set;}

        public class MainParams {
            public MainParams( ElsiData data ) {
                //EU_CO2 = 26 * data._gbConv;
                //GB_CO2 = EU_CO2 + 18.08;
                //VLL = 4000;
                var mp = data._da.Elsi.GetMiscParams();
                EU_CO2 = mp.EU_CO2;
                GB_CO2 = mp.GB_CO2;
                VLL = mp.VLL;
            }
            /// <summary>
            /// EUETS price of carbon
            /// </summary>
            /// <value></value>
            public double EU_CO2 {get; private set;}
            /// <summary>
            /// GB price of carbon (included GB power sector carbon floor price)
            /// </summary>
            /// <value></value>
            public double GB_CO2 {get; private set;}
            /// <summary>
            /// Value of lost load (cost of demand curtailment if insufficient accessible generation)
            /// </summary>
            /// <value></value>
            public double VLL {get; private set;}
        }

        public class PeriodData {

            private OrderedDictionary<ElsiPeriod,Row> _dict;
            private ElsiData _data;

            public PeriodData(ElsiData data) {
                _data = data;
                _dict = new OrderedDictionary<ElsiPeriod,Row>();
                //
                var row = new Row(data) {
                    Period = ElsiPeriod.Pk,
                    MonthHours = new int[12] {2,2,2,2,2,2,2,2,2,2,2,2}
                };
                _dict.Add(row.Period,row);
                //
                row = new Row(data) {
                    Period = ElsiPeriod.Pl,
                    MonthHours = new int[12] {7,6,5,4,4,4,4,4,4,5,7,8}
                };
                _dict.Add(row.Period,row);
                //
                row = new Row(data) {
                    Period = ElsiPeriod.So,
                    MonthHours = new int[12] {6,7,8,9,9,9,9,9,9,8,6,5}
                };
                _dict.Add(row.Period,row);
                //
                row = new Row(data) {
                    Period = ElsiPeriod.Pu,
                    MonthHours = new int[12] {4,3,3,3,3,3,3,3,3,3,3,3}
                };
                _dict.Add(row.Period,row);
                //
                row = new Row(data) {
                    Period = ElsiPeriod.Tr,
                    MonthHours = new int[12] {5,6,6,6,6,6,6,6,6,6,6,6}
                };
                _dict.Add(row.Period,row);
            }

            public ElsiPeriod GetPeriod(int index) {
                return (ElsiPeriod) index;
            }

            public List<Row> Items {
                get {
                    return _dict.Items;
                }
            }

            public int Count {
                get {
                    return _dict.Count;
                }
            }

            public class Row {
                ElsiData _data;
                public Row(ElsiData data) {
                    _data = data;
                }

                public ElsiPeriod Period {get; set;}

                public int Hours {
                    get {
                        return MonthHours[_data.Month];
                    }
                }
                public int[] MonthHours {get; set;}
            }
        }


        public class ZDemData {
            private OrderedDictionary<ElsiMainZone,Row> _dict;
            // The matches the original spreadsheet that makes debug easier
            private ElsiMainZone[] order = new ElsiMainZone[] {
                ElsiMainZone.GB_SH, ElsiMainZone.GB_SP, ElsiMainZone.GB_UN, ElsiMainZone.GB_NW, ElsiMainZone.GB_MC, ElsiMainZone.GB_EA, ElsiMainZone.GB_SC,
                ElsiMainZone.NI, ElsiMainZone.IE, ElsiMainZone.NO, ElsiMainZone.DKw, ElsiMainZone.DKe, ElsiMainZone.NL, ElsiMainZone.BE, ElsiMainZone.FR, ElsiMainZone.DE
            };
            private ElsiData _data;
            public ZDemData(ElsiData data) {
                _data = data;
                _dict = new OrderedDictionary<ElsiMainZone, Row>();
                // Add rows to dictionary for fast lookup
                foreach( var mz in order) {
                    if ( !_dict.ContainsKey(mz)) {
                        _dict.Add(mz,createRow(mz));
                    }
                }
            }

            public Row GetRow(ElsiMainZone mainZone) {
                return _dict.Item(mainZone);
            }

            public Row GetRow(int index) {
                return _dict.Item(index);
            }

            public bool ContainsKey(ElsiMainZone mainZone) {
                return _dict.ContainsKey(mainZone);
            }

            public int Count {
                get {
                    return _dict.Count;
                }
            }

            public int GetIndex(ElsiMainZone mainZone) {
                var row = GetRow(mainZone);              
                return _dict.Index(row);
            }

            public List<Row> Items {
                get {
                    return _dict.Items;
                }
            }

            private string? getMarket(ElsiProfile profile) {
                if ( profile == ElsiProfile.GB ) {
                    return "BETTA";
                } else if ( profile == ElsiProfile.IE || profile == ElsiProfile.NI) {
                    return "SEM";
                } else {
                    return null;
                }
            }

            private Row createRow(ElsiMainZone mainZone) {
                var peakDemand = _data.GetPeakDemand(mainZone);
                var profile = peakDemand.Profile;
                var market = getMarket(profile);
                var demandsDict = _data.GetDemandDict(profile);
                return new Row(mainZone, peakDemand.GetPeakDemand(_data.Scenario), demandsDict, market);
            }

            public class Row {
                private Dictionary<ElsiPeriod,AvailOrDemand> _demandDict;
                public Row(ElsiMainZone zone, double peak, Dictionary<ElsiPeriod,AvailOrDemand> demandDict, string? market) {
                    Zone = zone;
                    AnnualPeak = peak;
                    _demandDict = demandDict;
                    Market = market;
                }

                public ElsiMainZone Zone {get; private set;}

                public double AnnualPeak {get; private set;}
                public double GetDemand(ElsiPeriod period) {
                    return (_demandDict[period].Value * AnnualPeak);
                }
                public string? Market {get; private set;}
            }
        }

        public class GenData {
            private Dictionary<ElsiMainZone,Dictionary<ElsiGenType,Row>> _dict;
            private List<Row> _list;
            private ElsiData _data;
            public GenData(ElsiData data) {
                _data = data;
                _dict = new Dictionary<ElsiMainZone, Dictionary<ElsiGenType, Row>>();
                _list = new List<Row>();
                // Add rows to dictionary for fast lookup
                foreach( var gc in _data._genCaps) {
                    if ( !gc.GenType.IsStorage() ) {
                        if ( !_dict.ContainsKey(gc.MainZone)) {
                            _dict.Add(gc.MainZone,new Dictionary<ElsiGenType, Row>());
                        }
                        if( !_dict[gc.MainZone].ContainsKey(gc.GenType) ) {
                            var row = createRow(gc.MainZone,gc.GenType);
                            _dict[gc.MainZone].Add(gc.GenType,row);
                            _list.Add(row);                            
                        }
                    }
                }
            }
            private Row createRow(ElsiMainZone mainZone, ElsiGenType genType) {
                var gcs = _data.GetGenCapacity(mainZone,genType);
                var capacity = gcs.Sum(m=>m.GetCapacity(_data.Scenario));
                var genParam = _data.GetGenParameter(genType);
                var wavail =genParam.GetWAvail();
                var oavail = genParam.GetOAvail();
                var emissions = genParam.GetEmissionsPerMWh();
                var emissionsCost = mainZone.ToString().StartsWith("GB") ? _data.Param.GB_CO2 : _data.Param.EU_CO2;
                var varCost = genParam.GetVarCostPerMWh()*_data.Param.GBPConv;
                var price = varCost + (emissionsCost * emissions);
                var bid = 0.6*price;
                var offer = 1.6*price;
                return new Row() {
                                    Zone = mainZone,
                                    Type = genType,
                                    Capacity = capacity, 
                                    Wavail = wavail, 
                                    Oavail = oavail,
                                    Emissions = emissions,
                                    EmissionsCost = emissionsCost,
                                    VarCost = varCost,
                                    Price = price,
                                    Bid = bid,
                                    Offer = offer
                                    };
            }

            public Row GetRow(ElsiMainZone mainZone, ElsiGenType genType) {
                return _dict[mainZone][genType];
            }

            public Row GetRow(int index) {
                return _list[index-1];
            }

            public int Count {
                get {
                    return _list.Count;
                }
            }

            public List<Row> Items {
                get {
                    return _list;
                }
            }

            public class Row {
                public Row() {
                }
                public string GenName {
                    get {
                        return $"{Zone}:{Type}";
                    }
                }
                public ElsiMainZone Zone {get; set;}

                public ElsiGenType Type {get; set;}

                public double Capacity {get; set;}

                public double Wavail {get; set;}

                public double Oavail {get; set;}

                public double Emissions {get; set;}

                public double EmissionsCost {get; set;}

                public double VarCost {get; set;}

                public double Price {get; set;}

                public double Bid {get; set;}

                public double Offer {get; set;}

                public double? Flex1 {
                    get {
                        return null;
                    }
                }

                public double? Flex2 {
                    get {
                        return null;
                    }
                }
            }
        }

        public class StorageData {

            private Dictionary<ElsiMainZone,Dictionary<ElsiGenType,Row>> _dict;
            private List<Row> _list;
            private ElsiData _data;
            public StorageData(ElsiData data) {
                _data = data;
                _dict = new Dictionary<ElsiMainZone, Dictionary<ElsiGenType, Row>>();
                _list = new List<Row>();
                // Add rows to dictionary for fast lookup
                foreach( var gc in _data._genCaps)  {
                    if ( gc.GenType.IsStorage() ) {
                        if ( !_dict.ContainsKey(gc.MainZone)) {
                            _dict.Add(gc.MainZone,new Dictionary<ElsiGenType, Row>());
                        }
                        if ( !_dict[gc.MainZone].ContainsKey(gc.GenType) ) {
                            var row = createRow(gc.MainZone,gc.GenType);
                            _dict[gc.MainZone].Add(gc.GenType,row);
                            _list.Add(row);
                        }
                    }
                }

            }
            private Row createRow(ElsiMainZone mainZone, ElsiGenType genType) {
                var scenario = _data.Scenario;
                var gcs = _data.GetGenCapacity(mainZone, genType);
                var capacity = gcs.Sum(m=>m.GetCapacity(_data.Scenario));
                var genParam = _data.GetGenParameter(genType);
                var wavail = genParam.GetWAvail();
                var oavail = genParam.GetOAvail();
                var cycleEff=genParam.Efficiency;
                var endurance=genParam.Endurance!=null ? (double) genParam.Endurance : 0;
                var bid = 1;
                var offer = 1;
                return new Row() {  
                                    Zone = mainZone,
                                    Type = genType,
                                    Capacity = capacity, 
                                    MaxPump = capacity,
                                    Wavail = wavail, 
                                    Oavail = oavail,
                                    CycleEff = cycleEff,
                                    Endurance = endurance,
                                    Bid = bid,
                                    Offer = offer
                                    };

            }

            public Row GetRow(ElsiMainZone mainZone, ElsiGenType genType) {
                return _dict[mainZone][genType];
            }

            public Row GetRow(int index) {
                return _list[index-1];
            }

            public List<Row> Items {
                get {
                    return _list;
                }
            }

            public int Count {
                get {
                    return _list.Count;
                }
            }

            public class Row {
                public string StoreName {
                    get {
                        return $"{Zone}:{Type.GetName()}";
                    }
                }
                public ElsiMainZone Zone;
                public ElsiGenType Type;
                public double Capacity {get; set;}
                public double MaxPump {get; set;} 
                public double Wavail {get; set;}
                public double Oavail {get; set;}
                public double CycleEff {get; set;}
                public double Endurance {get; set;}
                public double Bid {get; set;}
                public double Offer {get; set;}
            }
        }

        public class LinkData {
            private ElsiData _data;
            private OrderedDictionary<string,Link> _dict;
            public LinkData(ElsiData data) {
                _data= data;
                _dict = new OrderedDictionary<string, Link>();
                //
                foreach( var l in _data._links.OrderBy(m=>m.Id)) {
                    if ( !_dict.ContainsKey(l.Name)) {
                        _dict.Add(l.Name,l);
                    }
                }
            }

            public Link GetRow(string name) {
                return _dict.Item(name);
            }

            public Link GetRow(int index) {
                return _dict.Item(index);
            }

            public List<Link> Rows {
                get {
                    return _dict.Items;
                }
            }

            public int Count {
                get {
                    return _dict.Count;
                }
            }

            public int GetIndex(string name) {
                return _dict.Index(GetRow(name));
            }
        }

        public class AvailabilityData {
            private ElsiData _data;
            private Dictionary<ElsiGenDataType,Dictionary<ElsiProfile,Dictionary<ElsiPeriod,Dictionary<int,AvailOrDemand>>>> _dict;
            public AvailabilityData(ElsiData data) {
                _data = data;
                _dict = new Dictionary<ElsiGenDataType, Dictionary<ElsiProfile, Dictionary<ElsiPeriod, Dictionary<int, AvailOrDemand>>>>();
                foreach( var kvp in _data._availabilities) {
                    if ( !_dict.ContainsKey(kvp.Key) ) {
                        _dict.Add(kvp.Key, new Dictionary<ElsiProfile, Dictionary<ElsiPeriod, Dictionary<int, AvailOrDemand>>>());
                    }
                    foreach( var aod in kvp.Value) {
                        if (!_dict[kvp.Key].ContainsKey(aod.Profile)) {
                            _dict[kvp.Key].Add(aod.Profile,new Dictionary<ElsiPeriod, Dictionary<int, AvailOrDemand>>());
                        }
                        if ( !_dict[kvp.Key][aod.Profile].ContainsKey(aod.Period) ) {
                            _dict[kvp.Key][aod.Profile].Add(aod.Period, new Dictionary<int, AvailOrDemand>());
                        }
                        _dict[kvp.Key][aod.Profile][aod.Period][aod.Day] = aod;
                    }
                }
            }

            public double? GetAvailability(ElsiGenType genType, ElsiProfile profile, ElsiPeriod period) {
                var genDataType = GetGenDataType(genType);
                if ( genDataType!=null ) {
                    var day = _data.Day;
                    var availOrDemand = _dict[(ElsiGenDataType) genDataType][profile][period][day];
                    return availOrDemand.Value;
                } else {
                    return null;
                }
            }

            public ElsiGenDataType? GetGenDataType(ElsiGenType genType) {
                if ( genType == ElsiGenType.SolarPv || genType==ElsiGenType.SolarThermal ) {
                    return ElsiGenDataType.SolarAvail;
                } else if ( genType == ElsiGenType.WindOnShore ) {
                    return ElsiGenDataType.OnShoreAvail;
                } else if ( genType == ElsiGenType.WindOffShore ) {
                    return ElsiGenDataType.OffShoreAvail;
                } else if ( genType == ElsiGenType.Curtail ) {
                    return ElsiGenDataType.Demands;
                } else {
                    return null;
                }
            }
        }

    }

    public class LineWriter {
        public LineWriter() {
            _lines = new List<string>();
        }

        private List<string> _lines;
        public void WriteLine(string line) {
            _lines.Add(line);
        }

        public string GetLinesAsString() {
            StringBuilder builder = new StringBuilder();
            foreach( var line in _lines) {
                builder.AppendLine(line);
            }
            return builder.ToString();
        }
    }
}