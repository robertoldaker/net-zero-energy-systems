using NHibernate.Mapping.Attributes;
using NHibernate.Classic;
using System.Text.Json.Serialization;
using MySqlX.XDevAPI;

namespace SmartEnergyLabDataApi.Data
{

    [Class(0, Table = "gis_data")]
    public class GISData : ILifecycle
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

        [Property(Formula = "( select count(*) from gis_boundaries gb where gb.gisdataid = id )")]
        public virtual int NumBoundaries {get; set;}

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

        public virtual LifecycleVeto OnDelete(NHibernate.ISession s)
        {
            // Delete all boundaries pointing at this GISData record
            var boundaries = s.QueryOver<GISBoundary>().Where(m=>m.GISData.Id==this.Id).List();
            foreach( var b in boundaries) {
                s.Delete(b);
            }
            return LifecycleVeto.NoVeto;
        }

        public virtual void OnLoad(NHibernate.ISession s, object id)
        {
        }

        public  virtual LifecycleVeto OnSave(NHibernate.ISession s)
        {
            return LifecycleVeto.NoVeto;
        }

        public virtual LifecycleVeto OnUpdate(NHibernate.ISession s)
        {
            return LifecycleVeto.NoVeto;
        }
    }
}
