using System.Text.RegularExpressions;

namespace SmartEnergyLabDataApi.Data;

public class BranchItemHandler : IEditItemHandler
{
    public void BeforeUndelete(EditItemModel m)
    {
        Branch b = (Branch) m.Item;
        int node1Id = b.Node1.Id;
        // ensure node1 is not deleted
        var ue = m.Da.Datasets.GetDeleteUserEdit(m.Dataset.Id, "Node", b.Node1.Id.ToString());
        if ( ue!=null ) {
            m.Da.Datasets.Delete(ue);
        }
        // ensure node2 is not deleted
        ue = m.Da.Datasets.GetDeleteUserEdit(m.Dataset.Id, "Node", b.Node2.Id.ToString());
        if ( ue!=null ) {
            m.Da.Datasets.Delete(ue);
        }
    }

    public void Check(EditItemModel m)
    {
        if ( m.GetString("code",out string code)) {
            if ( string.IsNullOrEmpty(code)) {
                m.AddError("code",$"Code must be set");
            }
            else if ( m.Da.Loadflow.BranchExists(m.Dataset.Id,code, out Dataset ds) ) {
                m.AddError("code",$"Branch already exists in dataset [{ds.Name}]");
            }
        } else if ( m.ItemId == 0 ) {
            m.AddError("code","Code must be set");
        }
        // demand        
        m.CheckDouble("x",0);
        // generation
        m.CheckDouble("cap",0);
        // node 1 id
        if ( m.CheckInt("nodeId1")==null && m.ItemId == 0) {
            m.AddError("nodeId1","Node 1 must be set");
        }
        // node 2 id
        if ( m.CheckInt("nodeId2")==null && m.ItemId == 0) {
            m.AddError("nodeId2","Node 2 must be set");
        }
    }

    public object GetItem(EditItemModel model)
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
}