
namespace SmartEnergyLabDataApi.Data;


public class ZoneItemHandler : IEditItemHandler
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
        // code
        if ( m.GetString("code",out string code)) {
            if ( m.Da.Loadflow.ZoneExists(m.Dataset.Id,code, out Dataset? dataset) ) {
                m.AddError("code",$"Zone already exists in dataset [{dataset?.Name}]");
            }
        } else if ( m.ItemId==0) {
            m.AddError("code","Code must be set to something");
        }
    }

    public IId GetItem(EditItemModel m)
    {
        var id = m.ItemId;
        return id>0 ? m.Da.Loadflow.GetZone(id) : new Zone(m.Dataset);
    }

    public void Save(EditItemModel m)
    {
        Zone zone = (Zone) m.Item;
        //
        if ( m.GetString("code",out string code)) {
            zone.Code = code;
        }
        //
        if ( zone.Id==0) {
            m.Da.Loadflow.Add(zone);
        }
    }

    public List<DatasetData<object>> GetDatasetData(EditItemModel m)
    {
        using( var da = new DataAccess() ) {
            var list = new List<DatasetData<object>>();  
            var zone = (Zone) m.Item;          
            var zoneDi = da.Loadflow.GetZoneDatasetData(m.Dataset.Id, m=>m.Id == zone.Id);
            list.Add(zoneDi.getBaseDatasetData());
            return list;
        }
    }

}

