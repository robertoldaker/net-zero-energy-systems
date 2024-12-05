using System.Text.Json.Serialization;
using NHibernate.Mapping.Attributes;

namespace SmartEnergyLabDataApi.Data
{
    [ApplicationGroup(ApplicationGroup.Elsi)]
    [Class(0, Table = "elsi_links")]
    public class Link : IDataset, IId
    {
        public Link()
        {

        }

        public Link(Dataset dataset) {
            this.Dataset = dataset;
        }

        /// <summary>
        /// Database identifier
        /// </summary>
        [Id(0, Name = "Id", Type = "int")]
        [Generator(1, Class = "identity")]
        public virtual int Id { get; set; }

        /// <summary>
        /// Name of link
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual string Name {get; set;}

        /// <summary>
        /// Sending zone
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual ElsiMainZone FromZone {get; set;}
        public virtual string FromZoneStr {
            get {
                return FromZone.ToString();
            }
        }

        /// <summary>
        /// Receiving zone
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual ElsiMainZone ToZone {get; set;}
        public virtual string ToZoneStr {
            get {
                return ToZone.ToString();
            }
        }

        /// <summary>
        /// MW that can be taken from the zone
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual double Capacity {get; set;}

        /// <summary>
        /// MW that can be taken to the zone
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual double RevCap {get; set;}

        /// <summary>
        /// Difference between sent and received power
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual double Loss {get; set;}

        /// <summary>
        /// True means flow constrained in market phase, false means flow may exceed capacity in market phase. All flows constrained in balance phase.
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual bool Market {get; set;}

        /// <summary>
        /// Import tariff at from zone
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual double ITF {get; set;}

        /// <summary>
        /// Import tariff at to zone
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual double ITT {get; set;}

        /// <summary>
        /// Balance import tariff premium at from zone
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual double BTF {get; set;}

        /// <summary>
        /// Balance import tariff premium at to zone
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual double BTT {get; set;}

        [JsonIgnore()]
        [ManyToOne(Column = "DatasetId", Cascade = "none")]
        public virtual Dataset Dataset { get; set; }

    }
}