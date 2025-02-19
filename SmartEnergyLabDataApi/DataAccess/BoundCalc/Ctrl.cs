using System.Text.Json.Serialization;
using NHibernate.Mapping.Attributes;

namespace SmartEnergyLabDataApi.Data.BoundCalc
{

    public enum BoundCalcCtrlType {  QB, // Quad Booster
                                    HVDC // High-voltage DC
                                                            }
    [ApplicationGroup(ApplicationGroup.BoundCalc)]
    [Class(0, Table = "boundcalc_ctrls")]
    public class Ctrl : IId, IDataset
    {
        public Ctrl()
        {

        }

        public Ctrl(Dataset dataset, Branch branch)
        {
            Dataset = dataset;
            Branch = branch;
        }

        /// <summary>
        /// Database identifier
        /// </summary>
        [Id(0, Name = "Id", Type = "int")]
        [Generator(1, Class = "identity")]
        public virtual int Id { get; set; }

        [Property()]
        public virtual string Region {get; set;}

        public virtual string Code {
            get {
                return Branch.Code;
            }
        }

        [Property(Column = "code")]
        public virtual string old_Code { get; set;}

        [Property()]
        public virtual double MinCtrl {get; set;}

        [Property()]
        public virtual double MaxCtrl {get; set;}

        [Property()]
        public virtual double Cost {get; set;}

        [Property()]
        public virtual BoundCalcCtrlType Type {get; set;}

        [JsonIgnore]
        [ManyToOne(Column = "BranchId", Cascade = "none")]
        public virtual Branch Branch {get; set;}

        public virtual int BranchId {
            get {
                return Branch.Id;
            }
        }

        public virtual Node Node1 {
            get {
                return Branch.Node1;
            }
        }

        [JsonIgnore]
        [ManyToOne(Column = "Node1Id", Cascade = "none")]
        public virtual Node old_Node1 {get; set;}

        public virtual Node Node2 {
            get {
                return Branch.Node2;
            }
        }

        [JsonIgnore]
        [ManyToOne(Column = "Node2Id", Cascade = "none")]
        public virtual Node old_Node2 {get; set;}

        [JsonIgnore()]
        [ManyToOne(Column = "DatasetId", Cascade = "none")]
        public virtual Dataset Dataset { get; set; }

        public virtual int DatasetId {
            get {
                return Dataset.Id;
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

        public virtual string LineName {
            get {
                return Branch.LineName;
            }
        }

        public virtual string DisplayName {
            get {
                return Branch.DisplayName;
            }
        }


        // Control set point (csp)
        public virtual double? SetPoint {get; set;}


    }
}


