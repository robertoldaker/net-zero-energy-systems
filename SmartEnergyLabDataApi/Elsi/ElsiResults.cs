using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Common;
using System.Text.Json.Serialization;

namespace SmartEnergyLabDataApi.Elsi
{
    public enum ElsiStorageMode { Generation, Production }
    public enum ElsiBalanceMechanismInfoType { Gen, Store, Link }
    public class ElsiResults
    {

        public ElsiResults()
        {
            DayResults = new List<ElsiDayResult>();
        }

        public void AddResult(ModelManager mm)
        {
            DayResults.Add(new ElsiDayResult(mm));
        }

        public List<ElsiDayResult> DayResults { get; private set; }
    }

    public class ElsiDayResultEx {
        public ElsiDayResultEx() {

        }
    }

    public class ElsiDayResult
    {
        ModelManager _mm;
        ElsiData _data;
        public ElsiDayResult(ModelManager mm)
        {
            _mm = mm;
            _data = _mm.Data;
            Day = _data.Day;
            Year = _data.Year;
            Season = _data.Season;
            Scenario = _data.Scenario;

            // Period results
            createPeriodResults();
        }

        public ElsiDayResult() {

        }

        public void CreateAvailabilityResults()
        {
            // Availability results
            Availability = new AvailabilityResults(this);
        }

        public void CreateMarketResults()
        {
            // Availability results
            Market = new MarketResults(this);
        }

        public void CreateBalanceResults()
        {
            // Availability results
            Balance = new BalanceResults(this);
            BalanceMechanism = new BalanceMechanismResults(this);
        }

        public void CreateMismatchResults() {
            Mismatches = new MismatchResults(this);
        }


        private void createPeriodResults()
        {
            PeriodResults = new List<PeriodResult>();
            int i = 0;
            foreach (var p in _mm.Data.Per.Items)
            {
                var pr = new PeriodResult()
                {
                    Period = p.Period,
                    Hours = p.Hours,
                    Index = i
                };
                PeriodResults.Add(pr);
                i++;
            }
        }

        private List<DemandResult> createDemandResults(int dType)
        {
            int numPeriods = Enum.GetValues<ElsiPeriod>().Count();
            var results = new List<DemandResult>();
            object[,] oparray;
            double[,] auxdata = null;
            int p = 0, z = 0;
            foreach (var lp in _mm.PerLp)
            {
                _mm.Zones.Outputs(lp, dType, auxdata, out oparray);
                if (results.Count == 0)
                {
                    for (z = 1; z < oparray.GetLength(0); z++)
                    {
                        var row = _mm.Data.ZDem.GetRow(z);
                        var dr = new DemandResult(numPeriods, row.Zone);
                        results.Add(dr);
                    }
                }
                for (z = 1; z < oparray.GetLength(0); z++)
                {
                    results[z - 1].Demands[p] = (double)oparray[z, 1];
                }
                p++;
            }
            return results;
        }

        private List<GeneratorResult> createGeneratorResults(int dType)
        {
            int numPeriods = Enum.GetValues<ElsiPeriod>().Count();
            var results = new List<GeneratorResult>();
            object[,] oparray;
            double[,] auxdata = null;
            int p = 0, z = 0;
            foreach (var lp in _mm.PerLp)
            {
                _mm.Gens.Outputs(lp, dType, auxdata, out oparray);
                if (results.Count == 0)
                {
                    for (z = 1; z < oparray.GetLength(0); z++)
                    {
                        var row = _mm.Data.Gen.GetRow(z);
                        var gr = new GeneratorResult(numPeriods, row.Zone, row.Type);
                        gr.Cost = row.Price;
                        gr.Capacity = row.Capacity;
                        results.Add(gr);
                    }
                }
                for (z = 1; z < oparray.GetLength(0); z++)
                {
                    results[z - 1].Capacities[p] = (double)oparray[z, 1];
                }
                p++;
            }
            //
            return results;
        }

