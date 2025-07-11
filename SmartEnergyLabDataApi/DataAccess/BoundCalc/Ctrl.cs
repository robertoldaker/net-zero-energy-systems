using System.Text.Json.Serialization;
using NHibernate.Mapping.Attributes;

namespace SmartEnergyLabDataApi.Data.BoundCalc
{

    public enum BoundCalcCtrlType {
        QB,         // Quad Booster
        HVDC,       // High-voltage DC
        SeriesCap,  // Series capacitor
        DecInc,     // node <=> node transfer
        InterTrip,  // node <=> zone transfer
        Transfer,   // zone <=> zone transfer
                                                            }
    [ApplicationGroup(ApplicationGroup.BoundCalc)]
    [Class(0, Table = "boundcalc_ctrls")]
    public class Ctrl : IDatasetIId
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
                return (Branch!=null) ? Branch.Code : "";
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

        [JsonIgnore()]
        [ManyToOne(Column = "N1Id", Cascade = "none")]
        public virtual Node N1 { get; set; }

        [JsonIgnore()]
        [ManyToOne(Column = "N2Id", Cascade = "none")]
        public virtual Node N2 { get; set; }

        [JsonIgnore()]
        [ManyToOne(Column = "Z1Id", Cascade = "none")]
        public virtual Zone Z1 { get; set; }

        [JsonIgnore()]
        [ManyToOne(Column = "Z2Id", Cascade = "none")]
        public virtual Zone Z2 { get; set; }

        [Property()]
        public virtual double GPC1 { get; set; }

        [Property()]
        public virtual double GPC2 { get; set; }

        public virtual int BranchId
        {
            get {
                return Branch != null ? Branch.Id : 0;
            }
        }

        [JsonIgnore()]
        public virtual Node Node1 {
            get {
                return Branch!=null ? Branch.Node1 : null;
            }
        }

        [JsonIgnore()]
        public virtual Node Node2 {
            get {
                return Branch!=null ? Branch.Node2 : null;
            }
        }

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
                if (Node1 != null) {
                    return Node1.Code;
                } else if (N1 != null) {
                    return N1.Code;
                } else {
                    return "";
                }
            }
        }

        public virtual string Node2Code {
            get {
                if (Node2 != null) {
                    return Node2.Code;
                } else if (N2 != null) {
                    return N2.Code;
                } else {
                    return "";
                }
            }
        }

        public virtual int Node1Id
        {
            get {
                if (Node1 != null) {
                    return Node1.Id;
                } else if (N1 != null) {
                    return N1.Id;
                } else {
                    return 0;
                }
            }
        }

        public virtual int Node2Id
        {
            get {
                if (Node2 != null) {
                    return Node2.Id;
                } else if (N2 != null) {
                    return N2.Id;
                } else {
                    return 0;
                }
            }
        }

        public virtual string Node1Name
        {
            get {
                if (Node1 != null) {
                    return Node1.Name;
                } else if (N1 != null) {
                    return N1.Name;
                } else {
                    return "";
                }
            }
        }

        public virtual string Node2Name {
            get {
                if (Node2 != null) {
                    return Node2.Name;
                } else if (N2 != null) {
                    return N2.Name;
                } else {
                    return "";
                }
            }
        }

        public virtual int Node1LocationId {
            get {
                if (Branch != null) {
                    return Branch.Node1LocationId;
                } else if (N1 != null && N1.Location != null) {
                    return N1.Location.Id;
                } else {
                    return 0;
                }
            }
        }

        public virtual int Node2LocationId {
            get {
                if (Branch != null) {
                    return Branch.Node2LocationId;
                } else if (N2 != null && N2.Location != null) {
                    return N2.Location.Id;
                } else {
                    return 0;
                }
            }
        }

        public virtual string Zone1Code
        {
            get {
                if (Z1 != null) {
                    return Z1.Code;
                } else {
                    return "";
                }
            }
        }

        public virtual string Zone2Code
        {
            get {
                if (Z2 != null) {
                    return Z2.Code;
                } else {
                    return "";
                }
            }
        }

        public virtual int Zone1Id
        {
            get {
                if (Z1 != null) {
                    return Z1.Id;
                } else {
                    return 0;
                }
            }
        }

        public virtual int Zone2Id
        {
            get {
                if (Z2 != null) {
                    return Z2.Id;
                } else {
                    return 0;
                }
            }
        }

        public virtual string LineName
        {
            get {
                return Branch != null ? Branch.LineName : "";
            }
        }

        public virtual string DisplayName {
            get {
                return Branch!=null ? Branch.DisplayName : "";
            }
        }


        // Control set point (csp)
        public virtual double? SetPoint {get; set;}


    }
}


