using System.Text.Json.Serialization;
using NHibernate.Classic;
using NHibernate.Mapping.Attributes;
using SmartEnergyLabDataApi.BoundCalc;

namespace SmartEnergyLabDataApi.Data.BoundCalc
{
    [ApplicationGroup(ApplicationGroup.BoundCalc)]
    [Class(0, Table = "boundcalc_transport_models")]
    public class GenerationModel : IDatasetIId, ILifecycle
    {
        public GenerationModel()
        {
            Entries = new List<GenerationModelEntry>();
        }

        public GenerationModel(Dataset dataset)
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
        public virtual string Name { get; set; }

        [JsonIgnore()]
        [ManyToOne(Column = "DatasetId", Cascade = "none")]
        public virtual Dataset Dataset { get; set; }

        public virtual int DatasetId
        {
            get
            {
                return this.Dataset.Id;
            }
        }

        [JsonIgnore()]
        public virtual IList<GenerationModelEntry> Entries { get; set; }

        public virtual LifecycleVeto OnDelete(NHibernate.ISession s)
        {
            // Delete entris pointing at this transport model
            var entries = s.QueryOver<GenerationModelEntry>().Where( m=>m.GenerationModel.Id == Id).List();
            foreach( var tme in entries) {
                s.Delete(tme);
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
