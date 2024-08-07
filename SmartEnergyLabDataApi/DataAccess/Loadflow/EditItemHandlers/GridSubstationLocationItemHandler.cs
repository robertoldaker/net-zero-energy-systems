
using System.Text.RegularExpressions;

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
        Regex regex = new Regex("[A-Z]{4}");
        // reference
        if ( m.GetString("code", out string reference)) {
            if ( string.IsNullOrEmpty(reference)) {
                m.AddError("code","Please enter a 4-letter uppercase code");                
            } else {
                if ( !regex.IsMatch(reference) ) {
                    m.AddError("code","Please enter a 4-letter uppercase code");                
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

    public object GetItem(EditItemModel m)
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
}