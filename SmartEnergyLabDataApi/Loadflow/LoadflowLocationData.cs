using System.Linq;
using NHibernate.Criterion;
using SmartEnergyLabDataApi.Data;

namespace SmartEnergyLabDataApi.Loadflow
{
    public class LoadflowLocationData {
        public LoadflowLocationData() {
            using( var da = new DataAccess()) {
                // Locations
                var locations = da.NationalGrid.GetGridSubstationLocationsForLoadflow();
                var qbIds = da.NationalGrid.GetGridSubstationLocationsForLoadflowCtrls();
                Locations = locations.Select(m=>new LoadflowLocation(m,qbIds.Contains(m.Id))).ToList();

                // Branches
                var visibleBranches = da.Loadflow.GetVisibleBranches();
                Branches = visibleBranches.Select(m=>new LoadflowBranch(m)).ToList();
            }
        }

        public IList<LoadflowLocation> Locations {get; private set;}
        public IList<LoadflowBranch> Branches {get; private set;}

    }

    public class LoadflowLocation {
        private GridSubstationLocation _gsl;
        private bool _isQB;
        public LoadflowLocation(GridSubstationLocation gridLocation, bool isQB) {
            _gsl = gridLocation;
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

    public class LoadflowBranch {
        private Branch _b;
        public LoadflowBranch(Branch branch) {
            _b = branch;
        }

        public int Id {
            get {
                return _b.Id;
            }
        }

        public Branch Branch {
            get {
                return _b;
            }
        }

        public int Voltage {
            get {
                return _b.Node1.Voltage;
            }
        }

        public GISData GISData1 {
            get {
                return _b.Node1.Location.GISData;
            }
        }
        public GISData GISData2 {
            get {
                return _b.Node2.Location.GISData;
            }
        }
    }
}
