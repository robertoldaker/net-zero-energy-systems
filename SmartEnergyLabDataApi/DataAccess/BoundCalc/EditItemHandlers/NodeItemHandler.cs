using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components.Routing;
using NHibernate;
using NHibernate.AdoNet.Util;
using NHibernate.Criterion;
using Org.BouncyCastle.Crypto.Digests;

namespace SmartEnergyLabDataApi.Data.BoundCalc;

public class NodeItemHandler : BaseEditItemHandler {
    public override string BeforeUndelete(EditItemModel m)
    {
        // Location
        var node = (Node)m.Item;
        if (node.Location != null) {
            m.UnDeleteObject<GridSubstationLocation>(node.Location);
        }
        // branches
        (var branchDi, var ctrlDi) = m.Da.BoundCalc.GetBranchDatasetData(m.Dataset.Id, m => m.Node1.Id == node.Id || m.Node2.Id == node.Id, false);
        var nodeIds = branchDi.DeletedData.Where(m => m.Node1.Id != node.Id).Select(m => m.Node1.Id).ToArray();
        // ensure node1 is undeleted
        foreach (var nodeId in nodeIds) {
            m.UnDeleteObject<Node>(nodeId);
        }
        nodeIds = branchDi.DeletedData.Where(m => m.Node2.Id != node.Id).Select(m => m.Node2.Id).ToArray();
        // and node 2
        foreach (var nodeId in nodeIds) {
            m.UnDeleteObject<Node>(nodeId);
        }
        // undelete the branch
        foreach (var br in branchDi.DeletedData) {
            m.UnDeleteObject(br);
            if (br.Ctrl != null) {
                m.UnDeleteObject(br.Ctrl);
            }
        }

        // NodeGenerators
        var nodeGenDi = m.Da.BoundCalc.GetNodeGeneratorDatasetData(m.Dataset.Id, m => m.Node.Id == node.Id);
        var genIds = nodeGenDi.DeletedData.Select(m => m.Generator.Id).ToArray();
        foreach (var genId in genIds) {
            m.UnDeleteObject<Generator>(genId);
        }
        foreach (var ng in nodeGenDi.DeletedData) {
            m.UnDeleteObject(ng);
        }
        return "";
    }

    public override string BeforeDelete(EditItemModel m, bool isSourceEdit)
    {
        /*var numBranches = m.Da.BoundCalc.GetBranchCountForNode(m.ItemId, isSourceEdit);
        if ( numBranches > 0 ) {
            return $"Cannot delete node as used by <b>{numBranches}</b> branches";
        }*/

        var node = (Node)m.Item;
        // Branches
        (var branchDi, var ctrlDi) = m.Da.BoundCalc.GetBranchDatasetData(m.Dataset.Id, m => m.Node1.Id == node.Id || m.Node2.Id == node.Id, false);
        foreach (var br in branchDi.Data) {
            var deleteItem = m.DeleteObject(br);
            // If source deleted is a ctrl branch add the ctrl to the list of deletedItems
            if (br.Ctrl != null) {
                if (deleteItem.isSourceDelete) {
                    m.DeletedItems.Add(new DeletedItem(br.Ctrl) { isSourceDelete = true });
                } else {
                    m.DeleteObject(br.Ctrl);
                }
            }
        }

        // Node Generators
        var nodeGenDi = m.Da.BoundCalc.GetNodeGeneratorDatasetData(m.Dataset.Id, m => m.Node.Id == node.Id);
        foreach (var nd in nodeGenDi.Data) {
            m.DeleteObject(nd, false);
        }
        return "";
    }

    public override void Check(EditItemModel m)
    {
        if (m.GetString("code", out string code)) {
            Regex regex = new Regex(@"^[A-Z]{4}\d");
            var codeMatch = regex.Match(code);
            if (!codeMatch.Success) {
                m.AddError("code", "Code must be in form <uppercase-4-letter-code><voltage id><anything>");
            }
            if (m.Da.BoundCalc.NodeExists(m.Dataset.Id, code, out Dataset ds)) {
                m.AddError("code", $"Node already exists in dataset [{ds.Name}]");
            }
        } else if (m.ItemId == 0) {
            m.AddError("code", "Code must be set");
        }
        // demand
        m.CheckDouble("demand");
        // generation A
        //??m.CheckDouble("generation_A");
        // generation B
        //??m.CheckDouble("generation_B");
        // external
        m.CheckBoolean("ext");
        // zone id
        if (m.CheckInt("zoneId") == null && m.ItemId == 0) {
            m.AddError("zoneId", "Zone must be set");
        }
    }

    public override IDatasetIId GetItem(EditItemModel model)
    {
        var id = model.ItemId;
        return id > 0 ? model.Da.BoundCalc.GetNode(id) : new Node(model.Dataset);
    }

