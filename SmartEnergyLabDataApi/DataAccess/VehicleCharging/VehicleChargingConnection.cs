using NHibernate.Mapping.Attributes;
using System.Text.Json.Serialization;

namespace SmartEnergyLabDataApi.Data
{
    [Class(0, Table = "vehicle_charging_connections")]
    public class VehicleChargingConnection
    {
        public VehicleChargingConnection()
        {

        }

        public VehicleChargingConnection(string externalId, VehicleChargingStation station) {
            VehicleChargingStation = station;
            ExternalId = externalId;
        }

        /// <summary>
        /// Database identifier
        /// </summary>
        [Id(0, Name = "Id", Type = "int")]
        [Generator(1, Class = "identity")]
        public virtual int Id { get; set; }


        /// <summary>
        /// External id for the connection
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual string ExternalId {get; set;}

        /// <summary>
        /// Number of connection of this type
        /// </summary>
        [Property()]
        public virtual int Quantity {get; set;}

        /// <summary>
        /// Current in amps
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual double Current {get; set;}

        /// <summary>
        /// Voltage
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual double Voltage {get; set;}

        /// <summary>
        /// Power in Kw
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual double PowerKw {get; set;}

        /// <summary>
        /// Vehicle charging connection type
        /// </summary>
        [ManyToOne(Column = "VehicleChargingConnectionTypeId", Cascade = "none", Fetch = FetchMode.Join)]
        public virtual VehicleChargingConnectionType ConnectionType { get; set; }

        /// <summary>
        /// Vehicle charging current type
        /// </summary>
        [ManyToOne(Column = "VehicleChargingCurrentTypeId", Cascade = "none", Fetch = FetchMode.Join)]
        public virtual VehicleChargingCurrentType CurrentType { get; set; }

        /// <summary>
        /// Vehicle charging station
        /// </summary>
        [JsonIgnore]
        [ManyToOne(Column = "VehicleChargingStationId", Cascade = "none")]
        public virtual VehicleChargingStation VehicleChargingStation { get; set; }

    }
}