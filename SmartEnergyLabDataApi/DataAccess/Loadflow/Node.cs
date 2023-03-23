using System.Text.Json.Serialization;
using NHibernate.Mapping.Attributes;

namespace SmartEnergyLabDataApi.Data
{

    [Class(0, Table = "loadflow_nodes")]
    public class Node
    {
        public Node()
        {

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
        }

        [Property()]
        public virtual int? Gen_Zone {get; set;}

        [Property()]
        public virtual int? Dem_zone {get; set;}

        [Property()]
        public virtual bool Ext {get; set;}
        
        [ManyToOne(Column = "ZoneId", Cascade = "none")]
        public virtual Zone Zone {get; set;}

    }
}
