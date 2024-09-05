using System.Text.Json.Serialization;
using NHibernate.Mapping.Attributes;

namespace SmartEnergyLabDataApi.Data
{
    /// <summary>
    /// Generation type
    /// </summary>
    public enum ElsiGenType {Battery,  Biofuels,  Curtail,  Gas,  HardCoal,  HydroPump,  HydroRun,  HydroTurbine,  WindOnShore, WindOffShore, SolarPv, SolarThermal, Lignite,  Nuclear,  Oil,  OtherNonRes,  OtherRes}
    /// <summary>
    /// Generation zone
    /// </summary>
    public enum ElsiZone {BE,  
                            DE,  
                            DKe,  
                            DKw, 
                            FR, 
                            GB_EA, 
                            GB_EA_Dx, 
                            GB_MC, 
                            GB_MC_Dx, 
                            GB_NW, 
                            GB_NW_Dx, 
                            GB_SC, 
                            GB_SC_Dx,
                            GB_SH,
                            GB_SH_Dx, 
                            GB_SP, 
                            GB_SP_Dx, 
                            GB_UN, 
                            GB_UN_Dx, 
                            IE, 
                            NI, 
                            NL, 
                            NO}
    /// <summary>
    /// Main generation zone main (includes distribution connected generators)
    /// </summary>
    public enum ElsiMainZone {BE, DE, DKe, DKw, FR, GB_EA, GB_MC, GB_NW, GB_SC, GB_SH, GB_SP, GB_UN, IE, NI, NL, NO}
    /// <summary>
    /// Generation & demand behaviour (relevant to solar/wind availability)
    /// </summary>
    public enum ElsiProfile {GB, NI, NO, DKe, DKw, NL, BE, FR, DE, IE}
    /// <summary>
    /// NGESO FES scenarios of several years ago
    /// </summary>
    public enum ElsiScenario {CommunityRenewables, TwoDegrees, SteadyProgression, ConsumerEvolution}

    /// <summary>
    /// Type of data stored in elsi_generations table
    /// </summary>
    public enum ElsiGenDataType {SolarAvail, OnShoreAvail, OffShoreAvail, Demands}

    /// <summary>
    /// Periods of day
    /// </summary>
    public enum ElsiPeriod {Pk, // peak 1 or 2 demand hours per day, 
                            Pl, // daytime plateau demand – hours of daytime activity
                            So, // daytime solar peak generation period (reduces plateau demand)
                            Pu, // pick up/drop off period – transition from night trough to daytime plateau 
                            Tr // night time trough demand
                            }

    [Class(0, Table = "elsi_gen_parameters")]
    public class GenParameter : IDataset, IId
    {

        public GenParameter()
        {

        }

        public GenParameter(Dataset dataset) {
            this.Dataset = dataset;
        }

        /// <summary>
        /// Database identifier
        /// </summary>
        [Id(0, Name = "Id", Type = "int")]
        [Generator(1, Class = "identity")]
        public virtual int Id { get; set; }

        /// <summary>
        /// Type of generation
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual ElsiGenType Type {get; set;}

        public virtual string TypeStr {
            get {
                return this.Type.ToString();
            }
        }

        /// <summary>
        /// Thermal efficiency
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual double Efficiency {get; set;}
        
        /// <summary>
        /// Emission rate of fuel (before plant conversion losses) in KG CO2/GJ
        /// </summary>
        /// <value></value>
        [Property()]        
        public virtual double EmissionsRate {get; set;}

        /// <summary>
        /// Forced outage rate days/year
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual double ForcedDays {get; set;}

        /// <summary>
        /// Planned maintenance days/year (assumed summer & spring/autumn)
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual double PlannedDays {get; set;}

        /// <summary>
        /// Maintenance cost Euro/MWh
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual double MaintenanceCost {get; set; }

        /// <summary>
        /// Fuel cost (before plant conversion losses) Euro/GJ
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual double FuelCost {get; set;}

        /// <summary>
        /// Energy needed to warm/start unit in GJ/MW
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual double WarmStart {get; set;}

        /// <summary>
        /// Wear and tear cost of startup in Euro/MW
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual double WearAndTearStart {get; set;}

        /// <summary>
        /// Max hours daily generation from energy storage (Battery and Hydro)
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual double? Endurance {get; set;}

        [JsonIgnore()]
        [ManyToOne(Column = "DatasetId", Cascade = "none")]
        public virtual Dataset Dataset { get; set; }


    }    
}