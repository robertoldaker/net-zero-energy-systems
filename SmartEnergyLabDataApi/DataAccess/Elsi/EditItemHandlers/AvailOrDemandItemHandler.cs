namespace SmartEnergyLabDataApi.Data;

public class AvailOrDemandItemHandler : BaseEditItemHandler
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
        var obj = id>0 ? m.Da.Elsi.GetAvailOrDemand(id) : new AvailOrDemand();
        if ( obj==null ) {
            throw new Exception($"Cannot find availOrDemand with id=[{id}]");
        }
        return obj;
    }

    public override void Save(EditItemModel m)
    {
    }
}