        private List<StoreResult> createStoreResults(int dType)
        {
            int numPeriods = Enum.GetValues<ElsiPeriod>().Count();
            var results = new List<StoreResult>();
            object[,] oparray;
            double[,] auxdata = null;
            int p, z;
            _mm.Stores.Outputs(_mm.DayLp, dType, auxdata, out oparray);
            for (z = 1; z <= (oparray.GetLength(0) - 1) / 2; z++)
            {
                var row = _mm.Data.Store.GetRow(z);
                var gr = new StoreResult(numPeriods, row.Zone, row.Type, ElsiStorageMode.Generation);
                gr.Cost = 0;
                gr.Capacity = row.Capacity;
                results.Add(gr);
                gr = new StoreResult(numPeriods, row.Zone, row.Type, ElsiStorageMode.Production);
                gr.Capacity = 0;
                gr.Capacity = -row.MaxPump;
                results.Add(gr);
            }
            for (z = 1; z < oparray.GetLength(0); z += 2)
            {
                for (p = 1; p < oparray.GetLength(1); p++)
                {
                    results[z - 1].Capacities[p - 1] = (double)oparray[z, p];
                    results[z].Capacities[p - 1] = (double)oparray[z + 1, p];
                }
            }
            //
            return results;
        }

        private List<StorePrice> createStorePrices(int dType)
        {
            int numPeriods = Enum.GetValues<ElsiPeriod>().Count();
            var results = new List<StorePrice>();
            object[,] oparray;
            double[,] auxdata = null;
            int p, z;
            _mm.Stores.Outputs(_mm.DayLp, dType, auxdata, out oparray);
            for (z = 1; z <= (oparray.GetLength(0) - 1) / 2; z++)
            {
                var row = _mm.Data.Store.GetRow(z);
                var gr = new StorePrice(numPeriods, row.Zone, row.Type, ElsiStorageMode.Generation);
                gr.Cost = 0;
                gr.Capacity = row.Capacity;
                results.Add(gr);
                gr = new StorePrice(numPeriods, row.Zone, row.Type, ElsiStorageMode.Production);
                gr.Capacity = 0;
                gr.Capacity = -row.MaxPump;
                results.Add(gr);
            }
            for (z = 1; z < oparray.GetLength(0); z += 2)
            {
                for (p = 1; p < oparray.GetLength(1); p++)
                {
                    results[z - 1].Prices[p - 1] = (double)oparray[z, p];
                    results[z].Prices[p - 1] = (double)oparray[z + 1, p];
                }
            }
            //
            return results;
        }

        private List<LinkResult> createLinkResults(int dType, object[]? auxdataArray = null)
        {
            int numPeriods = Enum.GetValues<ElsiPeriod>().Count();
            var results = new List<LinkResult>();
            object[,] oparray;
            int p = 0, z = 0;
            foreach (var lp in _mm.PerLp)
            {
                double[,] auxdata = auxdataArray!=null ? getDoubleArray(auxdataArray[p]) : null;

                _mm.Links.Outputs(lp, dType, auxdata, out oparray);
                if (results.Count == 0)
                {
                    for (z = 1; z <= _mm.Data.Links.Count; z++)
                    {
                        var row = _mm.Data.Links.GetRow(z);
                        var lr = new LinkResult(numPeriods, row.Name, row.FromZone, row.ToZone);
                        lr.From.Cost = 0;
                        lr.From.Capacity = row.Capacity;
                        lr.To.Capacity = row.RevCap;
                        results.Add(lr);
                    }
                }
                for (z = 0; z < results.Count; z++)
                {
                    results[z].From.Capacities[p] = (double)oparray[z * 2 + 1, 1];
                    results[z].To.Capacities[p] = (double)oparray[z * 2 + 2, 1];
                }
                p++;
            }
            //
            return results;
        }

