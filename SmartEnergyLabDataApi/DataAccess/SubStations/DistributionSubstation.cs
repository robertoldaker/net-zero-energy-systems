using NHibernate.Mapping.Attributes;
using System.Text.Json.Serialization;

namespace SmartEnergyLabDataApi.Data
{
    [Class(0, Table = "distribution_substations")]
    public class DistributionSubstation
    {
        public DistributionSubstation()
        {

        }

        public DistributionSubstation(string nr, PrimarySubstation primarySubstation)
        {
            NR = nr;
            PrimarySubstation = primarySubstation;
            GISData = new GISData(this);
            SubstationParams = new SubstationParams(this);
        }

        /// <summary>
        /// Database identifier
        /// </summary>
        [Id(0, Name = "Id", Type = "int")]
        [Generator(1, Class = "identity")]
        public virtual int Id { get; set; }

        /// <summary>
        /// External identifier
        /// </summary>
        [Property()]
        public virtual string ExternalId { get; set; }

        /// <summary>
        /// External identifier
        /// </summary>
        [Property()]
        public virtual string NR { get; set; }

        [Property()]
        public virtual string NRId { get; set; }

        /// <summary>
        /// Name of distribution substation
        /// </summary>
        [Property()]
        public virtual string Name { get; set; }

        /// <summary>
        /// Primary substation it is connected to
        /// </summary>
        [JsonIgnore]
        [ManyToOne(Column = "PrimarySubstationId", Cascade = "none")]
        public virtual PrimarySubstation PrimarySubstation { get; set; }

        /// <summary>
        /// GIS data
        /// </summary>
        [ManyToOne(Column = "GISDataId", Cascade = "all-delete-orphan", Fetch = FetchMode.Join)]
        public virtual GISData GISData { get; set; }

        /// <summary>
        /// Charging parameters
        /// </summary>
        [ManyToOne(Column = "ChargingParamsId", Cascade = "all-delete-orphan", Fetch = FetchMode.Join)]
        public virtual SubstationChargingParams ChargingParams { get; set; }

        /// <summary>
        /// Heating parameters
        /// </summary>
        [ManyToOne(Column = "HeatingParamsId", Cascade = "all-delete-orphan", Fetch = FetchMode.Join)]
        public virtual SubstationHeatingParams HeatingParams { get; set; }

        /// <summary>
        /// Substation parameters
        /// </summary>
        [ManyToOne(Column = "SubstationParamsId", Cascade = "all-delete-orphan", Fetch = FetchMode.Join)]
        public virtual SubstationParams SubstationParams { get; set; }

        public virtual IList<SubstationLoadProfile> LoadProfiles { get; set; }
        public virtual void LoadLoadProfiles(DataAccess da)
        {
            LoadProfiles = da.SubstationLoadProfiles.GetSubstationLoadProfiles(this);
        }

        public virtual IList<SubstationClassification> Classifications { get; set; }

        public virtual void LoadClassifications(DataAccess da)
        {
            Classifications = da.SubstationClassifications.GetSubstationClassifications(this);
        }
    }
}
