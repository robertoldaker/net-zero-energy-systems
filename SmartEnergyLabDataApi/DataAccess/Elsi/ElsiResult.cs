using System.Text.Json.Serialization;
using NHibernate.Mapping.Attributes;

namespace SmartEnergyLabDataApi.Data
{    
    [Class(0, Table = "elsi_results")]
    public class ElsiResult
    {
        /// <summary>
        /// Database identifier
        /// </summary>
        [Id(0, Name = "Id", Type = "int")]
        [Generator(1, Class = "identity")]
        public virtual int Id { get; set; }

        [Property()]
        public virtual ElsiScenario Scenario {get; set;}

        [Property()]
        public virtual int Day {get; set;}
        
        [JsonIgnore()]
        [Property(Type="BinaryBlob", Lazy=true)]
        public virtual byte[] Data {get; set;}

        [JsonIgnore()]
        [ManyToOne(Column = "DatasetId", Cascade = "none")]
        public virtual ElsiDataVersion Dataset {get; set;}
        
    }
}