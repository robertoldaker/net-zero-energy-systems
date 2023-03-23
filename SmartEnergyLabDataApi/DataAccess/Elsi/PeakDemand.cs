using NHibernate.Mapping.Attributes;

namespace SmartEnergyLabDataApi.Data
{
    [Class(0, Table = "elsi_peak_demands")]
    public class PeakDemand
    {
        public PeakDemand()
        {

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

    }    
}