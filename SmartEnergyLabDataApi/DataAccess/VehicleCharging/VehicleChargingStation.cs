using NHibernate.Mapping.Attributes;
using System.Text.Json.Serialization;

namespace SmartEnergyLabDataApi.Data
{

    public enum VehicleChargingStationSource { Manual, OpenChargeMap }

    [Class(0, Table = "vehicle_charging_stations")]
    public class VehicleChargingStation
    {
        public VehicleChargingStation()
        {

        }

        public VehicleChargingStation(string externalId, PrimarySubstation? primarySubstation)
        {
            ExternalId = externalId;
            GISData = new GISData();
            PrimarySubstation = primarySubstation;
            Connections = new List<VehicleChargingConnection>();
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
        /// Name of vehicle charging station
        /// </summary>
        [Property()]
        public virtual string Name { get; set; }

        /// <summary>
        /// Source of the charging station
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual VehicleChargingStationSource Source {get; set;}

        /// <summary>
        /// Primary substation it is connected to
        /// </summary>
        [JsonIgnore]
        [ManyToOne(Column = "PrimarySubstationId", Cascade = "none")]
        public virtual PrimarySubstation? PrimarySubstation { get; set; }
        
        [Property(0)]
        [Formula(1, Content = "( primarysubstationid )")] 
        public virtual int PrimarySubstationId {get; set;}

        /// <summary>
        /// GIS data
        /// </summary>
        [ManyToOne(Column = "GISDataId", Cascade = "all-delete-orphan", Fetch = FetchMode.Join)]
        public virtual GISData GISData { get; set; }

        /// <summary>
        /// Connections available at the charging station
        /// </summary>
        /// <value></value>
        public virtual IList<VehicleChargingConnection> Connections {get; set;}        

    }
}

