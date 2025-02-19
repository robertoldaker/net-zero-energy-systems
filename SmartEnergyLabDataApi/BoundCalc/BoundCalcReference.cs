using System.ComponentModel;
using System.Text.Json.Serialization;
using Antlr.Runtime;
using Google.Protobuf.WellKnownTypes;
using HaloSoft.EventLogger;
using NHibernate.Linq.Clauses;
using NHibernate.Util;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.BoundCalc;
using SmartEnergyLabDataApi.Models;

namespace SmartEnergyLabDataApi.BoundCalc;
public class BoundCalcReference {

    private string getBaseFilename() {
        string folder = getReferenceFolder();
        return Path.Combine(folder,"BoundCalcBase.xlsm");
    }

    private string getReferenceFolder() {
        string folder = Path.Combine(AppFolders.Instance.Uploads,"BoundCalc","Reference");
        Directory.CreateDirectory(folder);
        return folder;
    }

    private string getB8Filename() {
        string folder = getReferenceFolder();
        return Path.Combine(folder,"BoundCalcB8.xlsm");
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

    public BoundCalcErrors RunBase(bool showAllErrors) {
        //
        if ( File.Exists(getBaseFilename())) {
            var BoundCalcErrors = new BoundCalcErrors(showAllErrors);
            var m = new BoundCalcXlsmReader();
            m.LoadResults(getBaseFilename());
            Dataset ds;
            using( var da = new DataAccess() ) {
                var name = "GB network";
                ds = da.Datasets.GetDataset(DatasetType.BoundCalc,name);
                if ( ds==null) {
                    throw new Exception($"Cannot find dataset [{name}]");
                }
            }
            using( var bc = new BoundCalc(ds.Id) ) {
                bc.RunBoundCalc(null,null,BoundCalc.SPAuto,false,true); 
                var lfr = new BoundCalcResults(bc);
                // nodes
                foreach( var nw in bc.Nodes.Objs) {
                    if ( m.NodeResults.TryGetValue(nw.Obj.Code, out BoundCalcXlsmReader.NodeResult nr)) {
                        BoundCalcErrors.AddNodeResult(nw.Obj.Code,nw,nr);
                    } else {
                        throw new Exception($"Could not find node [{nw.Obj.Code}] in ref spreadsheet");
                    }
                }
                // branches
                foreach( var bw in bc.Branches.Objs ) {
                    if ( m.BranchResults.TryGetValue(bw.LineName, out BoundCalcXlsmReader.BranchResult br)) {
                        BoundCalcErrors.AddBranchResult(bw.LineName,bw,br);
                    } else {
                        throw new Exception($"Could not find branch [{bw.LineName}] in ref spreadsheet");
                    }
                }
                // controls
                foreach( var cw in bc.Ctrls.Objs ) {
                    if ( m.CtrlResults.TryGetValue(cw.Obj.Code, out BoundCalcXlsmReader.CtrlResult cr)) {
                        BoundCalcErrors.AddCtrlResult(cw.Obj.Code,cw,cr);
                    } else {
                        throw new Exception($"Could not find ctrl [{cw.Obj.Code}] in ref spreadsheet");
                    }
                }
            }
            return BoundCalcErrors;
        } else {
            throw new Exception("No base reference has been loaded. Please load a base reference spreadsheet.");
        }

        //

    }

    public BoundCalcErrors RunB8(bool showAllErrors) {
        //
        if ( File.Exists(getB8Filename())) {
            var BoundCalcErrors = new BoundCalcErrors(showAllErrors);
            var m = new BoundCalcXlsmReader();
            m.LoadResults(getB8Filename(),"B8");
            var boundaryName="B8";
            Dataset ds;
            using( var da = new DataAccess() ) {
                var name = "GB network";
                ds = da.Datasets.GetDataset(DatasetType.BoundCalc,name);
                if ( ds==null) {
                    throw new Exception($"Cannot find dataset [{name}]");
                }
            }
            using( var bc = new BoundCalc(ds.Id) ) {
                var bnd = bc.Boundaries.GetBoundary(boundaryName);
                if ( bnd == null ) {
                    throw new Exception($"Cannot find boundary with name [{boundaryName}]");
                }
                bc.RunAllTrips(bnd, BoundCalc.SPAuto);

                var lfr = new BoundCalcResults(bc);
                // Single trips
                foreach( var st in bc.SingleTrips ) {
                    if ( m.SingleTripResults.TryGetValue(st.Trip.Text, out BoundCalcXlsmReader.TripResult tr)) {
                        BoundCalcErrors.AddTripResult(st.Trip.Text,BoundCalcRefErrorType.SingleTrip,st,tr);
                    } else {
                        throw new Exception($"Could not find trip [{st.Trip.Text}] in ref spreadsheet");
                    }
                }
                // Dual trips
                foreach( var st in bc.DoubleTrips ) {
                    if ( m.DualTripResults.TryGetValue(st.Trip.Text, out BoundCalcXlsmReader.TripResult tr)) {
                        BoundCalcErrors.AddTripResult(st.Trip.Text,BoundCalcRefErrorType.DualTrip,st,tr);
                    } else {
                        //?? Server does all 2-trip combinations but spreadsheet only does some.
                        //??throw new Exception($"Could not find trip [{st.Trip.Text}] in ref spreadsheet");
                    }
                }
            }
            return BoundCalcErrors;
        } else {
            throw new Exception("No B8 reference has been loaded. Please load a B8 reference spreadsheet.");
        }

        //
    }

    public class BoundCalcErrors {
        private List<BoundCalcRefError> _allErrors;
        public  bool _showAllErrors;
        public BoundCalcErrors(bool showAllErrors) {
            _allErrors = new List<BoundCalcRefError>();
            _showAllErrors = showAllErrors;
        }

        public BoundCalcRefError MaxError {
            get {
                return _allErrors.OrderByDescending(m=>m.AbsDiff).First();
            }
        }

        public void AddNodeResult(string name, NodeWrapper nw, BoundCalcXlsmReader.NodeResult cr) {
            var error = new BoundCalcRefError(name,BoundCalcRefErrorType.Node,"Mismatch",nw.Mismatch,cr.Mismatch);
            _allErrors.Add(error);
        }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<BoundCalcRefError> NodeErrors {
            get {
                var list = _showAllErrors ? filterAllErrors(BoundCalcRefErrorType.Node) : null;
                return list;
            }
        }

        public void AddBranchResult(string name, BranchWrapper bw, BoundCalcXlsmReader.BranchResult br) {
            var bFlow = new BoundCalcRefError(name,BoundCalcRefErrorType.Branch,"Power flow",bw.PowerFlow,br.bFlow);
            _allErrors.Add(bFlow);
            double? fp = bw.FreePower==99999 ? null: bw.FreePower;
            var fPower = new BoundCalcRefError(name,BoundCalcRefErrorType.Branch,"Free power",fp,br.freePower);
            _allErrors.Add(fPower);
        }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<BoundCalcRefError> BranchErrors {
            get {
                var list = _showAllErrors ? filterAllErrors(BoundCalcRefErrorType.Branch) : null;
                return list;
            }
        }
        
        public void AddCtrlResult(string name, CtrlWrapper cw, BoundCalcXlsmReader.CtrlResult cr) {
            var sp = new BoundCalcRefError(name,BoundCalcRefErrorType.Ctrl,"Set point",cw.SetPoint,cr.SetPoint);
            _allErrors.Add(sp);
        }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<BoundCalcRefError> ControlErrors {
            get {
                var list = _showAllErrors ? filterAllErrors(BoundCalcRefErrorType.Ctrl) : null;
                return list;
            }
        }

        private List<BoundCalcRefError> filterAllErrors(BoundCalcRefErrorType type) {
            var list = _allErrors.Where(m=>m.ObjectType == type).OrderByDescending(m=>m.AbsDiff).ToList();
            return list;
        }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<BoundCalcRefError> SingleTripErrors {
            get {
                var list = _showAllErrors ? filterAllErrors(BoundCalcRefErrorType.SingleTrip) : null;
                return list;
            }
        }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<BoundCalcRefError> DualTripErrors {
            get {
                var list = _showAllErrors ? filterAllErrors(BoundCalcRefErrorType.DualTrip) : null;
                return list;
            }
        }
        public void AddTripResult(string name, BoundCalcRefErrorType type,BoundCalcAllTripsResult trc, BoundCalcXlsmReader.TripResult trr) {
            var sp = new BoundCalcRefError(name,type,"Surplus",trc.Capacity,trr.Capacity);
            _allErrors.Add(sp);
            var cap = new BoundCalcRefError(name,type,"Capacity",trc.Capacity,trr.Capacity);
            _allErrors.Add(cap);
            foreach( var ct in trc.Ctrls) {
                if ( trr.SetPointDict.ContainsKey(ct.Code)) {
                    var ctrError = new BoundCalcRefError(name,type,ct.Code,ct.SetPoint,trr.SetPointDict[ct.Code]);
                    _allErrors.Add(ctrError);
                }
            }
        }
    }

    public enum BoundCalcRefErrorType { Node, Branch, Ctrl, SingleTrip, DualTrip }

    public class BoundCalcRefError {  
        public BoundCalcRefError(string objName,BoundCalcRefErrorType objType,string var, double? calc, double? r) {
            ObjectName = objName;
            ObjectType = objType;
            Variable=var;
            Ref = r;
            Calc = calc;
        }
        public string ObjectName {get; set;}
        public BoundCalcRefErrorType ObjectType {get; set;}
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