        private double[,] getDoubleArray(object ad) {
            if ( ad is double[,]) {
                return (double[,]) ad;
            } else if ( ad is object[,]) {
                var adObj = (object[,]) ad;
                var auxdata = new double[adObj.GetLength(0),adObj.GetLength(1)];
                for( int i=0;i<adObj.GetLength(0);i++) {
                    for( int j=0;j<adObj.GetLength(1);j++) {
                        if ( adObj[i,j]!=null ) {
                            auxdata[i,j] = (double) adObj[i,j];
                        }
                    }
                }
                return auxdata;
            } else {
                throw new Exception($"Unexpected type for auxdata [{ad.GetType().Name}]");
            }
        }

        private List<MarginalPrice> createMarginalPrices(int dType)
        {
            int numPeriods = Enum.GetValues<ElsiPeriod>().Count();
            var results = new List<MarginalPrice>();
            object[,] oparray;
            double[,] auxdata = null;
            int p = 0, z = 0;
            foreach (var lp in _mm.PerLp)
            {
                _mm.Zones.Outputs(lp, dType, auxdata, out oparray);
                if (results.Count == 0)
                {
                    for (z = 1; z < oparray.GetLength(0); z++)
                    {
                        var row = _mm.Data.ZDem.GetRow(z);
                        var dr = new MarginalPrice(numPeriods, row.Zone);
                        results.Add(dr);
                    }
                }
                for (z = 1; z < oparray.GetLength(0); z++)
                {
                    results[z - 1].Prices[p] = (double)oparray[z, 1];
                }
                p++;
            }
            return results;
        }

        private List<LinkPrice> createLinkPrices()
        {
            int numPeriods = Enum.GetValues<ElsiPeriod>().Count();
            var results = new List<LinkPrice>();
            object[,] oparray;
            _mm.zmps( out object[,] auxdataObj);
            var auxdata = new double[auxdataObj.GetLength(0),auxdataObj.GetLength(1)];
            for( int i=0;i<auxdataObj.GetLength(0);i++) {
                for( int j=0;j<auxdataObj.GetLength(1);j++) {
                    if ( auxdataObj[i,j]!=null ) {
                        auxdata[i,j] = (double) auxdataObj[i,j];
                    }
                }
            }
            int p = 0, z = 0;
            foreach (var lp in _mm.PerLp)
            {
                _mm.Links.Outputs(lp, ModelConsts.d_price, auxdata, out oparray);
                if (results.Count == 0)
                {
                    for (z = 1; z <= _mm.Data.Links.Count; z++)
                    {
                        var row = _mm.Data.Links.GetRow(z);
                        var lr = new LinkPrice(numPeriods, row.Name, row.FromZone, row.ToZone);
                        lr.From.Cost = 0;
                        lr.From.Capacity = row.Capacity;
                        lr.To.Capacity = row.RevCap;
                        results.Add(lr);
                    }
                }
                for (z = 0; z < results.Count; z++)
                {
                    results[z].From.Prices[p] = (double)oparray[z * 2 + 1, 1];
                    results[z].To.Prices[p] = (double)oparray[z * 2 + 2, 1];
                }
                p++;
            }
            //
            return results;
        }

        private List<ZoneEmissionsRate> createZoneEmissionsRates()
        {
            int numPeriods = Enum.GetValues<ElsiPeriod>().Count();
            var results = new List<ZoneEmissionsRate>();
            object[,] oparray;
            double[,] auxdata = null;
            int p = 0, z = 0;
            foreach (var lp in _mm.PerLp)
            {
                _mm.Zones.Outputs(lp, ModelConsts.d_emissions, auxdata, out oparray);
                if (results.Count == 0)
                {
                    for (z = 1; z < oparray.GetLength(0); z++)
                    {
                        var row = _mm.Data.ZDem.GetRow(z);
                        var dr = new ZoneEmissionsRate(numPeriods, row.Zone);
                        results.Add(dr);
                    }
                }
                for (z = 1; z < oparray.GetLength(0); z++)
                {
                    results[z - 1].Rates[p] = (double)oparray[z, 1];
                }
                p++;
            }
            return results;
        }

