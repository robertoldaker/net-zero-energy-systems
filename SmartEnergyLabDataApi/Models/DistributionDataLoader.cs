using System.Text.Json.Serialization;
using HaloSoft.EventLogger;
using SmartEnergyLabDataApi.Data;

namespace SmartEnergyLabDataApi.Models
{
    public class DistributionDataLoader {

        private string BASE_ADDRESS = "https://connecteddata.nationalgrid.co.uk";
        private const string DATASET_NAME = "Distribution Substations";
        private TaskRunner? _taskRunner;

        //
        private ProcessInfo _gspInfo;
        private ProcessInfo _primaryInfo;
        private ProcessInfo _distInfo;

        //
        private List<List<DistributionData>> _dataRecords;

        //
        public DistributionDataLoader(TaskRunner? taskRunner) {
            _taskRunner = taskRunner;
            _gspInfo = new ProcessInfo();
            _primaryInfo = new ProcessInfo();
            _distInfo = new ProcessInfo();
        }

        public string Load() {
            string message = "";
            var ckanLoader = new CKANDataLoader(BASE_ADDRESS,"distribution-substations");
            var spd = ckanLoader.GetDatasetInfo(DATASET_NAME);
            message = new ConditionalDataLoader().Load(spd, ()=>{
                return processDistributionData(ckanLoader,spd);
            });
            //
            return message;
        }

        private class DistributionData {
            [JsonPropertyName("License Area")]
            public string LicenseArea {get; set;}
            [JsonPropertyName("Grid Supply Point Number")]
            public int GridSupplyPointNumber {get; set;}
            [JsonPropertyName("Grid Supply Point Name")]
            public string GridSupplyPointName {get; set;}
            [JsonPropertyName("Bulk Supply Point Number")]
            public int BulkSupplyPointNumber {get; set;}
            [JsonPropertyName("Bulk Supply Point Name")]
            public string BulkSupplyPointName {get; set;}
            [JsonPropertyName("Primary Number")]
            public int PrimaryNumber {get; set;}
            [JsonPropertyName("Primary Name")]
            public string PrimaryName {get; set;}
            [JsonPropertyName("HV Feeder")]
            public string HVFeeder {get; set;}
            [JsonPropertyName("Substation Name")]
            public string SubstationName {get; set;}
            [JsonPropertyName("Substation Number")]
            public int SubstationNumber {get; set;}
            [JsonPropertyName("Grid Reference")]
            public string GridReference {get; set;}
            public double LATITUDE {get; set;}
            public double LONGITUDE {get; set;}
            [JsonPropertyName("Day Max Demand")]
            public double DayMaxDemand {get; set;}
            [JsonPropertyName("Night Max Demand")]
            public double NightMaxDemand {get; set;}
            [JsonPropertyName("Substation Rating")]
            public double SubstationRating {get; set;}
            [JsonPropertyName("Transformer Headroom")]
            public string TransformerHeadroom {get; set;}
            [JsonPropertyName("LTC Count Total")]
            public int LCTCountTotal {get; set;}
            [JsonPropertyName("Energy Storage")]
            public double EnergyStorage {get; set;}
            [JsonPropertyName("Heat Pumps")]
            public int HeatPumps {get; set;}
            [JsonPropertyName("EV Chargers")]
            public int EVChargers {get; set;}
            [JsonPropertyName("Total LCT Capacity")]
            public double TotalLCTCapacity {get; set;}
            [JsonPropertyName("Total Generation Capacity")]
            public double TotalGenerationCapacity {get; set;}
            public double Solar {get; set;}
            public double Wind {get; set;}
            [JsonPropertyName("Bio Fuels")]
            public double BioFuels {get;set;}
            [JsonPropertyName("Water Generation")]
            public double WaterGeneration {get; set;}
            [JsonPropertyName("Waste Generation")]
            public double WasteGeneration {get; set;}
            [JsonPropertyName("Storage Generation")]
            public double StorageGeneration {get; set;}
            [JsonPropertyName("Fossil Fuels")]
            public double FossilFuels {get; set;}
            [JsonPropertyName("Other Generation")]
            public double OtherGeneration {get; set;}
            public int Customers {get; set;}
        } 

