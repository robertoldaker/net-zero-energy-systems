using NHibernate.Mapping.Attributes;
using System.Text.Json.Serialization;

namespace SmartEnergyLabDataApi.Data
{

    [Class(0, Table = "gis_data")]
    public class GISData
    {
        public GISData()
        {

        }
        
        public GISData(VehicleChargingStation chargingStation)
        {
            VehicleChargingStation = chargingStation;
        }

        public GISData(GeographicalArea geographicalArea)
        {
            GeographicalArea = geographicalArea;
        }

        public GISData(DistributionSubstation distributionSubstation)
        {
            DistributionSubstation = distributionSubstation;
        }

        public GISData(PrimarySubstation primarySubstation)
        {
            PrimarySubstation = primarySubstation;
        }

        /// <summary>
        /// Database identifier
        /// </summary>
        [Id(0, Name = "Id", Type = "int")]
        [Generator(1, Class = "identity")]
        public virtual int Id { get; set; }

        [Property()]
        public virtual double Latitude { get; set; }

        [Property()]
        public virtual double Longitude { get; set; }

        [Property(Type="HaloSoft.DataAccess.DoubleArrayType, DataAccessBase")]
        [Column(SqlType = "double precision[]", Name = "BoundaryLatitudes")]
        public virtual double[] BoundaryLatitudes { get; set; }

        [Property(Type="HaloSoft.DataAccess.DoubleArrayType, DataAccessBase")]
        [Column(SqlType = "double precision[]", Name = "BoundaryLongitudes")]
        public virtual double[] BoundaryLongitudes { get; set; }

        [JsonIgnore]
        [ManyToOne(Column = "GeographicalAreaId", Cascade = "none")]
        public virtual GeographicalArea GeographicalArea { get; set; }

        [JsonIgnore]
        [ManyToOne(Column = "DistributionSubstationId", Cascade = "none")]
        public virtual DistributionSubstation DistributionSubstation { get; set; }

        [JsonIgnore]
        [ManyToOne(Column = "PrimarySubstationId", Cascade = "none")]
        public virtual PrimarySubstation PrimarySubstation { get; set; }

        [JsonIgnore]
        [ManyToOne(Column = "VehicleChargingStationId", Cascade = "none")]
        public virtual VehicleChargingStation VehicleChargingStation { get; set; }
    }
}