        private MiscData createMiscData() {
            int numPeriods = Enum.GetValues<ElsiPeriod>().Count();
            var result = new MiscData(numPeriods);
            for(int p=0;p<numPeriods;p++) {
                result.Iters[p] = _mm.iters[p];
                result.ProductionCosts[p] = -_mm.tsobj[p];
            }
            result.DayError = _mm.Berr;
            //
            return result;
        }

        private ProductionCostDiffs createProductionCostDiffs() {
            int numPeriods = Enum.GetValues<ElsiPeriod>().Count();
            var result = new ProductionCostDiffs(numPeriods);
            for(int p=0;p<numPeriods;p++) {
                // Difference is + since tsobj represents -ve costs
                result.Diffs[p] = -(Market.MiscData.ProductionCosts[p]+_mm.tsobj[p]);
            }
            //
            return result;
        }

        public int Day { get; set;}

        public int Year {  get; set; }

        public string Season { get; set; }

        public ElsiScenario Scenario { get; set; }

        public List<PeriodResult> PeriodResults { get; set; }

        public AvailabilityResults Availability { get; set; }

        public MarketResults Market { get; set; }

        public BalanceResults Balance { get; set; }

        public BalanceMechanismResults BalanceMechanism { get; set; }

        public MismatchResults Mismatches {get; set;}

        public class AvailabilityResults
        {

            public AvailabilityResults(ElsiDayResult edr)
            {
                DemandResults = edr.createDemandResults(ModelConsts.d_avail);
                GeneratorResults = edr.createGeneratorResults(ModelConsts.d_avail);
                StoreResults = edr.createStoreResults(ModelConsts.d_avail);
                LinkResults = edr.createLinkResults(ModelConsts.d_avail);
            }

            public AvailabilityResults() {

            }

            public List<DemandResult> DemandResults { get; set; }

            public List<GeneratorResult> GeneratorResults { get; set; }

            public List<StoreResult> StoreResults { get; set; }

            public List<LinkResult> LinkResults { get; set; }
        }

        public class MarketResults
        {
            public MarketResults(ElsiDayResult edr)
            {
                GeneratorResults = edr.createGeneratorResults(ModelConsts.d_sched);
                StoreResults = edr.createStoreResults(ModelConsts.d_sched);
                LinkResults = edr.createLinkResults(ModelConsts.d_sched, edr._mm.linkflows );
                MarginalPrices = edr.createMarginalPrices(ModelConsts.d_price);
                StorePrices = edr.createStorePrices(ModelConsts.d_price);  
                LinkPrices = edr.createLinkPrices();
                ZoneEmissionRates = edr.createZoneEmissionsRates();
                MiscData = edr.createMiscData();
            }

            public MarketResults() {

            }

            public List<GeneratorResult> GeneratorResults { get; set; }
            public List<StoreResult> StoreResults { get; set; }
            public List<LinkResult> LinkResults { get; set; }
            public List<MarginalPrice> MarginalPrices { get; set; }
            public List<StorePrice> StorePrices { get; set; }
            public List<LinkPrice> LinkPrices { get; set; }
            public List<ZoneEmissionsRate> ZoneEmissionRates { get; set; }
            public MiscData MiscData { get; set; }
        }

        public class BalanceResults : MarketResults {
            public BalanceResults( ElsiDayResult edr) : base(edr) {
                ProductionCostDiffs = edr.createProductionCostDiffs();
            }

            public BalanceResults() {

            }

            public ProductionCostDiffs ProductionCostDiffs {get; set;}
        }

        public class PeriodResult
        {
            public ElsiPeriod Period { get; set; }
            public string PeriodName
            {
                get
                {
                    return Period.ToString();
                }
            }
            public int Hours { get; set; }
            public int Index { get; set; }
        }

