using System.Text.RegularExpressions;
using Microsoft.Extensions.ObjectPool;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Driver;

namespace SmartEnergyLabDataApi.Data.BoundCalc;

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
        if (isSourceEdit) {
            //
            Branch b = (Branch)m.Item;
            if (b.Ctrl != null) {
                m.DeletedItems.Add(new DeletedItem(b.Ctrl) { isSourceDelete = true });
            }
        } else {
            Branch b = (Branch)m.Item;
            if (b.Ctrl != null) {
                m.Da.Datasets.AddDeleteUserEdit(b.Ctrl, m.Dataset);
            }
        }
        //
        return "";
    }

    public override void Check(EditItemModel m)
    {
        Branch b = (Branch)m.Item;

        m.GetString("code",out string code);
        if ( code!=null ) {
            if ( string.IsNullOrWhiteSpace(code)) {
                // No nothing as later on it will get auto-generated in Save
            } else if ( m.Item!=null && ((Branch)m.Item).Code != code ) {
                var regex = new Regex(@"B_\d+");
                if ( regex.IsMatch(code)) {
                    m.AddError("code","Branch code must not be of the form B_<digit>");
                } else {
                    var branch = m.Da.BoundCalc.GetBranch(m.Dataset.Id, code);
                    if ( branch!=null &&  branch.Id != m.ItemId ) {
                        m.AddError("code","Branch code must be unique or empty");
                    }
                }
            }
        }
        // demand
        m.CheckDouble("x",0);
        // generation
        m.CheckDouble("cap",0);
        // node 1 id
        var nodeId1 = getNode1(m);
        if ( nodeId1==null) {
            m.AddError("nodeId1","Node 1 must be set");
        }
        // node 2 id
        var nodeId2 = getNode2(m);
        if ( nodeId2==null) {
            m.AddError("nodeId2","Node 2 must be set");
        }
        if ( nodeId1!=null && nodeId1==nodeId2 ) {
            m.AddError("nodeId1","Nodes must be different");
            m.AddError("nodeId2","Nodes must be different");
        }
        var type = m.CheckInt("type");
        if ( type!=null ) {
            var branchType = (BoundCalcBranchType) type;
            if ( branchType == BoundCalcBranchType.QB || branchType == BoundCalcBranchType.HVDC ) {
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
            if ( m.Da.BoundCalc.BranchExists(m.Dataset.Id,code, (int) nodeId1, (int) nodeId2,out Dataset ds) ) {
                m.AddError("code",$"Branch with same code and end-points already exists in dataset [{ds.Name}]");
            }
        }
    }

    private int? getNode1(EditItemModel m)
    {
        var nodeId = m.CheckInt("nodeId1");
        if (nodeId == null && m.ItemId != 0) {
            nodeId = ((Branch)m.Item).Node1.Id;
        }
        return nodeId;
    }

    private int? getNode2(EditItemModel m)
    {
        var nodeId = m.CheckInt("nodeId2");
        if (nodeId == null && m.ItemId != 0) {
            nodeId = ((Branch)m.Item).Node2.Id;
        }
        return nodeId;
    }

    public override IDatasetIId GetItem(EditItemModel model)
    {
        var id = model.ItemId;
        var branch = id > 0 ? model.Da.BoundCalc.GetBranch(id) : new Branch(model.Dataset);
        if (branch == null) {
            throw new Exception($"Cannot find branch with id=[{id}]");
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
            var node = m.Da.BoundCalc.GetNode((int) nodeId1);
            b.Node1 = node;
        }

        // node id 2
        var nodeId2 = m.CheckInt("nodeId2");
        if ( nodeId2!=null ) {
            var node = m.Da.BoundCalc.GetNode((int) nodeId2);
            b.Node2 = node;
        }
        //
        var type = m.CheckInt("type");
        if ( type!=null ) {
            var branchType = (BoundCalcBranchType) type;
            b.Type = branchType;
            if (branchType == BoundCalcBranchType.QB || branchType == BoundCalcBranchType.HVDC) {
                // branch types with controls
                BoundCalcCtrlType ctrlType;
                if (branchType == BoundCalcBranchType.QB) {
                    ctrlType = BoundCalcCtrlType.QB;
                } else if (branchType == BoundCalcBranchType.HVDC) {
                    ctrlType = BoundCalcCtrlType.HVDC;
                } else {
                    throw new Exception($"Unexpected branch type found [{branchType}]");
                }
                Ctrl ctrl = b.Ctrl;
                if (ctrl == null) {
                    ctrl = new Ctrl(m.Dataset, b);
                    b.Ctrl = ctrl;
                }
                ctrl.Type = ctrlType;
            } else {
                // branch types no controls
                if (b.Ctrl != null) {
                    // this will ensure it gets deleted from the dataset in the gui
                    m.DeletedItems.Add(new DeletedItem(b.Ctrl) { isSourceDelete = true });
                }
                // This will mean its deleted from the db (cascade = "all-delete-orphan")
                if (b.Ctrl != null) {
                    m.Da.BoundCalc.Delete(b.Ctrl);
                    b.Ctrl = null;
                }
            }
        }
        //
        var ohl = m.CheckDouble("ohl");
        if (ohl != null) {
            b.OHL = (double) ohl;
        }
        //
        var cableLength = m.CheckDouble("cableLength");
        if (cableLength != null) {
            b.CableLength = (double)cableLength;
        }
        //
        if (b.Ctrl != null) {
            var minCtrl = m.CheckDouble("minCtrl");
            if (minCtrl != null) {
                b.Ctrl.MinCtrl = (double)minCtrl;
            }
            var maxCtrl = m.CheckDouble("maxCtrl");
            if (maxCtrl != null) {
                b.Ctrl.MaxCtrl = (double)maxCtrl;
            }
            var cost = m.CheckDouble("cost");
            if (cost != null) {
                b.Ctrl.Cost = (double)cost;
            }
        }
        //
        if ( b.Id==0) {
            m.Da.BoundCalc.Add(b);
        }
        // set a unique code if non-set
        if ( b.Id!=0 && string.IsNullOrWhiteSpace(b.Code) ) {
            b.Code = $"B_{b.Id}";
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
            //
            var (branchDi,ctrlDi) = da.BoundCalc.GetBranchDatasetData(m.Dataset.Id, n=>n.Id == branch.Id, true);

            list.Add(branchDi.getBaseDatasetData());
            list.Add(ctrlDi.getBaseDatasetData());
            return list;
        }
    }

}
