using System.Linq;
using System.Text.Json.Serialization;
using HaloSoft.EventLogger;
using Microsoft.Extensions.ObjectPool;
using NHibernate;
using NHibernate.Criterion;
using Org.BouncyCastle.Asn1.Icao;
using Org.BouncyCastle.Crypto.Signers;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Data.BoundCalc;

namespace SmartEnergyLabDataApi.BoundCalc
{
    public class BoundCalcNetworkData {
        public BoundCalcNetworkData(BoundCalc bc)
        {
            // Nodes
            Nodes = bc.Nodes.DatasetData;
            // Branches
            Branches = bc.Branches.DatasetData;
            // Controls
            Ctrls = bc.Ctrls.DatasetData;
            // Boundaries
            Boundaries = bc.Boundaries.DatasetData;
            // Zones
            Zones = bc.Zones;
            // Generators
            Generators = bc.Generators;
            //
            using (var da = new DataAccess()) {
                // Locations
                Locations = loadLocations(da, bc.Dataset.Id);
                // Transport models
                TransportModels = loadTransportModels(da, bc.Dataset.Id);
                // Transport model entries
                TransportModelEntries = loadTransportModelEntries(da, bc.Dataset.Id);
                //
                setTransportModelScalings(da, bc.Dataset.Id);
                //
                //?? Test to see if we can connect GSPs from the Elexon data
                //??assignIsGGSP(da);
            }
            // Boundary branches
            BoundaryDict = new Dictionary<string, int[]>();
            foreach (var b in bc.Boundaries.Objs) {
                var boundaries = b.BoundCcts.Items.Select(m => m.Obj.Id).ToArray();
                BoundaryDict.Add(b.name, boundaries);
            }
            //
            TransportModel = bc.TransportModel;
        }

        public DatasetData<Node> Nodes { get; private set; }
        public DatasetData<Branch> Branches { get; private set; }
        public DatasetData<Ctrl> Ctrls { get; private set; }
        public DatasetData<Boundary> Boundaries { get; private set; }
        public DatasetData<Zone> Zones { get; private set; }
        public DatasetData<GridSubstationLocation> Locations { get; private set; }
        public DatasetData<Generator> Generators { get; private set; }
        public DatasetData<TransportModel> TransportModels { get; private set; }
        public TransportModel? TransportModel { get; private set; }
        public DatasetData<TransportModelEntry> TransportModelEntries { get; private set; }
        public Dictionary<string, int[]> BoundaryDict { get; private set; }
        private DatasetData<GridSubstationLocation> loadLocations(DataAccess da, int datasetId)
        {
            var q = da.Session.QueryOver<GridSubstationLocation>();
            var locs = new DatasetData<GridSubstationLocation>(da, datasetId, m => m.Id.ToString(), q);
            return locs;
        }

        private DatasetData<Generator> loadGenerators(DataAccess da, int datasetId)
        {
            var q = da.Session.QueryOver<Generator>();
            var objs = new DatasetData<Generator>(da, datasetId, m => m.Id.ToString(), q);
            return objs;
        }

        private DatasetData<TransportModel> loadTransportModels(DataAccess da, int datasetId)
        {
            var data = da.BoundCalc.GetTransportModelDatasetData(datasetId, null, true);
            return data;
        }

        private DatasetData<NodeGenerator> loadNodeGenerators(DataAccess da, int datasetId)
        {
            var q = da.Session.QueryOver<NodeGenerator>();
            var objs = new DatasetData<NodeGenerator>(da, datasetId, m => m.Id.ToString(), q);
            return objs;
        }

        private DatasetData<TransportModelEntry> loadTransportModelEntries(DataAccess da, int datasetId)
        {
            var q = da.Session.QueryOver<TransportModelEntry>();
            var objs = new DatasetData<TransportModelEntry>(da, datasetId, m => m.Id.ToString(), q);
            return objs;
        }

        private void setTransportModelScalings(DataAccess da, int datasetId)
        {
            var nodeGenDi = da.BoundCalc.GetNodeGeneratorDatasetData(datasetId, null);
            foreach (var tm in this.TransportModels.Data) {
                tm.UpdateScaling(Nodes.Data, nodeGenDi.Data, this.Generators.Data);
            }
        }

