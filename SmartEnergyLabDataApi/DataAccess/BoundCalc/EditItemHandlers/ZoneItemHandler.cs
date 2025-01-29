
namespace SmartEnergyLabDataApi.Data.BoundCalc;


public class ZoneItemHandler : BaseEditItemHandler
{
    public override void Check(EditItemModel m)
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

    public override IId GetItem(EditItemModel m)
    {
        var id = m.ItemId;
        return id>0 ? m.Da.Loadflow.GetZone(id) : new BoundCalcZone(m.Dataset);
    }

    public override void Save(EditItemModel m)
    {
        BoundCalcZone zone = (BoundCalcZone) m.Item;
        //
        if ( m.GetString("code",out string code)) {
            zone.Code = code;
        }
        //
        if ( zone.Id==0) {
            m.Da.BoundCalc.Add(zone);
        }
    }

    public override List<DatasetData<object>> GetDatasetData(EditItemModel m)
    {
        using( var da = new DataAccess() ) {
            var list = new List<DatasetData<object>>();  
            var zone = (BoundCalcZone) m.Item;          
            var zoneDi = da.BoundCalc.GetZoneDatasetData(m.Dataset.Id, m=>m.Id == zone.Id);
            list.Add(zoneDi.getBaseDatasetData());
            return list;
        }
    }

}

