using NHibernate.Mapping.Attributes;

namespace SmartEnergyLabDataApi.Data
{
    [ApplicationGroup(ApplicationGroup.Elsi)]
    [Class(0, Table = "elsi_availordemands")]
    public class AvailOrDemand : IId
    {
        public AvailOrDemand()
        {

        }

        /// <summary>
        /// Database identifier
        /// </summary>
        [Id(0, Name = "Id", Type = "int")]
        [Generator(1, Class = "identity")]
        public virtual int Id { get; set; }

        /// <summary>
        /// Day of year 1 - 365
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual int Day {get; set;}

        /// <summary>
        /// Generation & demand behaviour (relevant to solar/wind availability)
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual ElsiProfile Profile {get; set;}

        /// <summary>
        /// Period of day
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual ElsiPeriod Period {get; set;}

        /// <summary>
        /// Data type of generation
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual ElsiGenDataType DataType {get; set;}

        /// <summary>
        /// Value of generation as a % (either availability or of peak Demand)
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual double Value {get; set;}

    }
}