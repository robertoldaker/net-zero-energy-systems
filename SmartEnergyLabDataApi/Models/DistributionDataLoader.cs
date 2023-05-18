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
            [JsonPropertyName("Primary Numbe")]
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

            var dds = loader.LoadInitial<DistributionData>(DATASET_NAME,1000);
            int totalRead=dds.result.records.Count;
            processDistributionDataRecords(dds.result.records);
            updateProgress(totalRead,dds.result.total);
            var cont = dds.result.total>totalRead;
            while( cont ) {
                dds=loader.LoadNext<DistributionData>(dds);
                processDistributionDataRecords(dds.result.records);
                updateProgress(totalRead,dds.result.total);
                totalRead+=dds.result.records.Count;
                cont = dds.result.total > totalRead;
            }

            var message = $"GSPs {_gspInfo}\n";
            message += $"Primaries {_primaryInfo}\n";
            message += $"Distribution {_primaryInfo}\n";

            return message;
        }

        private void updateProgress(int totalRead, int totalCount) {
            int percent = (100*totalRead)/totalCount;
            _taskRunner?.Update(percent);
        }

        private void processDistributionDataRecords(List<DistributionData> records) {
            using ( var dp = new DataProcessor() ) {
                dp.Run(records);
                // store results
                _gspInfo.Add(dp.GSPInfo);
                _primaryInfo.Add(dp.PrimaryInfo);
                _distInfo.Add(dp.DistInfo);
            }
        }


        private class DataProcessor : IDisposable {
            private List<GridSupplyPoint> _addedGSPs;
            private DataAccess _da;

            public DataProcessor() {
                _da = new DataAccess();
                _addedGSPs = new List<GridSupplyPoint>();
                GSPInfo = new ProcessInfo();
                PrimaryInfo = new ProcessInfo();
                DistInfo = new ProcessInfo();
            }


            public void Run( List<DistributionData> dds) {
                //
                var gaDict=new Dictionary<string,GeographicalArea>();
                //
                gaDict.Add("East Midlands",_da.Organisations.GetGeographicalArea(DNOAreas.EastMidlands));
                gaDict.Add("South Wales",_da.Organisations.GetGeographicalArea(DNOAreas.SouthWales));
                gaDict.Add("South West",_da.Organisations.GetGeographicalArea(DNOAreas.SouthWestEngland));
                gaDict.Add("West Midlands",_da.Organisations.GetGeographicalArea(DNOAreas.WestMidlands));
                //
                foreach( var dd in dds) {
                    //                    
                    var gsp = getExistingGridSupplyPoint(dd);
                    bool needsAdding = gsp==null;
                    if ( needsAdding) {
                        gsp = new GridSupplyPoint();
                    } 
                    //
                    GeographicalArea ga;
                    if ( gaDict.TryGetValue(dd.LicenseArea, out ga)) {
                        gsp.GeographicalArea = ga;
                        gsp.DistributionNetworkOperator = ga.DistributionNetworkOperator;
                        var updated = updateGSP(gsp,dd);
                        if ( needsAdding ) {
                            _addedGSPs.Add(gsp);
                            GSPInfo.NumAdded++;
                        } else if ( updated ) {
                            GSPInfo.NumModified++;
                        }
                    } else {
                        GSPInfo.NumIgnored++;
                    }
                }

                // add new ones to db
                foreach( var gsp in _addedGSPs) {
                    //                    
                    Logger.Instance.LogInfoEvent($"Added GSP=[{gsp.Name}] [{gsp.GeographicalArea.Name}]");
                    _da.SupplyPoints.Add(gsp);
                }

                //
                _da.CommitChanges();
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

            private GridSupplyPoint getExistingGridSupplyPoint(DistributionData dd) {
                // first try list of ones to add
                var gsp = _addedGSPs.Where( m=>m.NR == dd.GridSupplyPointNumber.ToString() || m.Name==dd.GridSupplyPointName).FirstOrDefault();
                if ( gsp!=null) {
                    return gsp;
                }
                // if no result go to db and see if it exists there
                return _da.SupplyPoints.GetGridSupplyPointByNrOrName(dd.GridSupplyPointNumber.ToString(), dd.GridSupplyPointName);
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