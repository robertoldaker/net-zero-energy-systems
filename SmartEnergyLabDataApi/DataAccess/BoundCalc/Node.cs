using System.Text.Json.Serialization;
using NHibernate.Linq.Functions;
using NHibernate.Mapping.Attributes;
using Org.BouncyCastle.Asn1.Mozilla;
using SmartEnergyLabDataApi.BoundCalc;

namespace SmartEnergyLabDataApi.Data.BoundCalc
{
    [ApplicationGroup(ApplicationGroup.BoundCalc)]
    [Class(0, Table = "boundcalc_nodes")]
    public class Node : IDatasetIId {
        public Node()
        {
            Generators = new List<Generator>();
            DeletedGenerators = new List<Generator>();
            NewGenerators = new List<Generator>();
        }

        public Node(Dataset dataset) : this()
        {
            this.Dataset = dataset;
        }

        /// <summary>
        /// Database identifier
        /// </summary>
        [Id(0, Name = "Id", Type = "int")]
        [Generator(1, Class = "identity")]
        public virtual int Id { get; set; }

        [Property()]
        public virtual string Code { get; set; }

        [Property()]
        public virtual double Demand { get; set; }

        [Property()]
        public virtual double Generation_A { get; set; }

        [Property()]
        public virtual double Generation_B { get; set; }

        public virtual IList<Generator> Generators { get; set; }
        public virtual double Generation
        {
            get {
                double generation = 0;
                foreach (var gen in Generators) {
                    generation += gen.ScaledGenerationPerNode != null ? (double)gen.ScaledGenerationPerNode : 0;
                }
                return generation;
            }
        }
        public virtual IList<Generator> DeletedGenerators { get; set; }

        public virtual IList<Generator> NewGenerators { get; set; }

        [Property()]
        public virtual int? Gen_Zone { get; set; }

        [Property()]
        public virtual int? Dem_zone { get; set; }

        [Property()]
        public virtual bool Ext { get; set; }

        [Property()]
        public virtual int Voltage { get; set; }


        [ManyToOne(Column = "ZoneId", Cascade = "none", Fetch = FetchMode.Join)]
        public virtual Zone Zone { get; set; }


        /// <summary>
        /// location of node
        /// </summary>
        [ManyToOne(Column = "locationId", Cascade = "none", Fetch = FetchMode.Join)]
        public virtual GridSubstationLocation Location { get; set; }

        public virtual string Name
        {
            get {
                if (this.Location != null) {
                    return Location.Name;
                } else {
                    return null;
                }
            }
        }

        [ManyToOne(Column = "DatasetId", Cascade = "none")]
        public virtual Dataset Dataset { get; set; }

        public virtual int DatasetId
        {
            get {
                return this.Dataset.Id;
            }
        }

        public virtual string ZoneName
        {
            get {
                return Zone?.Code;
            }
        }

        public virtual double? Mismatch { get; set; }

        public virtual double? TLF { get; set; }

        public virtual double? km { get; set; }

        public virtual bool IsGSP { get; set; }

    }
}
