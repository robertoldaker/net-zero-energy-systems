using System.Text.Json.Serialization;
using NHibernate.Mapping.Attributes;

namespace SmartEnergyLabDataApi.Data
{
    [ApplicationGroup(ApplicationGroup.Elsi)]
    [Class(0, Table = "elsi_gen_capacities")]
    public class GenCapacity : IDatasetIId
    {
        public GenCapacity()
        {

        }

        public GenCapacity(Dataset dataset)
        {
            this.Dataset = dataset;
        }

        /// <summary>
        /// Database identifier
        /// </summary>
        [Id(0, Name = "Id", Type = "int")]
        [Generator(1, Class = "identity")]
        public virtual int Id { get; set; }

        /// <summary>
        /// Generation zone
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual ElsiZone Zone {get; set;}

        public virtual string ZoneStr {
            get {
                return this.Zone.ToString();
            }
        }

        /// <summary>
        /// Main generation zone main (includes distribution connected generators)
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual ElsiMainZone MainZone {get; set;}

        /// <summary>
        /// Generation type
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual ElsiGenType GenType {get; set;}

        public virtual string GenTypeStr {
            get {
                return this.GenType.ToString();
            }
        }

        public virtual string Name {
            get {
                return $"{Zone}:{GenType}";
            }
        }

        public virtual string Key {
            get {
                return this.GetKey();
            }
        }

        /// <summary>
        /// Generation and demand behaviour (relevant to solar/wind availability)
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual ElsiProfile Profile {get; set;}

        public virtual string ProfileStr {
            get {
                return this.Profile.ToString();
            }
        }

        /// <summary>
        /// FES scenario
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual ElsiScenario Scenario {get; set;}

        /// <summary>
        /// Generation capacity
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual double Capacity {get; set;}

        /// <summary>
        /// Commnity renewables
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual double CommunityRenewables {get; set;}

        /// <summary>
        /// Two degrees
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual double TwoDegrees {get; set;}

        /// <summary>
        /// Steady Progression
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual double SteadyProgression {get; set;}

        /// <summary>
        /// Consumer evolution
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual double ConsumerEvolution {get; set;}

        /// <summary>
        /// Order of entries
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual int? OrderIndex {get; set;}

        [JsonIgnore()]
        [ManyToOne(Column = "DatasetId", Cascade = "none")]
        public virtual Dataset Dataset { get; set; }


    }
}
