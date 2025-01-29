
using System.Text.RegularExpressions;
using Microsoft.Extensions.ObjectPool;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Util;

namespace SmartEnergyLabDataApi.Data.Loadflow;

public class GridSubstationLocationItemHandler : BaseEditItemHandler
{

    public override string BeforeDelete(EditItemModel m, bool isSourceEdit)
    {
        int numNodes = m.Da.Loadflow.GetNodeCountForLocation(m.ItemId, isSourceEdit);
        if ( numNodes > 0 ) {
            return $"Cannot delete location as used by <b>{numNodes}</b> nodes";
        }
        return "";
    }

    public override void Check(EditItemModel m)
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

    public override IId GetItem(EditItemModel m)
    {
        var id = m.ItemId;
        return id>0 ? m.Da.NationalGrid.GetGridSubstationLocation(id) : new GridSubstationLocation() { Dataset = m.Dataset};
    }

    public override void Save(EditItemModel m)
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

    public override List<DatasetData<object>> GetDatasetData(EditItemModel m)
    {
        var list = new List<DatasetData<object>>();        
        // location
        GridSubstationLocation loc = (GridSubstationLocation) m.Item;
        using ( var da = new DataAccess()) {
            var nodeIds = da.Session.QueryOver<Node>().Where( m=>m.Location.Id == loc.Id).Select(m=>m.Id).List<int>().ToArray();
            var branchIds = da.Session.QueryOver<Branch>().Where( m=>m.Node1.Id.IsIn(nodeIds) || m.Node2.Id.IsIn(nodeIds) ).Select(m=>m.Id).List<int>().ToArray();
            if ( nodeIds.Length == 0 ) {
                // not being used so just return the dataset for this location
                var locDi = da.NationalGrid.GetLocationDatasetData(m.Dataset.Id, m=>m.Id == loc.Id);
                list.Add(locDi.getBaseDatasetData());
            } else if ( branchIds.Length==0) {
                // 
                var nodeDi = da.Loadflow.GetNodeDatasetData(m.Dataset.Id, m=>m.Location.Id == loc.Id, out var locDi);
                list.Add(nodeDi.getBaseDatasetData());
                list.Add(locDi.getBaseDatasetData());
            } else {
                // load branches that used the nodes used by the location
                var branchDi = da.Loadflow.GetBranchDatasetData(m.Dataset.Id, n=>n.Node1.Id.IsIn(nodeIds) || n.Node2.Id.IsIn(nodeIds), out var ctrlDi, out var nodeDi, out var locDi);
                // add the datasets returned to the list
                list.Add(branchDi.getBaseDatasetData()); 
                list.Add(ctrlDi.getBaseDatasetData());
                list.Add(nodeDi.getBaseDatasetData());
                list.Add(locDi.getBaseDatasetData());
            }

        }
        //
        return list;
    }
}