        public class DemandResult
        {
            public DemandResult(int numPeriods, ElsiMainZone zone)
            {
                Zone = zone;
                Demands = new double[numPeriods];
            }

            public DemandResult() {

            }

            public ElsiMainZone Zone { get; set; }
            public string ZoneName
            {
                get
                {
                    return Zone.ToString();
                }
            }
            public double[] Demands { get; set; }
        }

        public class GeneratorResult
        {

            public GeneratorResult(int numPeriods, ElsiMainZone zone, ElsiGenType genType)
            {
                Capacities = new double[numPeriods];
                Zone = zone;
                GenType = genType;
            }

            public GeneratorResult() {

            }

            public ElsiMainZone Zone { get; set; }

            public string ZoneName
            {
                get
                {
                    return Zone.ToString();
                }
            }
            public ElsiGenType GenType { get; set; }
            public string GenTypeName
            {
                get
                {
                    return GenType.ToString();
                }
            }
            public double Cost { get; set; }
            public double Capacity { get; set; }
            public double[] Capacities { get; set; }
        }

        public class StoreResult : GeneratorResult
        {
            public StoreResult(int numPeriods, ElsiMainZone zone, ElsiGenType genType, ElsiStorageMode mode) : base(numPeriods, zone, genType)
            {
                Mode = mode;
            }

            public StoreResult() {

            }

            public ElsiStorageMode Mode { get; set; }

            public string ModeName
            {
                get
                {
                    return Mode.ToString();
                }
            }

        }

        public class LinkResult
        {

            public LinkResult(int numPeriods, string name, ElsiMainZone fromZone, ElsiMainZone toZone)
            {
                Name = name;
                From = new LinkEndResult(numPeriods, fromZone);
                To = new LinkEndResult(numPeriods, toZone);
            }

            public LinkResult() {

            }
            
            public string Name { get; set; }

            public LinkEndResult From { get; set; }
            public LinkEndResult To { get; set; }

        }

        public class LinkEndResult
        {

            public LinkEndResult(int numPeriods, ElsiMainZone zone)
            {
                Capacities = new double[numPeriods];
                Zone = zone;
            }

            public LinkEndResult() {

            }

            public ElsiMainZone Zone { get; set; }

            public string ZoneName
            {
                get
                {
                    return Zone.ToString();
                }
            }
            public double Cost { get; set; }
            public double Capacity { get; set; }
            public double[] Capacities { get; set; }
        }


        public class MarginalPrice
        {
            public MarginalPrice(int numPeriods, ElsiMainZone zone) {
                Zone = zone;
                Prices = new double[numPeriods];
            }

            public MarginalPrice() {

            }

            public ElsiMainZone Zone { get; set; }
            public string ZoneName
            {
                get
                {
                    return Zone.ToString();
                }
            }
            public double Cost {get; set;}
            public double Capacity {get; set;}
            public double[] Prices { get; set; }
        }

        public class StorePrice : MarginalPrice
        {
            public StorePrice(int numPeriods, ElsiMainZone zone, ElsiGenType genType, ElsiStorageMode mode) : base(numPeriods, zone) {
                GenType = genType;
                Mode = mode;
            }

            public StorePrice() {

            }

            public ElsiGenType GenType {get; set;}
            public string GenTypeName {
                get {
                    return GenType.ToString();
                }
            }

            public ElsiStorageMode Mode { get; set; }
            public string ModeName
            {
                get
                {
                    // First character i.e. G or P
                    return Mode.ToString()[0].ToString();
                }
            }

        }

        public class LinkPrice
        {
            public LinkPrice(int numPeriods, string name, ElsiMainZone from,ElsiMainZone to ) {
                Name = name;
                From = new MarginalPrice(numPeriods, from);
                To = new MarginalPrice(numPeriods, to);
            }

            public LinkPrice() {

            }

            public string Name { get; set; }
            public MarginalPrice From {get; set;}
            public MarginalPrice To {get; set;}
        }

