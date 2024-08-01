using System.Text.RegularExpressions;
using NHibernate.Criterion;
using NHibernate.Driver;

namespace SmartEnergyLabDataApi.Data;

public class CtrlItemHandler : IEditItemHandler
{
    public void BeforeUndelete(EditItemModel m)
    {
        Ctrl c = (Ctrl) m.Item;
        //?? Need to undelete Branch as well??
        int node1Id = c.Node1.Id;
        // ensure node1 is not deleted
        var ue = m.Da.Datasets.GetDeleteUserEdit(m.Dataset.Id, "Node", c.Node1.Id.ToString());
        if ( ue!=null ) {
            m.Da.Datasets.Delete(ue);
        }
        // ensure node2 is not deleted
        ue = m.Da.Datasets.GetDeleteUserEdit(m.Dataset.Id, "Node", c.Node2.Id.ToString());
        if ( ue!=null ) {
            m.Da.Datasets.Delete(ue);
        }
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

    public object GetItem(EditItemModel model)
    {
        var id = model.ItemId;
        return id>0 ? model.Da.Loadflow.GetCtrl(id) : new Ctrl(model.Dataset);
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
                c.Code = b.Code;
                c.Node1 = b.Node1;
                c.Node2 = b.Node2;
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
}