        private void assignNodeLocations()
        {
            // create dictionary using ref. as key
            var locs = Locations.Data;
            var nodes = Nodes.Data;
            var locDict = new Dictionary<string, GridSubstationLocation>();
            foreach (var loc in locs) {
                if (!locDict.ContainsKey(loc.Reference)) {
                    locDict.Add(loc.Reference, loc);
                }
            }
            // look up node location based on first 4 chars of code
            foreach (var n in nodes) {
                var locCode = n.Code.Substring(0, 4);
                if (n.Ext) {
                    locCode += "X";
                }
                if (locDict.ContainsKey(locCode)) {
                    n.Location = locDict[locCode];
                }
            }

        }

        private void assignIsGGSP(DataAccess da)
        {
            Logger.Instance.LogInfoEvent("Started finding GSPs for nodes");
            var dates = da.Elexon.GetGspDemandDates();
            var endDate = dates[dates.Count - 1];
            var profiles = da.Elexon.GetGspDemandProfiles(endDate, endDate, null);
            var codes = profiles.Select(m => m.GspCode).Distinct().ToList();
            var codeDict = new Dictionary<string, int>(codes.Count);
            foreach (var code in codes) {
                codeDict.Add(code, 0);
            }
            List<Node> missingGspNodes = new List<Node>();
            foreach (var node in Nodes.Data) {
                var nodeRef = node.Code.Substring(0, 4);
                // often demand are < 0 as well as > 0
                if (node.Demand != 0 && codeDict.TryGetValue(nodeRef, out int count)) {
                    node.IsGSP = true;
                    count++;
                    codeDict[nodeRef] = count;
                } else {
                    node.IsGSP = false;
                    if (node.Demand != 0) {
                        missingGspNodes.Add(node);
                    }
                }
            }
            //
            var nonRefGSPs = codeDict.Where(m => m.Value == 0).Select(m => m.Key).OrderBy(m => m).ToList();
            //foreach (var gsp in nonRefGSPs) {
            //    Logger.Instance.LogInfoEvent($"GSP not referenced by nodes [{gsp}]");
            //}

            double demand = 0;
            int missingNodes = 0;
            foreach (var node in missingGspNodes) {
                var gspGroupId = getGspDemandGroupId(node.Dem_zone);
                var poss1 = profiles.Where(m => nonRefGSPs.Contains(m.GspCode)).ToList();
                var poss2 = poss1.Where(m => m.GspGroupId == gspGroupId && m.GspCode.StartsWith(node.Code.Substring(0, 3))).ToList();
                if (poss2.Count == 1) {
                    codeDict[poss2[0].GspCode]++;
                } else {
                    demand += node.Demand;
                    missingNodes++;
                    Logger.Instance.LogInfoEvent($"Cannot find GSP for node [{node.Code}] [{node.Demand}]");
                }
            }
            nonRefGSPs = codeDict.Where(m => m.Value == 0).Select(m => m.Key).OrderBy(m => m).ToList();
            foreach (var gsp in nonRefGSPs) {
                Logger.Instance.LogInfoEvent($"GSP not referenced by nodes [{gsp}]");
            }
            Logger.Instance.LogInfoEvent($"Finished finding GSPs for nodes, total nodes with missing GSP [{missingNodes}], demand=[{demand}], GSPs not ref. by nodes=[{nonRefGSPs.Count}]");
        }

        private string getGspDemandGroupId(int? dem_zone) {
            if (dem_zone == 1) {
                return "_P";
            } else if (dem_zone == 2) {
                return "_N";
            } else if (dem_zone == 3) {
                return "_F";
            } else if (dem_zone == 4) {
                return "_G";
            } else if (dem_zone == 5) {
                return "_M";
            } else if (dem_zone == 6) {
                return "_D";
            } else if (dem_zone == 7) {
                return "_B";
            } else if (dem_zone == 8) {
                return "_E";
            } else if (dem_zone == 9) {
                return "_A";
            } else if (dem_zone == 10) {
                return "_K";
            } else if (dem_zone == 11) {
                return "J";
            } else if (dem_zone == 12) {
                return "_C";
            } else if (dem_zone == 13) {
                return "_H";
            } else if (dem_zone == 14) {
                return "_L";
            } else {
                throw new Exception($"Unexpected value for dmeand zone [{dem_zone}]");
            }
        }

    }
}