        public class ZoneEmissionsRate
        {
            public ZoneEmissionsRate(int numPeriods, ElsiMainZone zone) {
                Zone = zone;
                Rates = new double[numPeriods];
            }

            public ZoneEmissionsRate() {

            }

            public ElsiMainZone Zone { get; set; }
            public string ZoneName {
                get {
                    return Zone.ToString();
                }
            }
            public double[] Rates { get; set; }
        }

        public class MiscData
        {
            public MiscData(int numPeriods) {
                Iters = new int[numPeriods];
                ProductionCosts = new double[numPeriods];
            }

            public MiscData() {

            }

            public int[] Iters { get; set; }
            public double[] ProductionCosts { get; set; }
            public double DayError { get; set; }
        }

        public class ProductionCostDiffs {
            public ProductionCostDiffs(int numPeriods)  {
                Diffs = new double[numPeriods];
            }

            public ProductionCostDiffs() {

            }

            public double[] Diffs {get; set;}

        }

        public class BalanceMechanismResults {
            private ModelManager _mm;
            private ElsiDayResult _dayResult;
            private int _numPeriods;
            public BalanceMechanismResults(ElsiDayResult dayResult) {
                _numPeriods = Enum.GetValues<ElsiPeriod>().Count();
                _dayResult = dayResult;
                _mm=_dayResult._mm;
                MarketInfo = new List<BalanceMechanismMarketInfo>();
                foreach( var market in _mm.Markets.Items) {
                    var mktInfo = new BalanceMechanismMarketInfo(_numPeriods,market);
                    this.MarketInfo.Add(mktInfo);
                    // generators
                    this.addGeneratorInfo(mktInfo);
                    // stores
                    this.addStoreInfo(mktInfo);
                    // links
                    this.addLinkInfo(mktInfo);
                }

            }

            public BalanceMechanismResults() {

            }

            private double getDoubleValue(object obj) {
                return obj!=null ? (double) obj : 0;
            }

            private void addGeneratorInfo(BalanceMechanismMarketInfo mktInfo) {
                var bidsDict = new Dictionary<int,object[,]>();
                var offersDict = new Dictionary<int,object[,]>();
                var perList = _mm.Data.Per.Items;

                object[,] oparray;
                for(int p=0;p<_numPeriods;p++) {
                    _mm.Gens.Outputs(_mm.PerLp[p],ModelConsts.d_bids,null, out oparray);
                    bidsDict.Add(p,oparray);
                    _mm.Gens.Outputs(_mm.PerLp[p],ModelConsts.d_offers,null, out oparray);
                    offersDict.Add(p,oparray);
                }

                int i=0;
                foreach( var bgr in _dayResult.Balance.GeneratorResults) {
                    var zdem = _mm.Data.ZDem.GetRow(bgr.Zone);
                    if  ( zdem.Market == mktInfo.MarketName) {
                        var info = new BalanceMechanismInfo(_numPeriods,bgr.Zone,bgr.GenType,ElsiStorageMode.Generation);
                        var mgr = _dayResult.Market.GeneratorResults[i];                        
                        double diff;
                        int j=0;
                        foreach( var cap in bgr.Capacities ) {

                            // BOA
                            diff = bgr.Capacities[j] - mgr.Capacities[j];
                            info.BOA[j] = diff;
                            mktInfo.LossChange[j]+= diff;

                            // Costs
                            if ( diff > 0 ) {
                                
                                info.Costs[j] = getDoubleValue(offersDict[j][i+1,1]) * diff * perList[j].Hours;
                            } else {
                                info.Costs[j] = getDoubleValue(bidsDict[j][i+1,1]) * diff * perList[j].Hours;
                            }
                            mktInfo.Total[j]+=info.Costs[j];
                            j++;
                        }
                        mktInfo.Info.Add(info);
                    }
                    i++;
                }
            }

