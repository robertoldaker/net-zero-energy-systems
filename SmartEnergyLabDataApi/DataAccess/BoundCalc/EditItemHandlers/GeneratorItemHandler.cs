
using NHibernate.Criterion;

namespace SmartEnergyLabDataApi.Data.BoundCalc;


public class GeneratorItemHandler : BaseEditItemHandler {

    public override string BeforeUndelete(EditItemModel m)
    {
        //
        var gen = (Generator)m.Item;
        var nodeGenDi = m.Da.BoundCalc.GetNodeGeneratorDatasetData(m.Dataset.Id, m => m.Generator.Id == gen.Id);

        foreach( var ng in nodeGenDi.DeletedData) {
            var ue = m.Da.Datasets.GetDeleteUserEdit(m.Dataset.Id, ng );
            if (ue != null) {
                m.Da.Datasets.Delete(ue);
            }
        }
        return "";
    }

    public override string BeforeDelete(EditItemModel m, bool isSourceEdit)
    {
        var gen = (Generator)m.Item;
        var nodeGenDi = m.Da.BoundCalc.GetNodeGeneratorDatasetData(m.Dataset.Id, m => m.Generator.Id == gen.Id);
        foreach (var nd in nodeGenDi.Data) {
            bool isSrcEdit = nd.Dataset.Id == m.Dataset.Id;
            if (isSrcEdit) {
                // remove item
                m.Da.Session.Delete(nd);
                // remove all user edits associated with object
                var userEdits = m.Da.Datasets.GetUserEdits(nd.GetType().Name, ((IId)nd).Id.ToString());
                foreach (var ue in userEdits) {
                    m.Da.Datasets.Delete(ue);
                }
            } else {
                m.Da.Datasets.AddDeleteUserEdit((IId)nd, m.Dataset);
            }
        }
        return "";
    }
    public override void Check(EditItemModel m)
    {
        // name
        if (m.GetString("name", out string name)) {
            if (string.IsNullOrEmpty(name)) {
                m.AddError("name", "Needs to be set to something");
            } else if (m.Da.BoundCalc.GeneratorExists(m.ItemId, m.Dataset.Id, name, out Dataset? dataset)) {
                m.AddError("name", $"Generator already exists with name [{name}] in dataset [{dataset?.Name}]");
            }
        } else if (m.ItemId == 0) {
            m.AddError("name", "Needs to be set to something");
        }

        // type
        m.CheckEnum<GeneratorType>("type");

        // capacity
        m.CheckDouble("capacity");
    }

    public override IId GetItem(EditItemModel m)
    {
        var id = m.ItemId;
        return id > 0 ? m.Da.BoundCalc.GetGenerator(id) : new Generator(m.Dataset);
    }

    public override void Save(EditItemModel m)
    {
        Generator gen = (Generator)m.Item;
        //
        if (m.GetString("name", out string name)) {
            gen.Name = name;
        }
        var capacity = m.CheckDouble("capacity");
        if (capacity != null) {
            gen.Capacity = (double)capacity;
        }

        var gType = m.CheckEnum<GeneratorType>("type");
        if (gType != null) {
            gen.Type = (GeneratorType)gType;
        }
        //
        if (gen.Id == 0) {
            m.Da.BoundCalc.Add(gen);
        }
    }

    public override List<DatasetData<object>> GetDatasetData(EditItemModel m)
    {
        using (var da = new DataAccess()) {
            var list = new List<DatasetData<object>>();
            var gen = (Generator)m.Item;
            var genDi = da.BoundCalc.GetGeneratorDatasetData(m.Dataset.Id);
            var nodeDi = da.BoundCalc.GetNodeDatasetData(m.Dataset.Id, null, true);
            var tmDi = da.BoundCalc.GetTransportModelDatasetData(m.Dataset.Id, null, true);
            var tmeDi = da.BoundCalc.GetTransportModelEntryDatasetData(m.Dataset.Id);

            foreach (var tm in tmDi.Data) {
                tm.UpdateScaling(da, m.Dataset.Id);
                tm.UpdateGenerators(genDi.Data);
            }

            list.Add(genDi.getBaseDatasetData());
            list.Add(nodeDi.getBaseDatasetData());
            list.Add(tmeDi.getBaseDatasetData());
            return list;
        }
    }

}

