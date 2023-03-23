using NHibernate.Mapping.Attributes;
using System.Text.Json.Serialization;

namespace SmartEnergyLabDataApi.Data
{
    [Class(0, Table = "vehicle_charging_current_types")]
    public class VehicleChargingCurrentType
    {
        public VehicleChargingCurrentType()
        {

        }

        public VehicleChargingCurrentType(string externalId)
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
        /// Name of current type
        /// </summary>
        [Property()]
        public virtual string Name { get; set; }

        /// <summary>
        /// Description of current type
        /// </summary>
        [Property()]
        public virtual string Description { get; set; }


    }
}