        private string processDistributionData(CKANDataLoader loader, CKANDataLoader.CKANDataset spd)  {

            _taskRunner?.Update("Loading Distribution Data records ...");
            _dataRecords = new List<List<DistributionData>>();
            var dds = loader.LoadInitial<DistributionData>(DATASET_NAME,5000);
            int totalRead=dds.result.records.Count;
            _dataRecords.Add(dds.result.records);
            updateProgress(totalRead,dds.result.total);
            var cont = dds.result.total>totalRead;
            while( cont ) {
                dds=loader.LoadNext<DistributionData>(dds);
                _dataRecords.Add(dds.result.records);
                updateProgress(totalRead,dds.result.total);
                totalRead+=dds.result.records.Count;
                cont = dds.result.total > totalRead;
            }

            _taskRunner?.Update("Processing GSPs ...");
            iterateRecords((records,dp)=> {
                dp.ProcessGSPs(records);
                Logger.Instance.LogInfoEvent($"GSPs {dp.GSPInfo.ToString()}");
                _gspInfo.Add(dp.GSPInfo);
            });


            _taskRunner?.Update("Processing Primary substations ...");
            iterateRecords((records,dp)=> {
                dp.ProcessPrimaries(records);
                Logger.Instance.LogInfoEvent($"Primaries {dp.PrimaryInfo.ToString()}");
                _primaryInfo.Add(dp.PrimaryInfo);
            });
/*
            _taskRunner?.Update("Processing Distribution substations ...");
            iterateRecords((records,dp)=>{
                dp.ProcessDists(records);
                Logger.Instance.LogInfoEvent($"Distribution {dp.DistInfo.ToString()}");
                _distInfo.Add(dp.DistInfo);
            });
            */

            var message = $"GSPs {_gspInfo}\n";
            message += $"Primaries {_primaryInfo}\n";
            message += $"Distribution {_distInfo}\n";

            return message;
        }

        private void updateProgress(int totalRead, int totalCount) {
            int percent = (100*totalRead)/totalCount;
            _taskRunner?.Update(percent);
        }

        private void iterateRecords(Action<List<DistributionData>,DataProcessor> action) {
            _taskRunner?.Update(0);
            int nrecords=0;
            foreach( var records in _dataRecords) {
                using ( var dp = new DataProcessor() ) {
                    action.Invoke(records,dp);
                    int percent = 100*nrecords/_dataRecords.Count;
                    _taskRunner?.Update(percent);
                    nrecords++;
                }
            }
        }

        private void processDistributionDataRecords(List<DistributionData> records) {
            using ( var dp = new DataProcessor() ) {
                dp.Run(records);
                // store results
                _gspInfo.Add(dp.GSPInfo);
                _primaryInfo.Add(dp.PrimaryInfo);
                _distInfo.Add(dp.DistInfo);
                var message = $"GSPs {_gspInfo}\n";
                message += $"Primaries {_primaryInfo}\n";
                message += $"Distribution {_primaryInfo}\n";
                Logger.Instance.LogInfoEvent(message);
            }
        }


        private class DataProcessor : IDisposable {
            private Dictionary<int,bool> _processedGSPs;
            private Dictionary<int,GridSupplyPoint> _addedGSPs;
            private Dictionary<int,PrimarySubstation> _addedPrimaries;
            private Dictionary<int,bool> _processedPrimaries;
            private Dictionary<int,DistributionSubstation> _addedDists;
            private DataAccess _da;
            private Dictionary<string,GeographicalArea> _gaDict;

            public DataProcessor() {
                _da = new DataAccess();
                _addedGSPs = new Dictionary<int,GridSupplyPoint>();
                _processedGSPs = new Dictionary<int, bool>();
                _addedPrimaries = new Dictionary<int,PrimarySubstation>();
                _processedPrimaries = new Dictionary<int, bool>();
                _addedDists = new Dictionary<int,DistributionSubstation>();
                GSPInfo = new ProcessInfo();
                PrimaryInfo = new ProcessInfo();
                DistInfo = new ProcessInfo();
                //
                _gaDict=new Dictionary<string,GeographicalArea>();
                //
                _gaDict.Add("East Midlands",_da.Organisations.GetGeographicalArea(DNOAreas.EastMidlands));
                _gaDict.Add("South Wales",_da.Organisations.GetGeographicalArea(DNOAreas.SouthWales));
                _gaDict.Add("South West",_da.Organisations.GetGeographicalArea(DNOAreas.SouthWestEngland));
                _gaDict.Add("West Midlands",_da.Organisations.GetGeographicalArea(DNOAreas.WestMidlands));
            }


