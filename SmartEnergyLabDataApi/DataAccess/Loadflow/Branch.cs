using System.Text.Json.Serialization;
using Microsoft.Extensions.ObjectPool;
using MySqlX.XDevAPI;
using NHibernate.Classic;
using NHibernate.Mapping.Attributes;

namespace SmartEnergyLabDataApi.Data
{

    public enum BranchType {Other, HVDC, OHL, Cable, Composite, Transformer, QB, SSSC, SeriesCapacitor, SeriesReactor}

    [Class(0, Table = "loadflow_branches")]
    public class Branch : IId, IDataset, ILifecycle
    {
        public Branch()
        {

        }

        public Branch(Dataset dataset)
        {
            Dataset = dataset;
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
        public virtual double? B {get; set;}

        [Property()]
        public virtual double OHL {get; set;}

        [Property()]
        public virtual double CableLength {get; set;}

        [Property()]
        public virtual double Cap {get; set;}

        [Property()]
        public virtual double WinterCap {get; set;}

        [Property()]
        public virtual double SpringCap {get; set;}

        [Property()]
        public virtual double SummerCap {get; set;}

        [Property()]
        public virtual double AutumnCap {get; set;}

        [Property()]
        public virtual string LinkType {get; set;}

        [Property()]
        [Column(Name = "type", Default = "0")]
        public virtual BranchType Type {get; set;}

        public virtual string TypeStr {
            get {
                return Type.ToString();
            }
        }

        [JsonIgnore()]
        [ManyToOne(Column = "Node1Id", Cascade = "none", Fetch = FetchMode.Join)]
        public virtual Node Node1 {get; set;}

        [JsonIgnore()]
        [ManyToOne(Column = "Node2Id", Cascade = "none", Fetch = FetchMode.Join)]
        public virtual Node Node2 {get; set;}

        [JsonIgnore()]
        [ManyToOne(Column = "DatasetId", Cascade = "none")]
        public virtual Dataset Dataset { get; set; }

        [JsonIgnore()]
        [ManyToOne(Column = "CtrlId", Cascade = "all-delete-orphan", Fetch = FetchMode.Join)]
        public virtual Ctrl Ctrl {get; set;}
        public virtual int CtrlId {
            get {
                return Ctrl!=null ? Ctrl.Id : 0;
            }
        }

        public virtual int DatasetId {
            get {
                return Dataset.Id;
            }
        }

        public virtual string LineName {
            get {
                var code = string.IsNullOrEmpty(Code) ? Id.ToString() : Code;
                return $"{Node1.Code}-{Node2.Code}:{code}";
            }
        }

        public virtual string DisplayName {
            get {
                var suffix = string.IsNullOrEmpty(Code) ? "" : $" ({Code})";
                return $"{Node1.Code} <=> {Node2.Code}{suffix}";
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

        public virtual int Node1Voltage {
            get {
                return Node1.Voltage;
            }
        }
        
        public virtual int Node2Voltage {
            get {
                return Node2.Voltage;
            }
        }

        public virtual int Node1LocationId {
            get {
                return Node1.Location!=null ? Node1.Location.Id : 0;
            }
        }
        
        public virtual int Node2LocationId {
            get {
                return Node2.Location!=null ? Node2.Location.Id : 0;
            }
        }

        public virtual GISData Node1GISData {
            get {
                return Node1.Location!=null ? Node1.Location.GISData : null;
            }
        }
        
        public virtual GISData Node2GISData {
            get {
                return Node2.Location!=null ? Node2.Location.GISData : null;
            }
        }

        public virtual int Node1Id {
            get {
                return Node1.Id;
            }
        }
        
        public virtual int Node2Id {
            get {
                return Node2.Id;
            }
        }

        // outaged (bout)
        public virtual bool Outaged {get; set;}

        // Intact planned flow (ipflow)
        public virtual double? PowerFlow {get; set;}

        // Boundary flow
        public virtual double? BFlow {get; set;}

        public virtual double? FreePower {get; set;}

        public virtual LifecycleVeto OnDelete(NHibernate.ISession s)
        {
            // Delete ctrls pointing at this branch
            var ctrls = s.QueryOver<Ctrl>().Where( m=>m.Branch.Id == Id).List();
            foreach( var c in ctrls) {
                s.Delete(c);
            }

            return LifecycleVeto.NoVeto;
        }

        public virtual void OnLoad(NHibernate.ISession s, object id)
        {
        }

        public virtual LifecycleVeto OnSave(NHibernate.ISession s)
        {
            return LifecycleVeto.NoVeto;
        }

        public virtual LifecycleVeto OnUpdate(NHibernate.ISession s)
        {
            return LifecycleVeto.NoVeto;
        }
    }
}