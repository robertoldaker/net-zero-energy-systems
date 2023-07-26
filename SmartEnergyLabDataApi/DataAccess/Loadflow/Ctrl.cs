using System.Text.Json.Serialization;
using NHibernate.Mapping.Attributes;

namespace SmartEnergyLabDataApi.Data
{

    public enum LoadflowCtrlType {  QB, // Quad Booster
                                    HVDC // High-voltage DC
                                                            }

    [Class(0, Table = "loadflow_ctrls")]
    public class Ctrl
    {
        public Ctrl()
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
        public virtual double MinCtrl {get; set;}

        [Property()]
        public virtual double MaxCtrl {get; set;}

        [Property()]
        public virtual double Cost {get; set;}

        [Property()]
        public virtual LoadflowCtrlType Type {get; set;}

        [JsonIgnore]
        [ManyToOne(Column = "Node1Id", Cascade = "none")]
        public virtual Node Node1 {get; set;}

        [JsonIgnore]
        [ManyToOne(Column = "Node2Id", Cascade = "none")]
        public virtual Node Node2 {get; set;}

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


