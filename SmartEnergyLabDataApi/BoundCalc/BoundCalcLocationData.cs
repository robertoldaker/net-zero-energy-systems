using System.Linq;
using Microsoft.Extensions.ObjectPool;
using NHibernate.Criterion;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Data.BoundCalc;
using SmartEnergyLabDataApi.Models;

namespace SmartEnergyLabDataApi.BoundCalc
{
    public class BoundCalcLocationData {
        public BoundCalcLocationData(BoundCalcNetworkData networdData) {
            // Locations
            var locDict = new Dictionary<GridSubstationLocation,BoundCalcLocation>();
            var nodes = networdData.Nodes.Data;
            var locs = networdData.Locations.Data;
            var ctrls = networdData.Ctrls.Data;
            var branches = networdData.Branches.Data;
            //
            foreach( var loc in locs) {
                var isQB = ctrls.Where( m=>m.Node1.Location?.Id == loc.Id).FirstOrDefault()!=null;
                locDict.Add(loc,new BoundCalcLocation(loc, isQB));
            }
            // ids of locations that have ctrls
            foreach(var node in nodes) {
                BoundCalcLocation loc;
                if ( node.Location!=null && locDict.ContainsKey(node.Location)) {
                    loc = locDict[node.Location];
                    loc.Nodes.Add(node);
                } 
            }
            Locations = locDict.Values.ToList();

            // Links - include branches that connect different locations
            var visibleBranches = branches.Where( 
                m=>m.Node1.Location!=null && 
                m.Node2.Location!=null && 
                m.Node1.Location.Id != m.Node2.Location.Id);
            var linkDict = new Dictionary<string,BoundCalcLink>();
            foreach( var b in visibleBranches) {
                var key1 = $"{b.Node1.Location.Id}:{b.Node2.Location.Id}";
                var key2 = $"{b.Node2.Location.Id}:{b.Node1.Location.Id}";
                BoundCalcLink link;
                if ( linkDict.ContainsKey(key1) ) {
                    link = linkDict[key1];
                    link.Branches.Add(b);
                } else if ( linkDict.ContainsKey(key2)) {
                    link = linkDict[key2];
                    link.Branches.Add(b);
                } else {
                    var ctrl = ctrls.Where( m=>m.Branch.Id == b.Id).FirstOrDefault();
                    var isHVDC = ctrl!=null && ctrl.Type == BoundCalcCtrlType.HVDC;
                    link = new BoundCalcLink(b,isHVDC);
                    linkDict.Add(key1,link);
                }
            }
            Links = linkDict.Values.ToList();
        }

        public IList<BoundCalcLocation> Locations {get; private set;}
        public IList<BoundCalcLink> Links {get; private set;}

    }

    public class BoundCalcLocation {
        private GridSubstationLocation _gsl;
        private List<Node> _nodes;
        private bool _isQB;
        public BoundCalcLocation(GridSubstationLocation loc, bool isQB) {
            _nodes = new List<Node>();
            _gsl = loc;
            _isQB = isQB;
        }
        public BoundCalcLocation(Node node, bool isQB) {
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

    public class BoundCalcLink {
        private List<Branch> _branches;
        public BoundCalcLink(Branch branch, bool isHVDC) {
            _branches = new List<Branch>();
            _branches.Add(branch);
            IsHVDC = isHVDC;
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

        public bool IsHVDC {  get; private set;}
    }
}
