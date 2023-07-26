using System.Text.Json.Serialization;
using NHibernate.Mapping.Attributes;

namespace SmartEnergyLabDataApi.Data
{

    [Class(0, Table = "loadflow_branches")]
    public class Branch
    {
        public Branch()
        {

        }

        /// <summary>
        /// Database identifier
        /// </summary>
        [Id(0, Name = "Id", Type = "int")]
        [Generator(1, Class = "identity")]
        public virtual int Id { get; set; }

        [Property()]
        public virtual string Region {get; set;}

        [Property()]
        public virtual string Code {get; set;}

        [Property()]
        public virtual double R {get; set;}

        [Property()]
        public virtual double X {get; set;}

        [Property()]
        public virtual double OHL {get; set;}

        [Property()]
        public virtual double Cap {get; set;}

        [Property()]
        public virtual string LinkType {get; set;}

        [JsonIgnore()]
        [ManyToOne(Column = "Node1Id", Cascade = "none")]
        public virtual Node Node1 {get; set;}

        [JsonIgnore()]
        [ManyToOne(Column = "Node2Id", Cascade = "none")]
        public virtual Node Node2 {get; set;}

        public virtual string LineName {
            get {
                return $"{Node1.Code}-{Node2.Code}:{Code}";
            }
        }

        public virtual string Node1Code {
            get {
                return Node1.Code;
            }
        }
        
        public virtual string Node2Code {
            get {
                return Node2.Code;
            }
        }
        public virtual string Node1Name {
            get {
                return Node1.Name;
            }
        }
        
        public virtual string Node2Name {
            get {
                return Node2.Name;
            }
        }
    }
}