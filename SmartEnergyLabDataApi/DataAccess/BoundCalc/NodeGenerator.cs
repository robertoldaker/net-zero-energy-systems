using System.Text.Json.Serialization;
using NHibernate.Mapping.Attributes;
using NHibernate.Type;
using SmartEnergyLabDataApi.BoundCalc;

namespace SmartEnergyLabDataApi.Data.BoundCalc
{
    [ApplicationGroup(ApplicationGroup.BoundCalc)]
    [Class(0, Table = "boundcalc_node_generators")]
    public class NodeGenerator : IDatasetIId {
        public NodeGenerator()
        {
        }

        public NodeGenerator(Dataset dataset, Node node, Generator gen)
        {
            Node = node;
            Generator = gen;
            Dataset = dataset;
        }

        public NodeGenerator(Dataset dataset)
        {
            this.Dataset = dataset;
        }

        /// <summary>
        /// Database identifier
        /// </summary>
        [Id(0, Name = "Id", Type = "int")]
        [Generator(1, Class = "identity")]
        public virtual int Id { get; set; }

        [JsonIgnore()]
        [ManyToOne(Column = "DatasetId", Cascade = "none")]
        public virtual Dataset Dataset { get; set; }
        public virtual int DatasetId
        {
            get {
                return this.Dataset.Id;
            }
        }

        [JsonIgnore()]
        [ManyToOne(Column = "NodeId", Cascade = "none")]
        public virtual Node Node { get; set; }
        public virtual int NodeId
        {
            get {
                return this.Node.Id;
            }
        }

        [JsonIgnore()]
        [ManyToOne(Column = "GeneratorId", Cascade = "none")]
        public virtual Generator Generator { get; set; }
        public virtual int GeneratorId
        {
            get {
                return this.Generator.Id;
            }
        }

    }
}