            private void addStoreInfo(BalanceMechanismMarketInfo mktInfo) {
                var perList = _mm.Data.Per.Items;

                _mm.Gens.Outputs(_mm.DayLp,ModelConsts.d_bids,null, out object[,] bids);
                _mm.Gens.Outputs(_mm.DayLp,ModelConsts.d_offers,null, out object[,] offers);

                int i=0;                
                foreach( var bsr in _dayResult.Balance.StoreResults) {
                    var zdem = _mm.Data.ZDem.GetRow(bsr.Zone);
                    if  ( zdem.Market == mktInfo.MarketName) {
                        var info = new BalanceMechanismInfo(_numPeriods,bsr.Zone,bsr.GenType,bsr.Mode);
                        var msr = _dayResult.Market.StoreResults[i]; 
                        double diff;                       
                        int j=0;
                        foreach( var cap in bsr.Capacities ) {

                            //
                            diff = bsr.Capacities[j] - msr.Capacities[j];

                            // BOA
                            info.BOA[j] = diff;
                            mktInfo.LossChange[j]+=diff;

                            // Costs
                            if ( diff > 0 ) {
                                info.Costs[j] = getDoubleValue(offers[i+1,1]) * diff * perList[j].Hours;
                            } else {
                                info.Costs[j] = getDoubleValue(bids[i+1,1]) * diff * perList[j].Hours;
                            }
                            mktInfo.Total[j]+=info.Costs[j];

                            j++;
                        }
                        mktInfo.Info.Add(info);
                    }
                    i++;
                }
            }

            private void addLinkInfo(BalanceMechanismMarketInfo mktInfo) {
                var bidsDict = new Dictionary<int,object[,]>();
                var offersDict = new Dictionary<int,object[,]>();
                var perList = _mm.Data.Per.Items;

                object[,] oparray;
                for(int p=0;p<_numPeriods;p++) {
                    _mm.zmps(out object[,] margpObj);
                    var margp = _dayResult.getDoubleArray(margpObj);
                    _mm.Links.Outputs(_mm.PerLp[p],ModelConsts.d_bids,margp, out oparray);
                    bidsDict.Add(p,oparray);
                    _mm.Links.Outputs(_mm.PerLp[p],ModelConsts.d_offers,margp, out oparray);
                    offersDict.Add(p,oparray);
                }

                //
                int i=0;                
                foreach( var blr in _dayResult.Balance.LinkResults) {
                    var zdem1 = _mm.Data.ZDem.GetRow(blr.From.Zone);
                    var zdem2 = _mm.Data.ZDem.GetRow(blr.To.Zone);
                    // Only one in the market        
                    if  ( (zdem1.Market == mktInfo.MarketName) ^ (zdem2.Market == mktInfo.MarketName) ) {
                        var ber = (zdem1.Market == mktInfo.MarketName) ? blr.From : blr.To;
                        var info = new BalanceMechanismInfo(_numPeriods,ber.Zone,blr.Name);
                        var mlr = _dayResult.Market.LinkResults[i]; 
                        double diff;                       
                        int j=0;
                        foreach( var cap in ber.Capacities ) {
                            var mer = (zdem1.Market == mktInfo.MarketName) ? mlr.From : mlr.To;

                            // BOA
                            diff = ber.Capacities[j] - mer.Capacities[j];
                            info.BOA[j] = diff;
                            mktInfo.LossChange[j]+=diff;

                            // Costs
                            if ( diff > 0 ) {
                                info.Costs[j] = getDoubleValue(offersDict[j][i+1,1]) * diff * perList[j].Hours;
                            } else {
                                info.Costs[j] = getDoubleValue(bidsDict[j][i+1,1]) * diff * perList[j].Hours;
                            }
                            mktInfo.Total[j]+=info.Costs[j];

                            j++;
                        }
                        mktInfo.Info.Add(info);
                    }
                    i++;
                }
            }

            public List<BalanceMechanismMarketInfo> MarketInfo {get; set;}
        }

