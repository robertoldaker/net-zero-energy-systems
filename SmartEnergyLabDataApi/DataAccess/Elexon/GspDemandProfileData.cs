using System.Text.Json.Serialization;
using NHibernate.Mapping.Attributes;

namespace SmartEnergyLabDataApi.Data;

// Note not including this as its large and not need to perform elsi calcs
[Class(0, Table = "gsp_demand_profile_data")]
public class GspDemandProfileData {
    /// <summary>
    /// Database identifier
    /// </summary>
    [Id(0, Name = "Id", Type = "int")]
    [Generator(1, Class = "identity")]
    public virtual int Id { get; set; }

    [Property()]
    public virtual string GspId { get; set; }

    [Property()]
    public virtual string GspCode { get; set; }

    [Property()]
    public virtual string GspGroupId { get; set; }

    [Property()]
    public virtual DateTime Date { get; set; }

    [Property(Type = "HaloSoft.DataAccess.DoubleArrayType, DataAccessBase")]
    [Column(SqlType = "Double precision[]", Name = "Demand")]
    public virtual double[] Demand { get; set; }

    [Property(Type = "HaloSoft.DataAccess.BoolArrayType, DataAccessBase")]
    [Column(SqlType = "boolean[]", Name = "IsEstimate")]
    public virtual bool[] IsEstimate { get; set; }

    [ManyToOne(Column = "LocationId", Cascade = "none")]
    public virtual GridSubstationLocation Location { get; set; }


}
