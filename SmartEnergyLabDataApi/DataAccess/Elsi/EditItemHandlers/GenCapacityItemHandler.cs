
namespace SmartEnergyLabDataApi.Data;

public class GenCapacityItemHandler : BaseEditItemHandler
{
    public override void Check(EditItemModel m)
    {

    }

    public override List<DatasetData<object>> GetDatasetData(EditItemModel m)
    {
        return new List<DatasetData<object>>();
    }

    public override IDatasetIId GetItem(EditItemModel m)
    {
        var id = m.ItemId;
        var obj = id>0 ? m.Da.Elsi.GetGenCapacity(id) : new GenCapacity(m.Dataset);
        if ( obj==null ) {
            throw new Exception($"Cannot find gen capacity with id=[{id}]");
        }
        return obj;
    }

    public override void Save(EditItemModel m)
    {
    }
}
