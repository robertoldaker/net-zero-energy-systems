using System.Text.RegularExpressions;
using Microsoft.Extensions.ObjectPool;
using NHibernate;
using NHibernate.Driver;

namespace SmartEnergyLabDataApi.Data;

public class BranchItemHandler : BaseEditItemHandler
{
    public override string BeforeUndelete(EditItemModel m)
    {
        Branch b = (Branch) m.Item;
        int node1Id = b.Node1.Id;
        // ensure node1 is not deleted
        var ue = m.Da.Datasets.GetDeleteUserEdit(m.Dataset.Id, b.Node1);
        if ( ue!=null ) {
            m.Da.Datasets.Delete(ue);
        }
        // ensure node2 is not deleted
        ue = m.Da.Datasets.GetDeleteUserEdit(m.Dataset.Id, b.Node2);
        if ( ue!=null ) {
            m.Da.Datasets.Delete(ue);
        }
        // undelete a ctrl pointing at this branch (assuming one only)
        if ( b.Ctrl!=null) {
            ue = m.Da.Datasets.GetDeleteUserEdit(m.Dataset.Id, b.Ctrl);
            if ( ue!=null ) {
                m.Da.Datasets.Delete(ue);
            }
        }
        //
        return "";
    }

    public override string BeforeDelete(EditItemModel m, bool isSourceEdit) {
        // Also mark as deleted any ctrls pointing at this branch
        if ( isSourceEdit ) {
            // Ctrl should get deleted automatically
        } else {
            Branch b = (Branch) m.Item;
            if ( b.Ctrl!=null ) {
                m.Da.Datasets.AddDeleteUserEdit(b.Ctrl,m.Dataset);
            }
        }
        //
        return "";
    }

    public override void Check(EditItemModel m)
    {

        m.GetString("code",out string code);
        
        // demand        
        m.CheckDouble("x",0);
        // generation
        m.CheckDouble("cap",0);
        // node 1 id
        var nodeId1 = m.CheckInt("nodeId1");
        if ( nodeId1==null && m.ItemId == 0) {
            m.AddError("nodeId1","Node 1 must be set");
        }
        // node 2 id
        var nodeId2 = m.CheckInt("nodeId2");
        if ( nodeId2==null && m.ItemId == 0) {
            m.AddError("nodeId2","Node 2 must be set");
        }
        if ( nodeId1!=null && nodeId1==nodeId2 ) {
            m.AddError("nodeId1","Nodes must be different");
            m.AddError("nodeId2","Nodes must be different");
        }
        var type = m.CheckInt("type");
        if ( type!=null ) {
            var branchType = (BranchType) type;            
            if ( branchType == BranchType.QB || branchType == BranchType.HVDC ) {
                var minCtrl = m.CheckDouble("minCtrl");
                if ( minCtrl == null ) {
                    m.AddError("minCtrl","Please set min ctrl");
                }
                var maxCtrl = m.CheckDouble("maxCtrl");
                if ( maxCtrl == null ) {
                    m.AddError("maxCtrl","Please set max ctrl");
                }
                var cost = m.CheckDouble("cost");
                if ( cost == null ) {
                    m.AddError("cost","Please set cost");
                }
            }
        } else if ( m.ItemId == 0 ) {
            m.AddError("type","Please set a type for this branch");
        }
        // Check another one doesn't already exist
        if ( nodeId1!=null && nodeId2!=null) {
            if ( m.Da.Loadflow.BranchExists(m.Dataset.Id,code, (int) nodeId1, (int) nodeId2,out Dataset ds) ) {
                m.AddError("code",$"Branch with same code and end-points already exists in dataset [{ds.Name}]");
            }            
        }
    }

    public override IId GetItem(EditItemModel model)
    {
        var id = model.ItemId;
        var branch = id>0 ? model.Da.Loadflow.GetBranch(id) : new Branch(model.Dataset);
        if ( branch==null ) {
            throw new Exception($"Cannot find branch with is=[{id}]");
        }
        return branch;
    }

    public override void Save(EditItemModel m)
    {
        Branch b = (Branch) m.Item;
        //
        if ( m.GetString("code",out string code)) {
            b.Code = code;
        }
        // reactance
        var x = m.CheckDouble("x",0);
        if ( x!=null ) {
            b.X = (double) x;
        }
        // capacity
        var cap = m.CheckDouble("cap",0);
        if ( cap!=null ) {
            b.Cap = (double) cap;            
        }
        // node id 1
        var nodeId1 = m.CheckInt("nodeId1");
        if ( nodeId1!=null ) {
            var node = m.Da.Loadflow.GetNode((int) nodeId1);
            b.Node1 = node;
        } 

        // node id 2
        var nodeId2 = m.CheckInt("nodeId2");
        if ( nodeId2!=null ) {
            var node = m.Da.Loadflow.GetNode((int) nodeId2);
            b.Node2 = node;
        } 
        //
        var type = m.CheckInt("type");
        if ( type!=null && b.Id ==0 ) {
            var branchType = (BranchType) type;
            b.Type = branchType;
            if ( branchType == BranchType.QB || branchType == BranchType.HVDC ) {
                LoadflowCtrlType ctrlType;
                if ( branchType == BranchType.QB) {
                    ctrlType = LoadflowCtrlType.QB;
                } else if ( branchType == BranchType.HVDC) {
                    ctrlType = LoadflowCtrlType.HVDC;
                } else {
                    throw new Exception($"Unexpected branch type found [{branchType}]");
                }
                var ctrl = new Ctrl(m.Dataset,b);
                ctrl.Type = ctrlType; // note needs to be done before SetCtrl
                b.SetCtrl(ctrl);                
                b.Ctrl.Type = ctrlType; // also need to reset this as SetCtrl changes branch type
            }
        }
        //
        if ( b.Ctrl!=null ) {
            var minCtrl = m.CheckDouble("minCtrl");
            if ( minCtrl!=null ) {
                b.Ctrl.MinCtrl = (double) minCtrl;
            }
            var maxCtrl = m.CheckDouble("maxCtrl");
            if ( maxCtrl!=null ) {
                b.Ctrl.MaxCtrl = (double) maxCtrl;
            }
            var cost = m.CheckDouble("cost");
            if ( cost!=null ) {
                b.Ctrl.Cost = (double) cost;
            }
        }
        //
        if ( b.Id==0) {
            m.Da.Loadflow.Add(b);
        }
    }

    public override void UpdateUserEdits(EditItemModel m) {
        // this save the branch
        base.UpdateUserEdits(m);
        // Also any changes to the control
        Branch b = (Branch) m.Item;
        if ( b.Ctrl!=null ) {
            m.UpdateUserEditForItem(b.Ctrl);
        }
    }

    public override List<DatasetData<object>> GetDatasetData(EditItemModel m)
    {
        using( var da = new DataAccess() ) {
            var list = new List<DatasetData<object>>();
            var branch = (Branch) m.Item;
            var branchDi = da.Loadflow.GetBranchDatasetData(m.Dataset.Id, n=>n.Id == branch.Id, out var ctrlDi, out var nodeDi, out var locDi );
            list.Add(branchDi.getBaseDatasetData());
            list.Add(ctrlDi.getBaseDatasetData());
            list.Add(nodeDi.getBaseDatasetData());
            list.Add(locDi.getBaseDatasetData());
            return list;
        }
    }

}