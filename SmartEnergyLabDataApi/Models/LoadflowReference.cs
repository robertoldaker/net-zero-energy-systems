using System.ComponentModel;
using System.Text.Json.Serialization;
using Antlr.Runtime;
using Google.Protobuf.WellKnownTypes;
using HaloSoft.EventLogger;
using NHibernate.Linq.Clauses;
using NHibernate.Util;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Loadflow;

namespace SmartEnergyLabDataApi.Models;
public class LoadflowReference {

    private string getBaseFilename() {
        string folder = getReferenceFolder();
        return Path.Combine(folder,"LoadflowBase.xlsm");
    }

    private string getReferenceFolder() {
        string folder = Path.Combine(AppFolders.Instance.Uploads,"Loadflow","Reference");
        Directory.CreateDirectory(folder);
        return folder;
    }

    private string getB8Filename() {
        string folder = getReferenceFolder();
        return Path.Combine(folder,"LoadflowB8.xlsm");
    }

    public void LoadBase(IFormFile file) {
        string dest = getBaseFilename();

        using ( var fs = new FileStream(dest,FileMode.Create)) {
            using ( var sr=file.OpenReadStream() ) {
                sr.CopyTo(fs);
            }
        }
    }

    public void LoadB8(IFormFile file) {
        string dest = getB8Filename();

        using ( var fs = new FileStream(dest,FileMode.Create)) {
            using ( var sr=file.OpenReadStream() ) {
                sr.CopyTo(fs);
            }
        }
    }

    public LoadflowErrors RunBase(bool showAllErrors) {
        //
        if ( File.Exists(getBaseFilename())) {
            var loadflowErrors = new LoadflowErrors(showAllErrors);
            var m = new LoadflowXlsmReader();
            m.LoadResults(getBaseFilename());
            using( var lf = new Loadflow.Loadflow() ) {
                lf.RunBaseCase("Auto");
                var lfr = new LoadflowResults(lf);
                // nodes
                foreach( var nw in lfr.Nodes) {
                    if ( m.NodeResults.TryGetValue(nw.Obj.Code, out LoadflowXlsmReader.NodeResult nr)) {
                        loadflowErrors.AddNodeResult(nw.Obj.Code,nw,nr);
                    } else {
                        throw new Exception($"Could not find node [{nw.Obj.Code}] in ref spreadsheet");
                    }
                }
                // branches
                foreach( var bw in lfr.Branches ) {
                    if ( m.BranchResults.TryGetValue(bw.LineName, out LoadflowXlsmReader.BranchResult br)) {
                        loadflowErrors.AddBranchResult(bw.LineName,bw,br);
                    } else {
                        throw new Exception($"Could not find branch [{bw.LineName}] in ref spreadsheet");
                    }
                }
                // controls
                foreach( var cw in lfr.Ctrls ) {
                    if ( m.CtrlResults.TryGetValue(cw.Obj.Code, out LoadflowXlsmReader.CtrlResult cr)) {
                        loadflowErrors.AddCtrlResult(cw.Obj.Code,cw,cr);
                    } else {
                        throw new Exception($"Could not find ctrl [{cw.Obj.Code}] in ref spreadsheet");
                    }
                }
            }
            return loadflowErrors;
        } else {
            throw new Exception("No base reference has been loaded. Please load a base reference spreadsheet.");
        }

        //

    }

    public LoadflowErrors RunB8(bool showAllErrors) {
        //
        if ( File.Exists(getB8Filename())) {
            var loadflowErrors = new LoadflowErrors(showAllErrors);
            var m = new LoadflowXlsmReader();
            m.LoadResults(getB8Filename(),"B8");
            var boundaryName="B8";
            using( var lf = new Loadflow.Loadflow() ) {
                var bfr=lf.Boundary.RunAllBoundaryTrips(boundaryName, out List<AllTripsResult> singleTrips, out List<AllTripsResult> dualTrips);
                var lfr = new LoadflowResults(lf,bfr);
                // Single trips
                foreach( var st in singleTrips ) {
                    if ( m.SingleTripResults.TryGetValue(st.Trip.Text, out LoadflowXlsmReader.TripResult tr)) {
                        loadflowErrors.AddTripResult(st.Trip.Text,RefErrorType.SingleTrip,st,tr);
                    } else {
                        throw new Exception($"Could not find trip [{st.Trip.Text}] in ref spreadsheet");
                    }
                }
                // Dual trips
                foreach( var st in dualTrips ) {
                    if ( m.DualTripResults.TryGetValue(st.Trip.Text, out LoadflowXlsmReader.TripResult tr)) {
                        loadflowErrors.AddTripResult(st.Trip.Text,RefErrorType.DualTrip,st,tr);
                    } else {
                        //?? Server does all 2-trip combinations but spreadsheet only does some.
                        //??throw new Exception($"Could not find trip [{st.Trip.Text}] in ref spreadsheet");
                    }
                }
            }
            return loadflowErrors;
        } else {
            throw new Exception("No B8 reference has been loaded. Please load a B8 reference spreadsheet.");
        }

        //
    }

