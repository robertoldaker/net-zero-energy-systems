using System.Text.Json.Serialization;
using NHibernate.Mapping.Attributes;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace SmartEnergyLabDataApi.Data;

public enum DatasetType {Elsi,Loadflow}

[ValidateNever] // required as using this class in model binding and without it will get errors for any missing fields
[ApplicationGroup(ApplicationGroup.Elsi, ApplicationGroup.Loadflow)]
[Class(0, Table = "data_sets")]
public class Dataset
{
    public Dataset()
    {

    }

    /// <summary>
    /// Database identifier
    /// </summary>
    [Id(0, Name = "Id", Type = "int")]
    [Generator(1, Class = "identity")]
    public virtual int Id { get; set; }

    [Property()]
    public virtual DatasetType Type {get; set;}

    [Property()]
    public virtual string Name {get; set;}

    [ManyToOne(Column = "ParentId", Cascade = "none")]
    public virtual Dataset Parent {get; set;}

    [JsonIgnore]
    [ManyToOne(Column = "UserId", Cascade = "none")]
    public virtual User User {get; set;}

    public virtual bool IsReadOnly {
        get {
            return User==null;
        }
    }

}
