namespace SmartEnergyLabDataApi.Data;

public class LinkItemHandler : BaseEditItemHandler
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
        var obj = id>0 ? m.Da.Elsi.GetLink(id) : new Link(m.Dataset);
        if ( obj==null ) {
            throw new Exception($"Cannot find link with id=[{id}]");
        }
        return obj;
    }

    public override void Save(EditItemModel m)
    {
    }
}