        public class BalanceMechanismMarketInfo {
            public BalanceMechanismMarketInfo(int numPeriods, string marketName) {
                MarketName = marketName;
                LossChange = new double[numPeriods];
                Total = new double[numPeriods];
                Info = new List<BalanceMechanismInfo>();
            }

            public BalanceMechanismMarketInfo() {

            }

            public string MarketName {get; set;}
            public List<BalanceMechanismInfo> Info {get; set;}
            public double[] LossChange {get; set;}
            public double[] Total {get; set;}
        }

        public class BalanceMechanismInfo {
            private BalanceMechanismInfo(int numPeriods) {
                BOA = new double[numPeriods];
                Costs = new double[numPeriods];
            }

            public BalanceMechanismInfo(int numPeriods, ElsiMainZone zone, ElsiGenType genType, ElsiStorageMode storageMode) : this(numPeriods)
            {
                Zone = zone;
                GenType = genType;
                StorageMode = storageMode;
                Type = genType.IsStorage() ? ElsiBalanceMechanismInfoType.Store : ElsiBalanceMechanismInfoType.Gen;
            }

            public BalanceMechanismInfo(int numPeriods, ElsiMainZone zone, string linkName) : this(numPeriods)
            {
                Zone = zone;
                LinkName = linkName;
                Type = ElsiBalanceMechanismInfoType.Link;
            }

            public BalanceMechanismInfo() {

            }

            public ElsiMainZone Zone { get; set; }

            public string ZoneName
            {
                get
                {
                    return Zone.ToString();
                }
            }
            public ElsiBalanceMechanismInfoType Type {get; set;}
            public string TypeName {
                get {
                    return Type.ToString();
                }
            }
            public ElsiStorageMode StorageMode {get; set;}
            public string StorageModeName {
                get {
                    return StorageMode.ToString();
                }
            }
            public ElsiGenType GenType { get; set; }
            public string GenTypeName
            {
                get
                {
                    return GenType.ToString();
                }
            }

            public string LinkName { get; set;}
            public double[] BOA { get; set; }
            public double[] Costs { get; set; }

        }

        public class MismatchResults {
            public MismatchResults(ElsiDayResult dayResult) {
                var numPeriods = Enum.GetValues<ElsiPeriod>().Count();
                Market = new double[numPeriods];
                Balance = new double[numPeriods];
                // Market
                for(int i=0;i<numPeriods;i++) {
                    double mm = 0;
                    // demands
                    foreach( var d in dayResult.Availability.DemandResults) {
                        mm+=d.Demands[i];
                    }
                    // generators
                    foreach( var gens in dayResult.Market.GeneratorResults) {
                        mm+=gens.Capacities[i];
                    }
                    // stores
                    foreach( var store in dayResult.Market.StoreResults) {
                        mm+=store.Capacities[i];
                    }
                    // links
                    foreach( var link in dayResult.Market.LinkResults) {
                        mm+=link.From.Capacities[i];
                        mm+=link.To.Capacities[i];
                    }
                    Market[i]=mm;                    
                }
                // Balance
                for(int i=0;i<numPeriods;i++) {
                    double mm = 0;
                    // demands
                    foreach( var d in dayResult.Availability.DemandResults) {
                        mm+=d.Demands[i];
                    }
                    // generators
                    foreach( var gens in dayResult.Balance.GeneratorResults) {
                        mm+=gens.Capacities[i];
                    }
                    // stores
                    foreach( var store in dayResult.Balance.StoreResults) {
                        mm+=store.Capacities[i];
                    }
                    // links
                    foreach( var link in dayResult.Balance.LinkResults) {
                        mm+=link.From.Capacities[i];
                        mm+=link.To.Capacities[i];
                    }
                    Balance[i]=mm;                    
                }
            }

            public MismatchResults() {

            }
            
            public double[] Market {get; set;}
            public double[] Balance {get; set;}
        }

    }

}