using System.Text.Json.Serialization;
using NHibernate.Mapping.Attributes;

namespace SmartEnergyLabDataApi.Data
{
    [ApplicationGroup(ApplicationGroup.Elsi)]
    [Class(0, Table = "elsi_peak_demands")]
    public class PeakDemand : IDataset, IId
    {
        public PeakDemand()
        {
        }

        public PeakDemand(Dataset dataset) {
            this.Dataset = dataset;
        }

        /// <summary>
        /// Database identifier
        /// </summary>
        [Id(0, Name = "Id", Type = "int")]
        [Generator(1, Class = "identity")]
        public virtual int Id { get; set; }

        /// <summary>
        /// Zone
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual ElsiMainZone? MainZone {get; set;}
        public virtual string MainZoneStr {
            get {
                return MainZone.ToString();
            }
        }

        /// <summary>
        /// Profile
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual ElsiProfile Profile {get; set;}
        public virtual string ProfileStr {
            get {
                return Profile.ToString();
            }
        }

        /// <summary>
        /// Scenario
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual ElsiScenario? Scenario {get; set;}

        /// <summary>
        /// Peak demand in MW
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual double Peak {get; set;}

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

        [JsonIgnore()]
        [ManyToOne(Column = "DatasetId", Cascade = "none")]
        public virtual Dataset Dataset { get; set; }


    }    
}