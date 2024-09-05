namespace SmartEnergyLabDataApi.Data;

public class PeakDemandItemHandler : BaseEditItemHandler
{
    public override void Check(EditItemModel m)
    {

    }

    public override List<DatasetData<object>> GetDatasetData(EditItemModel m)
    {
        return new List<DatasetData<object>>();
    }

    public override IId GetItem(EditItemModel m)
    {
        var id = m.ItemId;
        var obj = id>0 ? m.Da.Elsi.GetPeakDemand(id) : new PeakDemand(m.Dataset);
        if ( obj==null ) {
            throw new Exception($"Cannot find peak demand with id=[{id}]");
        }
        return obj;
    }

    public override void Save(EditItemModel m)
    {
    }
}