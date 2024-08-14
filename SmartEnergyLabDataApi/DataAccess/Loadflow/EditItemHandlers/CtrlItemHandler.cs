using System.Text.RegularExpressions;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Driver;

namespace SmartEnergyLabDataApi.Data;

public class CtrlItemHandler : IEditItemHandler
{
    public string BeforeUndelete(EditItemModel m)
    {
        Ctrl c = (Ctrl) m.Item;
        
        // see if we already have a ctrl pointing at the branch referenced by this ctrl
        var q = m.Da.Session.QueryOver<Ctrl>();
        q = q.Fetch(SelectMode.Fetch,m=>m.Node1);
        q = q.Fetch(SelectMode.Fetch,m=>m.Node2);            
        var di = new DatasetData<Ctrl>(m.Da,m.Dataset.Id,m=>m.Id.ToString(),q);
        //
        var ctrl = di.Data.Where( m=>m.Branch.Id == c.Branch.Id).FirstOrDefault();
        if ( ctrl!=null ) { 
            return $"There is already a ctrl pointing at the same branch so this ctrl cannot be undeleted.\n\n(called <b>{ctrl.DisplayName}</b> in dataset <b>{ctrl.Dataset.Name}</b>)";
        } else {
            return "";
        }
    }

    public string BeforeDelete(EditItemModel m, bool isSourceEdit) {
        return "";
    }


    public void Check(EditItemModel m)
    {
        // Ctrls
        m.CheckDouble("minCtrl",null,0);
        m.CheckDouble("maxCtrl",0);
        // Cost
        m.CheckDouble("cost",0);
        // 
        var bId = m.CheckInt("branchId");
        if (  bId!=null ) {
            var branch = m.Da.Loadflow.GetBranch((int) bId);
            if ( branch==null ) {
                m.AddError("branchId",$"Cannot find branch with id [{bId}]");
            }
        }
    }

    public IId GetItem(EditItemModel model)
    {
        var id = model.ItemId;
        return id>0 ? model.Da.Loadflow.GetCtrl(id) : new Ctrl(model.Dataset, null);
    }

    public void Save(EditItemModel m)
    {
        Ctrl c = (Ctrl) m.Item;
        //
        var branchId = m.CheckInt("branchId");
        if ( c.Id==0 && branchId==null ) {
            m.AddError("branchId","Need to specify an associated branch");
        } else if ( branchId!=null ) {
            var b = m.Da.Loadflow.GetBranch((int) branchId);
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
            c.Type = (LoadflowCtrlType) type;
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
            m.Da.Loadflow.Add(c);
        }
    }

    public List<DatasetData<object>> GetDatasetData(EditItemModel m)
    {
        using( var da = new DataAccess() ) {
            var list = new List<DatasetData<object>>();
            var q = da.Session.QueryOver<Ctrl>().Where( n=>n.Id == m.Item.Id);
            q = q.Fetch(SelectMode.Fetch,m=>m.Branch);
            var di = new DatasetData<Ctrl>(da,m.Dataset.Id,m=>m.Id.ToString(), q);
            list.Add(di.getBaseDatasetData());
            return list;
        }
    }
}