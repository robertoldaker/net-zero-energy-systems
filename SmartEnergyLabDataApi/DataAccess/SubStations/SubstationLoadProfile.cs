using Google.Apis.Sheets.v4.Data;
using NHibernate.Mapping.Attributes;
using System.Text.Json.Serialization;

namespace SmartEnergyLabDataApi.Data
{
    public enum Day { Saturday, Sunday, Weekday, All}

    public enum Season { Winter, Spring, Summer, Autumn, NotSet }
    public enum LoadProfileSource { LV_Spreadsheet, Tool, EV_Pred, HP_Pred }
    public enum LoadProfileType { Base, EV, HP }

    [Class(0, Table = "substation_load_profiles")]
    public class SubstationLoadProfile
    {
        public SubstationLoadProfile()
        {
            // Ensure this is the default
            Season = Season.NotSet;
        }
        public SubstationLoadProfile(DistributionSubstation distributionSubstation)
        {
            setDistributionSubstation(distributionSubstation);
        }

        [Id(0, Name = "Id", Type = "int")]
        [Generator(1, Class = "identity")]
        public virtual int Id { get; set; }

        [Property()]
        public virtual int IntervalMins { get; set; }

        [Property(NotNull = true)]
        [Column( Name = "Year", Default ="2016")]
        public virtual int Year {get; set;}

        /// <summary>
        /// Number of HPs or EVs (0 if Type=base)
        /// Now set by a call to AdjustForYear and not stored in Db
        /// </summary>
        /// <value></value>
        public virtual double DeviceCount {get; set;}

        /// <summary>
        /// number of month from 1 to 12
        /// </summary>
        [Property()]
        public virtual int MonthNumber { get; set; }

        [Property()]
        public virtual Day Day { get; set; }

        [Property(NotNull = true)]
        [Column( Name = "Type", Default ="0")]
        public virtual LoadProfileType Type {get; set;}

        [Property(NotNull = true)]
        [Column( Name = "Source", Default ="0")]
        public virtual LoadProfileSource Source {get; set;}

        [Property(NotNull = true)]
        [Column( Name = "Season", Default ="4")]
        public virtual Season Season {get; set;}

        [Property(NotNull = true)]
        [Column( Name = "IsDummy", Default ="false")]
        public virtual bool IsDummy {get; set;}

        [Property(Type="HaloSoft.DataAccess.DoubleArrayType, DataAccessBase")]
        [Column(SqlType = "Double precision[]", Name = "ScalingFactors")]
        public virtual double[] ScalingFactors { get; set; }

        [Property(Type="HaloSoft.DataAccess.DoubleArrayType, DataAccessBase")]
        [Column(SqlType = "Double precision[]", Name = "Data")]
        public virtual double[] Data { get; set; }

        public virtual double[] Carbon {get; set;}

        public virtual double[] Cost { get; set; }

        [JsonIgnore]
        [ManyToOne(Column = "DistributionSubstationId", Cascade = "none")]
        public virtual DistributionSubstation DistributionSubstation { get; set; }

        public virtual void setDistributionSubstation(DistributionSubstation dss) {
            DistributionSubstation = dss;
            PrimarySubstation = dss.PrimarySubstation;
            GeographicalArea = dss.PrimarySubstation.GeographicalArea;
            GridSupplyPoint = dss.PrimarySubstation.GridSupplyPoint;
        }

        [JsonIgnore]
        [ManyToOne(Column = "PrimarySubstationId", Cascade = "none")]
        public virtual PrimarySubstation PrimarySubstation { get; set; }

        [JsonIgnore]
        [ManyToOne(Column = "GeographicalAreaId", Cascade = "none")]
        public virtual GeographicalArea GeographicalArea { get; set; }

        [JsonIgnore]
        [ManyToOne(Column = "GridSupplyPointId", Cascade = "none")]
        public virtual GridSupplyPoint GridSupplyPoint { get; set; }

        public virtual SubstationLoadProfile Copy(DistributionSubstation dss) {
            var lp = new SubstationLoadProfile(dss);
            //
            lp.IntervalMins = this.IntervalMins;
            lp.Data = this.Data;
            lp.ScalingFactors = this.ScalingFactors;
            lp.Day = this.Day;
            lp.DeviceCount = this.DeviceCount;
            lp.MonthNumber = this.MonthNumber;
            lp.Source = this.Source;
            lp.Type = this.Type;
            lp.Year = this.Year;
            lp.Season = this.Season;
            //
            return lp;
        }

        public virtual void AdjustForYear(int year) {
            if ( this.DeviceCount!=0) {
                throw new Exception($"AdjustForYear has already been called for this load profile [{this.Id}]");
            }
            int offset = year - this.Year;
            if ( offset<0) {
                throw new Exception($"Incorrect year for scaling load profile [{year}], offset < 0");
            } else if (this.ScalingFactors==null) {
                throw new Exception($"Null scaling factors attempting to adjust load profile [{year}]");
            } else if (offset>=this.ScalingFactors.Length) {
                throw new Exception($"Incorrect year for scaling load profile [{year}], offset[{offset}] >= length of scaling factors[{this.ScalingFactors.Length}]");
            }
            // Scale data by the device count
            double sf = this.ScalingFactors[offset];
            for( int i=0;i<this.Data.Length;i++) {
                this.Data[i] = this.Data[i]*sf;
            }
            // set device count
            this.DeviceCount=sf;
        }
    }
}
