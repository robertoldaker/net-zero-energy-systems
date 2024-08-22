using System.Text.RegularExpressions;
using NHibernate;
using NHibernate.Driver;

namespace SmartEnergyLabDataApi.Data;

public class BranchItemHandler : IEditItemHandler
{
    public string BeforeUndelete(EditItemModel m)
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
        var ctrls = m.Da.Loadflow.GetCtrlsForBranch(b,m.Dataset);
        if ( ctrls.Count==1) {
            ue = m.Da.Datasets.GetDeleteUserEdit(m.Dataset.Id, ctrls[0]);
            if ( ue!=null ) {
                m.Da.Datasets.Delete(ue);
            }
        }
        //
        return "";
    }

    public string BeforeDelete(EditItemModel m, bool isSourceEdit) {
        Branch b = (Branch) m.Item;
        // Also mark as deleted any ctrls pointing at this branch
        var ctrls = m.Da.Loadflow.GetCtrlsForBranch(b,m.Dataset);
        foreach( var c in ctrls) {
            m.Da.Datasets.AddDeleteUserEdit(c,m.Dataset);
        }
        //
        return "";
    }

    public void Check(EditItemModel m)
    {
        //?? prevents code from being null or empty
        /*
        if ( m.GetString("code",out string code)) {            
            if ( string.IsNullOrEmpty(code)) {
                m.AddError("code",$"Code must be set");
            }
            else if ( m.Da.Loadflow.BranchExists(m.Dataset.Id,code, out Dataset ds) ) {
                m.AddError("code",$"Branch already exists in dataset [{ds.Name}]");
            }
            
        } 
        else if ( m.ItemId == 0 ) {
            m.AddError("code","Code must be set");
        }*/
        //?? allows code to be null or empty
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
        // Check another one doesn't already exist
        if ( nodeId1!=null && nodeId2!=null) {
            if ( m.Da.Loadflow.BranchExists(m.Dataset.Id,code, (int) nodeId1, (int) nodeId2,out Dataset ds) ) {
                m.AddError("code",$"Branch with same code and end-points already exists in dataset [{ds.Name}]");
            }            
        }
    }

    public IId GetItem(EditItemModel model)
    {
        var id = model.ItemId;
        return id>0 ? model.Da.Loadflow.GetBranch(id) : new Branch(model.Dataset);
    }

    public void Save(EditItemModel m)
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
        if ( b.Id==0) {
            m.Da.Loadflow.Add(b);
        }
    }

    public List<DatasetData<object>> GetDatasetData(EditItemModel m)
    {
        using( var da = new DataAccess() ) {
            var list = new List<DatasetData<object>>();
            var branch = (Branch) m.Item;
            var branchDi = da.Loadflow.GetBranchDatasetData(m.Dataset.Id, n=>n.Id == branch.Id, out var nodeDi, out var locDi );
            list.Add(branchDi.getBaseDatasetData());
            list.Add(nodeDi.getBaseDatasetData());
            list.Add(locDi.getBaseDatasetData());
            return list;
        }
    }

}