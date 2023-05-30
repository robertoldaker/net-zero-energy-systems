using NHibernate.Mapping.Attributes;
using System.Text.Json.Serialization;

namespace SmartEnergyLabDataApi.Data
{
    [Class(0, Table = "primary_substations")]
    public class PrimarySubstation
    {
        public PrimarySubstation()
        {

        }

        public PrimarySubstation(string nr, string nrId, GridSupplyPoint gsp)
        {
            NR = nr;
            NRId = nrId;
            SetGSP(gsp);
            GISData = new GISData(this);
        }

        public PrimarySubstation(string siteFunctionalLocation, GridSupplyPoint gsp)
        {
            SiteFunctionalLocation = siteFunctionalLocation;
            SetGSP(gsp);
            GISData = new GISData(this);
        }

        private void SetGSP(GridSupplyPoint gsp) {
            GridSupplyPoint = gsp;
            if ( gsp!=null) {
                DistributionNetworkOperator = gsp.DistributionNetworkOperator;
                GeographicalArea = gsp.GeographicalArea;
            }
        }

        [Id(0, Name = "Id", Type = "int")]
        [Generator(1, Class = "identity")]
        public virtual int Id { get; set; }

        [Property()]
        public virtual string ExternalId { get; set; }

        [Property()]
        public virtual string NR { get; set; }

        [Property()]
        public virtual string NRId { get; set; }

        [Property()]
        public virtual string SiteFunctionalLocation { get; set; }

        [Property()]
        public virtual string Name { get; set; }

        [Property(0)]
        [Formula(1, Content = "( select count(*) from distribution_substations ds where (ds.primarysubstationid = id) )")] 
        public virtual int NumberOfDistributionSubstations {get; set;}

        /// <summary>
        /// GIS data
        /// </summary>
        [ManyToOne(Column = "GISDataId", Cascade = "all-delete-orphan")]
        public virtual GISData GISData { get; set; }

        /// <summary>
        /// Grid supply point
        /// </summary>
        [JsonIgnore]
        [ManyToOne(Column = "GridSupplyPointId", Cascade = "none")]
        public virtual GridSupplyPoint GridSupplyPoint { get; set; }

        /// <summary>
        /// Geographical area
        /// </summary>
        [JsonIgnore]
        [ManyToOne(Column = "GeographicalAreaId", Cascade = "none")]
        public virtual GeographicalArea GeographicalArea { get; set; }

        /// <summary>
        /// Distribtion network operator
        /// </summary>
        [JsonIgnore]
        [ManyToOne(Column = "DistributionNetworkOperatorId", Cascade = "none")]
        public virtual DistributionNetworkOperator DistributionNetworkOperator { get; set; }
    }
}
