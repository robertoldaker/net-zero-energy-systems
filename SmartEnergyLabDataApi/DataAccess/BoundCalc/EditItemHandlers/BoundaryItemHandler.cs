namespace SmartEnergyLabDataApi.Data.BoundCalc;

public class BoundaryItemHandler : BaseEditItemHandler
{

    public override void Check(EditItemModel m)
    {
        if ( !m.GetString("code",out string code) && m.ItemId==0) {
            m.AddError("code","Code needs to be set");
        }

        var zoneIds = m.GetIntArray("zoneIds");
        if ( zoneIds==null && m.ItemId==0) {
            m.AddError("zoneIds","Some zones need defining");
        }
    }

    public override IDatasetIId GetItem(EditItemModel m)
    {
        var id = m.ItemId;
        return id>0 ? m.Da.BoundCalc.GetBoundary(id) : new Boundary(m.Dataset);
    }

    public override void Save(EditItemModel m)
    {
        Boundary b = (Boundary) m.Item;
        //
        if ( m.GetString("code",out string code)) {
            b.Code = code;
        }
        if ( b.Id == 0) {
            m.Da.BoundCalc.Add(b);
        }
        //
        var zoneIds = m.GetIntArray("zoneIds");
        if ( zoneIds!=null ) {
            // and boundary zones
            var existingBoundaryZones = m.Da.BoundCalc.GetBoundaryZones(b.Id);
            var newBoundaryZones = new List<BoundaryZone>();
            // Add new ones
            foreach( var zoneId in zoneIds) {
                var bz = existingBoundaryZones.Where(m=>m.Zone.Id == zoneId).FirstOrDefault();
                if ( bz==null) {
                    var zone = m.Da.BoundCalc.GetZone(zoneId);
                    if ( zone!=null ) {
                        var nbz = new BoundaryZone() {
                            Boundary = b,
                            Zone = zone,
                            Dataset = m.Dataset
                        };
                        m.Da.BoundCalc.Add(nbz);
                    }
                }
            }
            // Delete ones not defined anymore
            foreach( var bz in existingBoundaryZones) {
                if ( !zoneIds.Contains(bz.Zone.Id)) {
                    m.Da.BoundCalc.Delete(bz);
                }
            }
        }
    }

    public override List<DatasetData<object>> GetDatasetData(EditItemModel m)
    {
        using( var da = new DataAccess() ) {
            var list = new List<DatasetData<object>>();
            var boundary = (Boundary) m.Item;
            var boundDi = da.BoundCalc.GetBoundaryDatasetData(m.Dataset.Id, m=>m.Id == boundary.Id);
            list.Add(boundDi.getBaseDatasetData());
            return list;
        }
    }
}
