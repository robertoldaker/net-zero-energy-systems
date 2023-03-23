using NHibernate.Mapping.Attributes;

namespace SmartEnergyLabDataApi.Data
{

    [Class(0, Table = "elsi_gen_capacities")]
    public class GenCapacity
    {
        public GenCapacity()
        {

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
        /// Order of entries
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual int? OrderIndex {get; set;}

    }
}