    public override void Save(EditItemModel m)
    {
        Node node = (Node)m.Item;
        //
        if (m.GetString("code", out string code)) {
            node.Code = code;
            node.SetVoltage();
            node.SetLocation(m.Da);
        }
        // demand
        var demand = m.CheckDouble("demand");
        if (demand != null) {
            node.Demand = (double)demand;
        }
        // generation A
        /*var generation_A = m.CheckDouble("generation_A");
        if ( generation_A!=null ) {
            node.Generation_A = (double) generation_A;
        }
        // generation B
        var generation_B = m.CheckDouble("generation_B");
        if ( generation_B!=null ) {
            node.Generation_B = (double) generation_B;
        }*/
        updateGenerators(m);

        // external
        var ext = m.CheckBoolean("ext");
        if (ext != null) {
            node.Ext = (bool)ext;
        }
        // zone id
        var zoneId = m.CheckInt("zoneId");
        if (zoneId != null) {
            var zone = m.Da.BoundCalc.GetZone((int)zoneId);
            node.Zone = zone;
        }

        //
        if (node.Id == 0) {
            m.Da.BoundCalc.Add(node);
        }
    }

    public override void UpdateUserEdits(EditItemModel m)
    {
        // base class
        base.UpdateUserEdits(m);
        //
        updateGenerators(m);
    }

    private void updateGenerators(EditItemModel m)
    {
        Node node = (Node)m.Item;
        var generatorIds = m.GetIntArray("generatorIds");
        if (generatorIds != null) {
            // and node generators
            int datasetId = m.Dataset.Id;
            var nodeDi = m.Da.BoundCalc.GetNodeGeneratorDatasetData(datasetId, m => m.Node.Id == node.Id);
            var currentNodeGenerators = nodeDi.Data;
            var deletedNodeGenerators = nodeDi.DeletedData;
            var newNodeGenerators = new List<NodeGenerator>();
            // Add new ones
            foreach (var genId in generatorIds) {
                var ng = currentNodeGenerators.Where(m => m.Generator.Id == genId).FirstOrDefault();
                if (ng == null) {
                    var ngDeleted = deletedNodeGenerators.Where(m => m.Generator.Id == genId).FirstOrDefault();
                    if (ngDeleted != null) {
                        m.UnDeleteObject(ngDeleted);
                        // make sure the geberator is undeleted
                        m.UnDeleteObject<Generator>(genId);
                    } else {
                        var gen = m.Da.BoundCalc.GetGenerator(genId);
                        if (gen != null) {
                            var newNg = new NodeGenerator() {
                                Node = node,
                                Generator = gen,
                                Dataset = m.Dataset
                            };
                            m.Da.BoundCalc.Add(newNg);
                        }
                    }
                }
            }
            // Delete ones not defined anymore
            foreach (var ng in currentNodeGenerators) {
                if (!generatorIds.Contains(ng.Generator.Id)) {
                    m.DeleteObject(ng, false);
                }
            }
        }
    }

    public override List<DatasetData<object>> GetDatasetData(EditItemModel m)
    {
        using (var da = new DataAccess(false)) {
            var list = new List<DatasetData<object>>();
            var node = (Node)m.Item;
            // need all nodes since they can all change since generation
            var nodeDi = da.BoundCalc.GetNodeDatasetData(m.Dataset.Id, null, true);
            m.EditItem.UpdateNodeGeneration(da, m.Dataset.Id, nodeDi);
            // also all generators since this will include any deleted from the node
            var genDi = da.BoundCalc.GetGeneratorDatasetData(m.Dataset.Id, null);

            // branches and controls that reference this node
            (var branchDi, var ctrlDi) = da.BoundCalc.GetBranchDatasetData(m.Dataset.Id, m => m.Node1.Id == node.Id || m.Node2.Id == node.Id, true);
            var tmeDi = da.BoundCalc.GetGenerationModelEntryDatasetData(m.Dataset.Id);

            list.Add(nodeDi.getBaseDatasetData());
            list.Add(genDi.getBaseDatasetData());
            list.Add(branchDi.getBaseDatasetData());
            list.Add(ctrlDi.getBaseDatasetData());
            list.Add(tmeDi.getBaseDatasetData());
            return list;
        }
    }

    private void addDummyUserEdits(DatasetData<Node> nodeDi)
    {
        foreach (var n in nodeDi.Data) {
            int datasetId = n.Dataset.Id;
            var gen = n.Generators.Where(m => m.Dataset.Id != datasetId).FirstOrDefault();
            if (gen != null) {
                nodeDi.UserEdits.Add(new UserEdit() {
                    ColumnName = "Generators",
                    TableName = "Node",
                    Key = n.Id.ToString()
                });
            }
        }
    }
}
