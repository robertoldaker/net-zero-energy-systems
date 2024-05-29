using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using NHibernate.Mapping.Attributes;
using SmartEnergyLabDataApi.Loadflow;

namespace SmartEnergyLabDataApi.Data
{    
    [Class(0, Table = "loadflow_results")]
    public class LoadflowResult
    {
        public LoadflowResult() {

        }
        
        public LoadflowResult(Dataset dataset) {
            Dataset = dataset;
        }

        /// <summary>
        /// Database identifier
        /// </summary>
        [Id(0, Name = "Id", Type = "int")]
        [Generator(1, Class = "identity")]
        public virtual int Id { get; set; }
        
        [JsonIgnore()]
        [Property(Type="BinaryBlob", Lazy=true)]
        public virtual byte[] Data {get; set;}

        [JsonIgnore()]
        [ManyToOne(Column = "DatasetId", Cascade = "none")]
        public virtual Dataset Dataset {get; set;}
        
    }
}