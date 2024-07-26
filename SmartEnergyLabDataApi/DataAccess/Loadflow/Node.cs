using System.Text.Json.Serialization;
using NHibernate.Mapping.Attributes;

namespace SmartEnergyLabDataApi.Data
{

    [Class(0, Table = "loadflow_nodes")]
    public class Node : IId, IDataset
    {
        public Node()
        {

        }

        public Node(Dataset dataset)
        {
            this.Dataset = dataset;
        }
        
        /// <summary>
        /// Database identifier
        /// </summary>
        [Id(0, Name = "Id", Type = "int")]
        [Generator(1, Class = "identity")]
        public virtual int Id { get; set; }

        [Property()]
        public virtual string Code {get; set;}

        [Property()]
        public virtual double Demand {get; set;}

        [Property()]
        public virtual double Generation_A {get; set;}

        [Property()]
        public virtual double Generation_B {get; set;}

        public virtual double Generation {
            get {
                return Generation_A;
            }

            set {
                Generation_A = value;
            }
        }

        [Property()]
        public virtual int? Gen_Zone {get; set;}

        [Property()]
        public virtual int? Dem_zone {get; set;}

        [Property()]
        public virtual bool Ext {get; set;}

        [Property()]
        public virtual int Voltage {get; set;}
        
        public virtual string Name { 
            get {
                if ( this.Location!=null ) {
                    return Location.Name;
                } else {
                    return null;
                }
            }
        }

        [ManyToOne(Column = "ZoneId", Cascade = "none")]
        public virtual Zone Zone {get; set;}
        

        /// <summary>
        /// location of node
        /// </summary>
        [ManyToOne(Column = "locationId", Cascade = "none", Fetch = FetchMode.Join)]
        public virtual GridSubstationLocation Location { get; set; }

        [JsonIgnore()]
        [ManyToOne(Column = "DatasetId", Cascade = "none")]
        public virtual Dataset Dataset { get; set; }

        public virtual string ZoneName {
            get {
                return Zone?.Code;
            }
        }

        public virtual double? Mismatch {get; set;}    

        public virtual int DatasetId {
            get {
                return this.Dataset.Id;
            }
        }    


    }
}
