using System.ComponentModel;
using NHibernate.Mapping.Attributes;
namespace SmartEnergyLabDataApi.Data.BoundCalc
{
    [ApplicationGroup(ApplicationGroup.BoundCalc)]
    [Class(0, Table = "boundcalc_adjustments")]
    public class BoundCalcAdjustment
    {
        public BoundCalcAdjustment() {

        }
        
        /// <summary>
        /// Database identifier
        /// </summary>
        [Id(0, Name = "Id", Type = "int")]
        [Generator(1, Class = "identity")]
        public virtual int Id { get; set; }

        [Property()]
        public virtual int SourceYear {get; set;}

        [Property()]
        public virtual int TargetYear {get ;set;}

        [Property()]
        public virtual string BranchCode {get; set;}

        [Property()]
        public virtual double Capacity {get; set;}
    }
}