            public void Run( List<DistributionData> dds) {

                // Process GSPs
                foreach( var dd in dds) {
                    processGSP(dd);
                }

                // Process Primaries
                foreach( var dd in dds) {
                    processPrimary(dd);
                }

                // Process Distributions
                foreach( var dd in dds) {
                    processDist(dd);
                }

                // add new ones to db
                foreach( var entry in _addedGSPs) {
                    //                    
                    _da.SupplyPoints.Add(entry.Value);
                }

                // add new ones to db
                foreach( var entry in _addedPrimaries) {
                    //                    
                    _da.Substations.Add(entry.Value);
                }

                // add new ones to db
                foreach( var entry in _addedDists) {
                    //                    
                    _da.Substations.Add(entry.Value);
                }
                //
                _da.CommitChanges();
            }

            public void ProcessGSPs(List<DistributionData> dds) {
                // Process GSPs
                foreach( var dd in dds) {
                    processGSP(dd);
                }

                // add new ones to db
                foreach( var entry in _addedGSPs) {
                    //                    
                    _da.SupplyPoints.Add(entry.Value);
                }

                //
                _da.CommitChanges();

            }

            public void ProcessPrimaries(List<DistributionData> dds) {

                // Process Primaries
                foreach( var dd in dds) {
                    processPrimary(dd);
                }


                // add new ones to db
                foreach( var entry in _addedPrimaries) {
                    //                    
                    _da.Substations.Add(entry.Value);
                }

                //
                _da.CommitChanges();

            }

            public void ProcessDists(List<DistributionData> dds) {

                // Process Distributions
                foreach( var dd in dds) {
                    processDist(dd);
                }

                // add new ones to db
                foreach( var entry in _addedDists) {
                    //                    
                    _da.Substations.Add(entry.Value);
                }
                //
                _da.CommitChanges();

            }

            private void processGSP(DistributionData dd) {
                if ( !hasGspBeenProcessed(dd) ) {
                    _processedGSPs.Add(dd.GridSupplyPointNumber,true);
                    var gsp = getExistingGridSupplyPoint(dd);
                    bool needsAdding = gsp==null;
                    if ( needsAdding) {
                        gsp = new GridSupplyPoint();
                    } 
                    //
                    GeographicalArea ga;
                    if ( _gaDict.TryGetValue(dd.LicenseArea, out ga)) {
                        gsp.GeographicalArea = ga;
                        gsp.DistributionNetworkOperator = ga.DistributionNetworkOperator;
                        var updated = updateGSP(gsp,dd);
                        if ( needsAdding ) {
                            _addedGSPs.Add(dd.GridSupplyPointNumber,gsp);
                            GSPInfo.NumAdded++;
                        } else if ( updated ) {
                            GSPInfo.NumModified++;
                        }
                    } else {
                        GSPInfo.NumIgnored++;
                    }
                } 
            }

            private bool hasGspBeenProcessed(DistributionData dd) {
                return _processedGSPs.ContainsKey(dd.GridSupplyPointNumber);
            }

            private void processPrimary(DistributionData dd) {
                if ( !hasPrimaryBeenProcessed(dd)) {
                    _processedPrimaries.Add(dd.PrimaryNumber,true);
                    var pss = getExistingPrimary(dd);
                    bool needsAdding = pss==null;
                    if ( needsAdding) {
                        pss = new PrimarySubstation();
                    } 
                    //
                    GeographicalArea ga;
                    if ( _gaDict.TryGetValue(dd.LicenseArea, out ga)) {
                        var gsp = getExistingGridSupplyPoint(dd);
                        if ( gsp!=null) {
                            pss.GridSupplyPoint = gsp;
                            pss.GeographicalArea = ga;
                            pss.DistributionNetworkOperator = ga.DistributionNetworkOperator;
                            var updated = updatePrimary(pss,dd);
                            if ( needsAdding ) {
                                _addedPrimaries.Add(dd.PrimaryNumber,pss);
                                PrimaryInfo.NumAdded++;
                            } else if ( updated ) {
                                PrimaryInfo.NumModified++;
                            }
                        } else {
                            PrimaryInfo.NumIgnored++;
                        }
                    } else {
                        PrimaryInfo.NumIgnored++;
                    }
                } 
            }
            private bool hasPrimaryBeenProcessed(DistributionData dd) {
                return _processedPrimaries.ContainsKey(dd.PrimaryNumber);
            }


