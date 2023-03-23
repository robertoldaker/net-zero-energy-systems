using NHibernate.Mapping.Attributes;
using System.Text.Json.Serialization;

namespace SmartEnergyLabDataApi.Data
{
    [Class(0, Table = "vehicle_charging_connection_types")]
    public class VehicleChargingConnectionType
    {
        public VehicleChargingConnectionType()
        {

        }

        public VehicleChargingConnectionType(string externalId)
        {
            ExternalId = externalId;
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
        /// Name of connection type
        /// </summary>
        [Property()]
        public virtual string Name { get; set; }

        /// <summary>
        /// Formal name of connection type
        /// </summary>
        [Property()]
        public virtual string FormalName { get; set; }

    }
}