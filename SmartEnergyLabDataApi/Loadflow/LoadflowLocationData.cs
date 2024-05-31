using System.Linq;
using NHibernate.Criterion;
using SmartEnergyLabDataApi.Data;

namespace SmartEnergyLabDataApi.Loadflow
{
    public class LoadflowLocationData {
        public LoadflowLocationData(int datasetId) {
            using( var da = new DataAccess()) {
                // Locations
                var locDict = new Dictionary<GridSubstationLocation,LoadflowLocation>();
                var nodes = da.Loadflow.GetNodesWithLocations(datasetId);
                // ids of locations that have ctrls
                var qbIds = da.NationalGrid.GetGridSubstationLocationsForLoadflowCtrls();
                foreach(var node in nodes) {
                    LoadflowLocation loc;
                    if ( locDict.ContainsKey(node.Location)) {
                        loc = locDict[node.Location];
                        loc.Nodes.Add(node);
                    } else {
                        var isQB = qbIds.Contains(node.Location.Id);
                        loc = new LoadflowLocation(node,isQB);
                        locDict.Add(node.Location,loc);
                    }
                }
                Locations = locDict.Values.ToList();

                // Links
                var visibleBranches = da.Loadflow.GetVisibleBranches(datasetId);
                var linkDict = new Dictionary<string,LoadflowLink>();
                foreach( var b in visibleBranches) {
                    var key1 = $"{b.Node1.Location.Id}:{b.Node2.Location.Id}";
                    var key2 = $"{b.Node2.Location.Id}:{b.Node1.Location.Id}";
                    LoadflowLink link;
                    if ( linkDict.ContainsKey(key1) ) {
                        link = linkDict[key1];
                        link.Branches.Add(b);
                    } else if ( linkDict.ContainsKey(key2)) {
                        link = linkDict[key2];
                        link.Branches.Add(b);
                    } else {
                        link = new LoadflowLink(b);
                        linkDict.Add(key1,link);
                    }
                }
                Links = linkDict.Values.ToList();
            }
        }

        public IList<LoadflowLocation> Locations {get; private set;}
        public IList<LoadflowLink> Links {get; private set;}

    }

    public class LoadflowLocation {
        private GridSubstationLocation _gsl;
        private List<Node> _nodes;
        private bool _isQB;
        public LoadflowLocation(Node node, bool isQB) {
            _nodes = new List<Node>();
            _nodes.Add(node);
            _gsl = node.Location;
            _isQB = isQB;
        }
        public int Id {
            get {
                return _gsl.Id;
            }
        }
        public string Name {
            get {
                return _gsl.Name;
            }            
        }

        public List<Node> Nodes {
            get {
                return _nodes;
            }
        }

        public string Reference {
            get {
                return _gsl.Reference;
            }            
        }

        public GISData GISData {
            get {
                return _gsl.GISData;
            }
        }

        public bool IsQB {
            get {
                return _isQB;
            }
        }
    }

    public class LoadflowLink {
        private List<Branch> _branches;
        public LoadflowLink(Branch branch) {
            _branches = new List<Branch>();
            _branches.Add(branch);
        }

        public int Id {
            get {
                return _branches[0].Id;
            }
        }

        public List<Branch> Branches {
            get {
                return _branches;
            }
        }

        public int Voltage {
            get {
                return _branches[0].Node1.Voltage;
            }
        }

        public GISData GISData1 {
            get {
                return _branches[0].Node1.Location.GISData;
            }
        }

        public GISData GISData2 {
            get {
                return _branches[0].Node2.Location.GISData;
            }
        }
    }
}