            private void processDist(DistributionData dd) {
                var dss = getExistingDist(dd);
                bool needsAdding = dss==null;
                if ( needsAdding) {
                    dss = new DistributionSubstation();
                } 
                //
                var pss = getExistingPrimary(dd);
                if ( pss!=null) {
                    dss.PrimarySubstation = pss;
                    var updated = updateDist(dss,dd);
                    if ( needsAdding ) {
                        _addedDists.Add(dd.SubstationNumber,dss);
                        DistInfo.NumAdded++;
                    } else if ( updated ) {
                        DistInfo.NumModified++;
                    }
                } else {
                    DistInfo.NumIgnored++;
                }
            }

            private bool updateGSP(GridSupplyPoint gsp, DistributionData dd) {
                bool update = false;
                if (gsp.Name!=dd.GridSupplyPointName) {
                    gsp.Name=dd.GridSupplyPointName;
                    update = true;
                }
                if (gsp.NR!=dd.GridSupplyPointNumber.ToString()) {
                    gsp.NR=dd.GridSupplyPointNumber.ToString();
                    update = true;
                }
                return update;
            }

            private bool updatePrimary(PrimarySubstation pss, DistributionData dd) {
                bool update = false;
                if (pss.Name!=dd.PrimaryName) {
                    Logger.Instance.LogInfoEvent($"Updating primary name [{pss.Name}] [{dd.PrimaryName}]");
                    pss.Name=dd.PrimaryName;
                    update = true;
                }
                if (pss.NR!=dd.PrimaryNumber.ToString()) {
                    Logger.Instance.LogInfoEvent($"Updating primary number [{pss.NR}] [{dd.PrimaryNumber}]");
                    pss.NR=dd.PrimaryNumber.ToString();
                    update = true;
                }
                return update;
            }

            private bool updateDist(DistributionSubstation dss, DistributionData dd) {
                bool update = false;
                if (dss.Name!=dd.SubstationName) {
                    dss.Name=dd.SubstationName;
                    update = true;
                }
                if (dss.NR!=dd.SubstationNumber.ToString()) {
                    dss.NR=dd.SubstationNumber.ToString();
                    update = true;
                }
                return update;
            }

            private GridSupplyPoint getExistingGridSupplyPoint(DistributionData dd) {
                // first try list of ones to add
                if ( _addedGSPs.TryGetValue(dd.GridSupplyPointNumber,out GridSupplyPoint gsp) ) {
                    return gsp;
                }
                // if no result go to db and see if it exists there
                return _da.SupplyPoints.GetGridSupplyPointByNrOrName(dd.GridSupplyPointNumber.ToString(), dd.GridSupplyPointName);
            }

            private PrimarySubstation getExistingPrimary(DistributionData dd) {
                // first try list of ones to add
                if ( _addedPrimaries.TryGetValue(dd.PrimaryNumber,out PrimarySubstation pss)) {
                    return pss;
                }
                // if no result go to db and see if it exists there
                return _da.Substations.GetPrimarySubstationByNrOrName(dd.PrimaryNumber.ToString(), dd.PrimaryName);
            }

            private DistributionSubstation getExistingDist(DistributionData dd) {
                // first try list of ones to add
                if ( _addedDists.TryGetValue(dd.SubstationNumber,out DistributionSubstation dss)) {
                    return dss;
                }
                // if no result go to db and see if it exists there
                return _da.Substations.GetDistributionSubstationByNrOrName(dd.SubstationNumber.ToString(), dd.SubstationName);
            }

            public void Dispose()
            {
                _da.Dispose();
            }

            public ProcessInfo GSPInfo;
            public ProcessInfo PrimaryInfo;
            public ProcessInfo DistInfo;
        }

        private class ProcessInfo {
            public int NumAdded {get; set;}
            public int NumModified {get; set;}
            public int NumIgnored {get; set;}

            public void Add(ProcessInfo pi) {
                NumAdded+=pi.NumAdded;
                NumModified+=pi.NumModified;
                NumIgnored+=pi.NumIgnored;
            }

            public override string ToString() {
                return $"num added=[{NumAdded}], num modified=[{NumModified}], num ignored=[{NumIgnored}]";
            }
        }

    }



}