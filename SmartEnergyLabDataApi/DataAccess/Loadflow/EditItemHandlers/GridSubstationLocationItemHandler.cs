
using System.Text.RegularExpressions;
using NHibernate;
using NHibernate.Criterion;

namespace SmartEnergyLabDataApi.Data;

public class GridSubstationLocationItemHandler : IEditItemHandler
{
    public string BeforeDelete(EditItemModel m, bool isSourceEdit)
    {
        return "";
    }

    public string BeforeUndelete(EditItemModel m)
    {
        return "";
    }

    public void Check(EditItemModel m)
    {
        Regex regex = new Regex("^[A-Z]{4}X?$");
        // reference
        if ( m.GetString("code", out string reference)) {
            if ( string.IsNullOrEmpty(reference)) {
                m.AddError("code","Please enter a 4-letter uppercase code with optional trailing X");                
            } else {
                if ( !regex.IsMatch(reference) ) {
                    m.AddError("code","Please enter a 4-letter uppercase code with optional trailing X");
                } else {
                    if ( m.Da.NationalGrid.GridSubstationLocationExists(m.Dataset.Id,reference, out Dataset ds) ) {
                        m.AddError("code",$"Location already exists in dataset [{ds.Name}]");
                    }
                }
            }
        }
        // name
        if ( m.GetString("name", out string name)) {
            if ( string.IsNullOrEmpty(name)) {
                m.AddError("name","Please enter a name");                
            } 
        }
        // Ctrls
        m.CheckDouble("latitude");
        m.CheckDouble("longitude");
    }

    public IId GetItem(EditItemModel m)
    {
        var id = m.ItemId;
        return id>0 ? m.Da.NationalGrid.GetGridSubstationLocation(id) : new GridSubstationLocation() { Dataset = m.Dataset};
    }

    public void Save(EditItemModel m)
    {
        GridSubstationLocation loc = (GridSubstationLocation) m.Item;
        //
        if ( loc.Id == 0 ) {
            loc.Dataset = m.Dataset;
            loc.Source = GridSubstationLocationSource.UserDefined;
            loc.GISData = new GISData();
        }
        if ( m.GetString("code",out string code) ) {
            loc.Reference = code;
        }
        if ( m.GetString("name",out string name) ) {
            loc.Name = name;
        }
        var lat = m.CheckDouble("latitude");
        if ( lat!=null) {
            loc.GISData.Latitude = (double) lat;
        }
        var lng = m.CheckDouble("longitude");
        if ( lng!=null) {
            loc.GISData.Longitude = (double) lng;
        }
        if ( loc.Id==0) {
            m.Da.NationalGrid.Add(loc);
        }
    }

    public List<DatasetData<object>> GetDatasetData(EditItemModel m)
    {
        using( var da = new DataAccess() ) {
            var list = new List<DatasetData<object>>();
            // location
            GridSubstationLocation loc = (GridSubstationLocation) m.Item;
            var locDi = da.NationalGrid.GetLocationDatasetData(m.Dataset.Id, n=>n.Id == loc.Id);
            list.Add(locDi.getBaseDatasetData());
            // nodes that use this location
            var nodeDi = da.Loadflow.GetNodeDatasetData(m.Dataset.Id,m=>m.Location.Id == loc.Id);
            list.Add(nodeDi.getBaseDatasetData());
            // branches that use these nodes
            var nodeIds=nodeDi.Data.Select(m=>m.Id).ToArray();
            var branchDi = da.Loadflow.GetBranchDatasetData(m.Dataset.Id, n=>n.Node1.Id.IsIn(nodeIds) || n.Node2.Id.IsIn(nodeIds));
            list.Add(branchDi.getBaseDatasetData());
            //
            return list;
        }        
    }
}