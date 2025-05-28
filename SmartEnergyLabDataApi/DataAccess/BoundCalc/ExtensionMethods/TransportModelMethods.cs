using HaloSoft.EventLogger;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.ObjectPool;
using Npgsql.Replication;
using SmartEnergyLabDataApi.Models;

namespace SmartEnergyLabDataApi.Data.BoundCalc;

public static class TransportModelMethods {

    public static void UpdateScaling(this TransportModel tm, IList<Node> nodes, IList<NodeGenerator> nodeGenerators, IList<Generator> generators)
    {
        // update generator node count
        foreach (var gen in generators) {
            gen.NodeCount = nodeGenerators.Where(m => m.GeneratorId == gen.Id).Count();
        }

        //
        double totalDemand = nodes.Sum(m => m.Demand);

        // work total capacity for each transport model entry
        foreach (var tme in tm.Entries) {
            // set total capacity for each transport model entry
            var tc = generators.Where(m => m.Type == tme.GeneratorType && m.NodeCount > 0).Sum(m => m.Capacity);
            //
            tme.TotalCapacity = tc;
        }

        // Calculate scaling factors for each transport model that uses autoScaling
        var totalScalingGeneration = tm.Entries.Where(m => !m.AutoScaling).Sum(m => m.TotalCapacity * m.Scaling);
        var totalAutoScalingCapacity = tm.Entries.Where(m => m.AutoScaling).Sum(m => m.TotalCapacity);
        var scaling = (totalDemand - totalScalingGeneration) / totalAutoScalingCapacity;
        // for auto scaling set the auto scaling
        foreach (var tme in tm.Entries) {
            if (tme.AutoScaling) {
                tme.Scaling = scaling;
            }
        }

    }

    public static void UpdateScaling(this TransportModel tm, DataAccess da, int datasetId)
    {
        // get nodes
        var nodeQuery = da.Session.QueryOver<Node>();
        var diNode = new DatasetData<Node>(da, datasetId, m => m.Id.ToString(), nodeQuery);

        // get node generators
        var nodeGenQuery = da.Session.QueryOver<NodeGenerator>();
        var diNodeGen = new DatasetData<NodeGenerator>(da, datasetId, m => m.Id.ToString(), nodeGenQuery);

        // get generators
        var genQuery = da.Session.QueryOver<Generator>();
        var diGen = new DatasetData<Generator>(da, datasetId, m => m.Id.ToString(), genQuery);

        // update scaling
        tm.UpdateScaling(diNode.Data, diNodeGen.Data, diGen.Data);
    }

    public static void UpdateGenerators(this TransportModel tm, IList<Generator> generators)
    {
        var nullNodeCount = generators.Where(m => m.NodeCount == null).FirstOrDefault();
        if (nullNodeCount != null) {
            throw new Exception($"Attempt to update generators with null node count. Please call TransportModel.UpdateScaling to update nodeCount");
        }

        // Work out generation for each generator based on the transport model scaling factors and node count
        foreach (var gen in generators) {
            if (gen.NodeCount > 0) {
                var tme = tm.Entries.Where(m => m.GeneratorType == gen.Type).FirstOrDefault();
                gen.ScaledGeneration = gen.Capacity * tme.Scaling;
            }
        }
    }

}
