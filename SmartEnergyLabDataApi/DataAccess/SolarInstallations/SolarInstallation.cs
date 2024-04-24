using System.Text.Json.Serialization;
using NHibernate.Classic;
using NHibernate.Mapping.Attributes;

namespace SmartEnergyLabDataApi.Data;

[Class(0, Table = "solar_installations")]
public class SolarInstallation
{
    public SolarInstallation() {
    }
    
    public SolarInstallation(int year, DistributionSubstation dss, double lat, double lng) {
        this.Year = year;
        this.DistributionSubstation = dss;
        this.PrimarySubstation = dss.PrimarySubstation;
        this.GridSupplyPoint = dss.GridSupplyPoint;
        this.GISData = new GISData();
        this.GISData.Latitude = lat;
        this.GISData.Longitude = lng;
    }
    
    /// <summary>
    /// Database identifier
    /// </summary>
    [Id(0, Name = "Id", Type = "int")]
    [Generator(1, Class = "identity")]        
    public virtual int Id { get; set; }


    /// <summary>
    /// Year installed
    /// </summary>
    /// <value></value>
    [Property()]
    public virtual int Year {get; set;}

    /// <summary>
    /// Distribution substation its linked to
    /// </summary>
    [JsonIgnore]
    [ManyToOne(Column = "DistributionSubstationId", Cascade = "none")]
    public virtual DistributionSubstation DistributionSubstation { get; set; }

    /// <summary>
    /// Primary substation it is connected to
    /// </summary>
    [JsonIgnore]
    [ManyToOne(Column = "PrimarySubstationId", Cascade = "none")]
    public virtual PrimarySubstation PrimarySubstation { get; set; }

    /// <summary>
    /// Grid supply point its connected to
    /// </summary>
    [JsonIgnore]
    [ManyToOne(Column = "GridSupplyPointId", Cascade = "none")]
    public virtual GridSupplyPoint GridSupplyPoint { get; set; }

    /// <summary>
    /// GIS data
    /// </summary>
    [ManyToOne(Column = "GISDataId", Cascade = "all-delete-orphan", Fetch = FetchMode.Join)]
    public virtual GISData GISData { get; set; }

}