    public class LoadflowErrors {
        private List<RefError> _allErrors;
        public  bool _showAllErrors;
        public LoadflowErrors(bool showAllErrors) {
            _allErrors = new List<RefError>();
            _showAllErrors = showAllErrors;
        }

        public RefError MaxError {
            get {
                return _allErrors.OrderByDescending(m=>m.AbsDiff).First();
            }
        }

        public void AddNodeResult(string name, NodeWrapper nw, LoadflowXlsmReader.NodeResult cr) {
            var error = new RefError(name,RefErrorType.Node,"Mismatch",nw.Mismatch,cr.Mismatch);
            _allErrors.Add(error);
        }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<RefError> NodeErrors {
            get {
                var list = _showAllErrors ? filterAllErrors(RefErrorType.Node) : null;
                return list;
            }
        }

        public void AddBranchResult(string name, BranchWrapper bw, LoadflowXlsmReader.BranchResult br) {
            var bFlow = new RefError(name,RefErrorType.Branch,"Power flow",bw.PowerFlow,br.bFlow);
            _allErrors.Add(bFlow);
            double? fp = bw.FreePower==99999 ? null: bw.FreePower;
            var fPower = new RefError(name,RefErrorType.Branch,"Free power",fp,br.freePower);
            _allErrors.Add(fPower);
        }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<RefError> BranchErrors {
            get {
                var list = _showAllErrors ? filterAllErrors(RefErrorType.Branch) : null;
                return list;
            }
        }
        
        public void AddCtrlResult(string name, CtrlWrapper cw, LoadflowXlsmReader.CtrlResult cr) {
            var sp = new RefError(name,RefErrorType.Ctrl,"Set point",cw.SetPoint,cr.SetPoint);
            _allErrors.Add(sp);
        }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<RefError> ControlErrors {
            get {
                var list = _showAllErrors ? filterAllErrors(RefErrorType.Ctrl) : null;
                return list;
            }
        }

        private List<RefError> filterAllErrors(RefErrorType type) {
            var list = _allErrors.Where(m=>m.ObjectType == type).OrderByDescending(m=>m.AbsDiff).ToList();
            return list;
        }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<RefError> SingleTripErrors {
            get {
                var list = _showAllErrors ? filterAllErrors(RefErrorType.SingleTrip) : null;
                return list;
            }
        }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<RefError> DualTripErrors {
            get {
                var list = _showAllErrors ? filterAllErrors(RefErrorType.DualTrip) : null;
                return list;
            }
        }
        public void AddTripResult(string name, RefErrorType type,AllTripsResult trc, LoadflowXlsmReader.TripResult trr) {
            var sp = new RefError(name,type,"Surplus",trc.Capacity,trr.Capacity);
            _allErrors.Add(sp);
            var cap = new RefError(name,type,"Capacity",trc.Capacity,trr.Capacity);
            _allErrors.Add(cap);
            foreach( var ct in trc.Ctrls) {
                if ( trr.SetPointDict.ContainsKey(ct.Code)) {
                    var ctrError = new RefError(name,type,ct.Code,ct.SetPoint,trr.SetPointDict[ct.Code]);
                    _allErrors.Add(ctrError);
                }
            }
        }
    }

    public enum RefErrorType { Node, Branch, Ctrl, SingleTrip, DualTrip }

    public class RefError {  
        public RefError(string objName,RefErrorType objType,string var, double? calc, double? r) {
            ObjectName = objName;
            ObjectType = objType;
            Variable=var;
            Ref = r;
            Calc = calc;
        }
        public string ObjectName {get; set;}
        public RefErrorType ObjectType {get; set;}
        public string ObjectTypeStr {
            get {
                return ObjectType.ToString();
            }
        }
        public string Variable {get; set;}
        public double? Ref {get; set;}
        public double? Calc {get; set;}
        public double AbsDiff {
            get {
                if ( Ref!=null && Calc!=null) {
                    return Math.Abs((double) Calc - (double) Ref);
                } else if ( Ref!=null) {
                    return Math.Abs((double) Ref);
                } else if ( Calc!=null ) {
                    return Math.Abs((double) Calc);
                } else {
                    return 0;
                }
            }
        }
    }

}

