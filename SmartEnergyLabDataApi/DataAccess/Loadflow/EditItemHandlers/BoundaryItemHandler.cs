namespace SmartEnergyLabDataApi.Data;

public class BoundaryItemHandler : IEditItemHandler
{
    public string BeforeUndelete(EditItemModel m)
    {     
        return "";   
    }

    public string BeforeDelete(EditItemModel m, bool isSourceEdit)
    {        
        return "";
    }

    public void Check(EditItemModel m)
    {
        if ( !m.GetString("code",out string code) && m.ItemId==0) {
            m.AddError("code","Code needs to be set");
        }

        var zoneIds = m.GetIntArray("zoneIds");
        if ( zoneIds==null && m.ItemId==0) {
            m.AddError("zoneIds","Some zones need defining");
        }
    }

    public IId GetItem(EditItemModel m)
    {
        var id = m.ItemId;
        return id>0 ? m.Da.Loadflow.GetBoundary(id) : new Boundary(m.Dataset);
    }

    public void Save(EditItemModel m)
    {
        Boundary b = (Boundary) m.Item;
        //
        if ( m.GetString("code",out string code)) {
            b.Code = code;
        }         
        if ( b.Id == 0) {
            m.Da.Loadflow.Add(b);
        }
        //
        var zoneIds = m.GetIntArray("zoneIds");
        if ( zoneIds!=null ) {
            // and boundary zones
            var existingBoundaryZones = m.Da.Loadflow.GetBoundaryZones(b.Id);
            var newBoundaryZones = new List<BoundaryZone>();
            // Add new ones
            foreach( var zoneId in zoneIds) {
                var bz = existingBoundaryZones.Where(m=>m.Zone.Id == zoneId).FirstOrDefault();
                if ( bz==null) {
                    var zone = m.Da.Loadflow.GetZone(zoneId);
                    if ( zone!=null ) {
                        var nbz = new BoundaryZone() {
                            Boundary = b,
                            Zone = zone,
                            Dataset = m.Dataset
                        };
                        m.Da.Loadflow.Add(nbz);
                    }
                }
            }
            // Delete ones not defined anymore
            foreach( var bz in existingBoundaryZones) {
                if ( !zoneIds.Contains(bz.Zone.Id)) {
                    m.Da.Loadflow.Delete(bz);
                }
            }
        }
    }

    public List<DatasetData<object>> GetDatasetData(EditItemModel m)
    {
        using( var da = new DataAccess() ) {
            var list = new List<DatasetData<object>>();
            var boundary = (Boundary) m.Item;
            var boundDi = da.Loadflow.GetBoundaryDatasetData(m.Dataset.Id, m=>m.Id == boundary.Id);
            list.Add(boundDi.getBaseDatasetData());
            return list;
        }
    }
}