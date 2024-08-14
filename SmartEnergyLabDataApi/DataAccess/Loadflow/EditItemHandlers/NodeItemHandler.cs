using System.Text.RegularExpressions;
using NHibernate;
using NHibernate.AdoNet.Util;

namespace SmartEnergyLabDataApi.Data;

public class NodeItemHandler : IEditItemHandler
{
    public string BeforeUndelete(EditItemModel m)
    {
        return "";
    }

    public string BeforeDelete(EditItemModel m, bool isSourceEdit) {
        return "";
    }

    public void Check(EditItemModel m)
    {
        if ( m.GetString("code",out string code)) {
            Regex regex = new Regex(@"^[A-Z]{4}\d");
            var codeMatch = regex.Match(code);        
            if ( !codeMatch.Success) {
                m.AddError("code","Code must be in form <uppercase-4-letter-code><voltage id><anything>");
            } 
            if ( m.Da.Loadflow.NodeExists(m.Dataset.Id,code, out Dataset ds) ) {
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

    public IId GetItem(EditItemModel model)
    {
        var id = model.ItemId;
        return id>0 ? model.Da.Loadflow.GetNode(id) : new Node(model.Dataset);
    }

    public void Save(EditItemModel m)
    {
        Node node = (Node) m.Item;
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
            var zone = m.Da.Loadflow.GetZone((int) zoneId);
            node.Zone = zone;
        } 

        //
        if ( node.Id==0) {
            m.Da.Loadflow.Add(node);
        }
    }

    public List<DatasetData<object>> GetDatasetData(EditItemModel m)
    {
        using( var da = new DataAccess() ) {
            var list = new List<DatasetData<object>>();
            var q = da.Session.QueryOver<Node>().Where( n=>n.Id == m.Item.Id);
                q = q.Fetch(SelectMode.Fetch,m=>m.Zone);
            var di = new DatasetData<Node>(da,m.Dataset.Id,m=>m.Id.ToString(), q);
            list.Add(di.getBaseDatasetData());
            Node node = (Node) m.Item;
            if ( node.Location!=null ) {
                var qq = da.Session.QueryOver<GridSubstationLocation>().Where( n=>n.Id == node.Location.Id);
                var dii = new DatasetData<GridSubstationLocation>(da,m.Dataset.Id,m=>m.Id.ToString(), qq);
                // add ref. to location
                di.Data[0].Location = dii.Data[0];
                list.Add(dii.getBaseDatasetData());            
            }
            return list;
        }
    }
}