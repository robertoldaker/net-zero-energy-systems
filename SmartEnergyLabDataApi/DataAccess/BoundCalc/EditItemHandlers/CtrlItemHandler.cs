using System.Text.RegularExpressions;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Driver;

namespace SmartEnergyLabDataApi.Data.BoundCalc;

public class CtrlItemHandler : BaseEditItemHandler
{
    public override string BeforeUndelete(EditItemModel m)
    {
        BoundCalcCtrl c = (BoundCalcCtrl) m.Item;
        
        // see if we already have a ctrl pointing at the branch referenced by this ctrl
        var q = m.Da.Session.QueryOver<BoundCalcCtrl>();
        q = q.Fetch(SelectMode.Fetch,m=>m.Node1);
        q = q.Fetch(SelectMode.Fetch,m=>m.Node2);            
        var di = new DatasetData<BoundCalcCtrl>(m.Da,m.Dataset.Id,m=>m.Id.ToString(),q);
        //
        var ctrl = di.Data.Where( m=>m.Branch.Id == c.Branch.Id).FirstOrDefault();
        if ( ctrl!=null ) { 
            return $"There is already a ctrl pointing at the same branch so this ctrl cannot be undeleted.\n\n(called <b>{ctrl.DisplayName}</b> in dataset <b>{ctrl.Dataset.Name}</b>)";
        } else {
            return "";
        }
    }

    public override string BeforeDelete(EditItemModel m, bool isSourceEdit) {
        return "";
    }


    public override void Check(EditItemModel m)
    {
        // Ctrls
        m.CheckDouble("minCtrl",null,0);
        m.CheckDouble("maxCtrl",0);
        // Cost
        m.CheckDouble("cost",0);
        // 
        var bId = m.CheckInt("branchId");
        if (  bId!=null ) {
            var branch = m.Da.BoundCalc.GetBranch((int) bId);
            if ( branch==null ) {
                m.AddError("branchId",$"Cannot find branch with id [{bId}]");
            }
        }
    }

    public override IId GetItem(EditItemModel model)
    {
        var id = model.ItemId;
        return id>0 ? model.Da.BoundCalc.GetCtrl(id) : new BoundCalcCtrl(model.Dataset, null);
    }

    public override void Save(EditItemModel m)
    {
        BoundCalcCtrl c = (BoundCalcCtrl) m.Item;
        //
        var branchId = m.CheckInt("branchId");
        if ( c.Id==0 && branchId==null ) {
            m.AddError("branchId","Need to specify an associated branch");
        } else if ( branchId!=null ) {
            var b = m.Da.BoundCalc.GetBranch((int) branchId);
            if ( b!=null ) {
                //
                c.Branch = b;
            } else {
                m.AddError("branchId",$"Cannot find branch with id [{branchId}]");
            }
        }
        // type
        var type = m.CheckInt("type");
        if ( type!=null) {
            c.Type = (BoundCalcCtrlType) type;
        }
        // Min Ctrl
        var minCtrl = m.CheckDouble("minCtrl");
        if ( minCtrl!=null) {
            c.MinCtrl = (double) minCtrl;
        }
        // Max Ctrl
        var maxCtrl = m.CheckDouble("maxCtrl");
        if ( maxCtrl!=null) {
            c.MaxCtrl = (double) maxCtrl;
        }
        // Cost
        var cost = m.CheckDouble("cost");
        if ( cost!=null) {
            c.Cost = (double) cost;
        }
        //
        if ( c.Id==0) {
            m.Da.BoundCalc.Add(c);
        }
    }

    public override List<DatasetData<object>> GetDatasetData(EditItemModel m)
    {
        using( var da = new DataAccess() ) {
            var list = new List<DatasetData<object>>();
            var ctrl = (BoundCalcCtrl) m.Item;
            var ctrlDi = da.BoundCalc.GetCtrlDatasetData(m.Dataset.Id,m=>m.Id == ctrl.Id);
            list.Add(ctrlDi.getBaseDatasetData());
            return list;
        }
    }
}