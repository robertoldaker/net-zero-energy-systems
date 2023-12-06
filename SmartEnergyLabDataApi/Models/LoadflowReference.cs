using System.ComponentModel;
using Antlr.Runtime;
using HaloSoft.EventLogger;
using NHibernate.Linq.Clauses;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Loadflow;

namespace SmartEnergyLabDataApi.Models;
public class LoadflowReference {

    private string getBaseFile() {
        string folder = Path.Combine(AppFolders.Instance.Uploads,"Loadflow","Reference");
        Directory.CreateDirectory(folder);
        return Path.Combine(folder,"LoadflowBase.xlsm");
    }
    public void LoadBase(IFormFile file) {
        string dest = getBaseFile();

        using ( var fs = new FileStream(dest,FileMode.Create)) {
            using ( var sr=file.OpenReadStream() ) {
                sr.CopyTo(fs);
            }
        }
    }

    public LoadflowErrors RunBase(double tol) {
        //
        if ( File.Exists(getBaseFile())) {
            var loadflowErrors = new LoadflowErrors(tol);
            var m = new LoadflowXlsmReader();
            m.LoadResults(getBaseFile());
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

    public class LoadflowErrors {
        private double _tol;
        public LoadflowErrors(double tol) {
            _tol = tol;
            NodeErrors = new Dictionary<string,List<RefError>>();
            BranchErrors = new Dictionary<string,List<RefError>>();
            ControlErrors = new Dictionary<string,List<RefError>>();
        }

        public void AddNodeResult(string name, NodeWrapper nw, LoadflowXlsmReader.NodeResult cr) {
            var errors = new List<RefError>();
            var mm = new RefError("Mismatch",nw.Mismatch,cr.Mismatch);
            if ( mm.AbsDiff>_tol) {
                errors.Add(mm);
            }
            if ( errors.Count>0) {
                NodeErrors.Add(name,errors);
            }
        }
        public Dictionary<string,List<RefError>> NodeErrors {get; set;}

        public void AddBranchResult(string name, BranchWrapper bw, LoadflowXlsmReader.BranchResult br) {
            var errors = new List<RefError>();
            var bFlow = new RefError("Power flow",bw.PowerFlow,br.bFlow);
            if ( bFlow.AbsDiff>_tol) {
                errors.Add(bFlow);
            }
            double? fp = bw.FreePower==99999 ? null: bw.FreePower;
            var fPower = new RefError("Free power",fp,br.freePower);
            if ( fPower.AbsDiff>_tol) {
                errors.Add(fPower);
            }
            if ( errors.Count>0) {
                BranchErrors.Add(name,errors);
            }
        }
        public Dictionary<string,List<RefError>> BranchErrors {get; set;}
        
        public void AddCtrlResult(string name, CtrlWrapper cw, LoadflowXlsmReader.CtrlResult cr) {
            var errors = new List<RefError>();
            var sp = new RefError("Set point",cw.SetPoint,cr.SetPoint);
            if ( sp.AbsDiff>_tol) {
                errors.Add(sp);
            }
            if ( errors.Count>0) {
                ControlErrors.Add(name,errors);
            }
        }
        public Dictionary<string,List<RefError>> ControlErrors {get; set;}
    }

    public class RefError {  
        public RefError(string var, double? calc, double? r) {
            Variable=var;
            Ref = r;
            Calc = calc;
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

