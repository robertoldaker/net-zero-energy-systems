using System.Text.RegularExpressions;
using NHibernate;
using NHibernate.AdoNet.Util;

namespace SmartEnergyLabDataApi.Data.BoundCalc;

public class NodeItemHandler : BaseEditItemHandler
{
    public override string BeforeUndelete(EditItemModel m)
    {
        // undelete any location that the node is pointing to
        var node = (BoundCalcNode) m.Item;
        if ( node.Location!=null) {
            var ue = m.Da.Datasets.GetDeleteUserEdit(m.Dataset.Id, node.Location);
            if ( ue!=null ) {
                m.Da.Datasets.Delete(ue);
            }
        }
        return "";
    }

    public override string BeforeDelete(EditItemModel m, bool isSourceEdit) {
        var numBranches = m.Da.BoundCalc.GetBranchCountForNode(m.ItemId, isSourceEdit);
        if ( numBranches > 0 ) {
            return $"Cannot delete node as used by <b>{numBranches}</b> branches";
        }
        return "";
    }

    public override void Check(EditItemModel m)
    {
        if ( m.GetString("code",out string code)) {
            Regex regex = new Regex(@"^[A-Z]{4}\d");
            var codeMatch = regex.Match(code);        
            if ( !codeMatch.Success) {
                m.AddError("code","Code must be in form <uppercase-4-letter-code><voltage id><anything>");
            } 
            if ( m.Da.BoundCalc.NodeExists(m.Dataset.Id,code, out Dataset ds) ) {
                m.AddError("code",$"Node already exists in dataset [{ds.Name}]");
            }
        } else if ( m.ItemId == 0 ) {
            m.AddError("code","Code must be set");
        }
        // demand        
        m.CheckDouble("demand");
        // generation
        m.CheckDouble("generation");
        // external
        m.CheckBoolean("ext");
        // zone id
        if ( m.CheckInt("zoneId")==null && m.ItemId == 0) {
            m.AddError("zoneId","Zone must be set");
        }
    }

    public override IId GetItem(EditItemModel model)
    {
        var id = model.ItemId;
        return id>0 ? model.Da.BoundCalc.GetNode(id) : new BoundCalcNode(model.Dataset);
    }

    public override void Save(EditItemModel m)
    {
        BoundCalcNode node = (BoundCalcNode) m.Item;
        //
        if ( m.GetString("code",out string code)) {
            node.Code = code;
            node.SetVoltage();
            node.SetLocation(m.Da);
        }
        // demand        
        var demand = m.CheckDouble("demand",0);
        if ( demand!=null ) {
            node.Demand = (double) demand;
        }
        // generation
        var generation = m.CheckDouble("generation",0);
        if ( generation!=null ) {
            node.Generation = (double) generation;            
        }
        // external
        var ext = m.CheckBoolean("ext");
        if ( ext!=null) {
            node.Ext = (bool) ext;
        }        
        // zone id
        var zoneId = m.CheckInt("zoneId");
        if ( zoneId!=null ) {
            var zone = m.Da.BoundCalc.GetZone((int) zoneId);
            node.Zone = zone;
        } 

        //
        if ( node.Id==0) {
            m.Da.BoundCalc.Add(node);
        }
    }

    public override List<DatasetData<object>> GetDatasetData(EditItemModel m)
    {
        using( var da = new DataAccess() ) {
            var list = new List<DatasetData<object>>();
            var node = (BoundCalcNode) m.Item;
            var nodeDi = da.BoundCalc.GetNodeDatasetData(m.Dataset.Id,m=>m.Id == node.Id, out var locDi);
            list.Add(nodeDi.getBaseDatasetData());
            list.Add(locDi.getBaseDatasetData()); 
            return list;
        }
    }
}