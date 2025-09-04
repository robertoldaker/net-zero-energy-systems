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

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<BoundCalcRefError> NodeErrors {
            get {
                var list = _showAllErrors ? filterAllErrors(BoundCalcRefErrorType.Node) : null;
                return list;
            }
        }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<BoundCalcRefError> BranchErrors {
            get {
                var list = _showAllErrors ? filterAllErrors(BoundCalcRefErrorType.Branch) : null;
                return list;
            }
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

