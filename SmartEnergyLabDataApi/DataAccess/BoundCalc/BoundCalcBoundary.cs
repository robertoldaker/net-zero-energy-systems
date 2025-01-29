using System.Text.Json.Serialization;
using MySqlX.XDevAPI;
using NHibernate.Classic;
using NHibernate.Mapping.Attributes;

namespace SmartEnergyLabDataApi.Data.BoundCalc
{
    [ApplicationGroup(ApplicationGroup.BoundCalc)]
    [Class(0, Table = "boundcalc_boundaries")]
    public class BoundCalcBoundary : IId, IDataset, ILifecycle
    {
        public BoundCalcBoundary()
        {

        }

        public BoundCalcBoundary(Dataset dataset)
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
        public virtual string Code {get; set;}

        [JsonIgnore()]
        [ManyToOne(Column = "DatasetId", Cascade = "none")]
        public virtual Dataset Dataset { get; set; }

        public virtual IList<BoundCalcZone> Zones {get; set;}

        public virtual int DatasetId {
            get {
                return Dataset.Id;
            }
        }

        public virtual LifecycleVeto OnDelete(NHibernate.ISession s)
        {
            //
            var bzs = s.QueryOver<BoundCalcBoundaryZone>().Where( m=>m.Boundary == this).List();
            foreach( var bz in bzs) {
                s.Delete(bz);
            }
            //
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