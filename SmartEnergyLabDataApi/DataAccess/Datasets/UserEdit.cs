using System.Text.Json.Serialization;
using NHibernate.Mapping.Attributes;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace SmartEnergyLabDataApi.Data;
[ValidateNever] // required as using this class in model binding and without it will get errors for any missing fields
[ApplicationGroup(ApplicationGroup.Elsi, ApplicationGroup.BoundCalc)]
[Class(0, Table = "user_edits")]
public class UserEdit
{
    public UserEdit()
    {

    }

    /// <summary>
    /// Database identifier
    /// </summary>
    [Id(0, Name = "Id", Type = "int")]
    [Generator(1, Class = "identity")]
    public virtual int Id { get; set; }

    [Property()]
    public virtual string Key {get; set;}

    [Property()]
    public virtual string TableName {get; set;}

    [Property()]
    public virtual string ColumnName {get; set;}

    [Property()]
    public virtual string Value {get; set;}

    [Property()]
    [Column(Name = "isRowDelete",Default = "false")]
    public virtual bool IsRowDelete {get; set;}

    public virtual int NewDatasetId {get; set; }

    public virtual string PrevValue {get; set; }

    [JsonIgnore()]
    [ManyToOne(Column = "DatasetId", Cascade = "none")]
    public virtual Dataset Dataset {get; set;}


}
