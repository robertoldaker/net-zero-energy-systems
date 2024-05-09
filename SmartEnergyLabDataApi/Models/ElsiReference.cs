using System.ComponentModel;
using System.Text.Json.Serialization;
using Antlr.Runtime;
using Google.Apis.Sheets.v4.Data;
using Google.Protobuf.WellKnownTypes;
using HaloSoft.EventLogger;
using Microsoft.AspNetCore.Mvc.Filters;
using NHibernate.Linq.Clauses;
using NHibernate.Util;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Elsi;
using SmartEnergyLabDataApi.Loadflow;
using static SmartEnergyLabDataApi.Elsi.ElsiDayResult;

namespace SmartEnergyLabDataApi.Models;
public class ElsiReference {

    private string getFilename() {
        string folder = getReferenceFolder();
        return Path.Combine(folder,"Elsi.xlsm");
    }

    private string getReferenceFolder() {
        string folder = Path.Combine(AppFolders.Instance.Uploads,"Elsi","Reference");
        Directory.CreateDirectory(folder);
        return folder;
    }

    public void Load(IFormFile file) {
        string dest = getFilename();

        using ( var fs = new FileStream(dest,FileMode.Create)) {
            using ( var sr=file.OpenReadStream() ) {
                sr.CopyTo(fs);
            }
        }
    }

    private string[] _phases = {"All", "Availabilities","Market","Balance","Balance mechanism"};

    public ElsiErrors Run(int day, double tol, string phase) {
        //
        if ( phase == "A") {
            phase = "Availabilities";
        } else if ( phase == "M") {
            phase = "Market";
        } else if ( phase == "B") {
            phase = "Balance";
        } else if ( phase == "BM") {
            phase = "Balance mechanism";
        }
        //
        if ( !_phases.Contains(phase)) {
            throw new Exception($"Unrecognised phase [{phase}]");
        }
        //
        if ( File.Exists(getFilename())) {
            var m = new ElsiXlsmReader();
            var refResults = m.Load(getFilename());
            //
            ElsiDayResult dayResult;
            ElsiScenario scenario = ElsiScenario.SteadyProgression;
            using ( var da = new DataAccess() ) {
                var datasetInfo = new DatasetInfo(da, 0);
                var data = new ElsiData(da, datasetInfo, scenario);
                var mm = new ModelManager(data,null); 
                dayResult = mm.RunDay(day);
            }
            //
            var errors = createElsiErrors(day, refResults, dayResult, tol, phase);
            return errors;
        } else {
            throw new Exception("Reference spreadsheet has not been loaded. Please load an Elsi reference spreadsheet.");
        }
    }

    private ElsiErrors createElsiErrors(int day, ElsiRefResults refResults, ElsiDayResult dayResult, double tol, string filterPhase ) {
        //
        var errors = new ElsiErrors(day, tol, filterPhase);

        // Availabilities
        string phase = "Availabilities";
        errors.AddDemandErrors(phase,dayResult.Availability.DemandResults, refResults.Availabilities);
        errors.AddGeneratorErrors(phase,dayResult.Availability.GeneratorResults, refResults.Availabilities);
        errors.AddStoreErrors(phase,dayResult.Availability.StoreResults, refResults.Availabilities);
        errors.AddLinkErrors(phase,dayResult.Availability.LinkResults, refResults.Availabilities);


        // Market phase
        phase = "Market";
        errors.AddGeneratorErrors(phase,dayResult.Market.GeneratorResults, refResults.MarketPhase);
        errors.AddStoreErrors(phase,dayResult.Market.StoreResults, refResults.MarketPhase);
        errors.AddLinkErrors(phase,dayResult.Market.LinkResults, refResults.MarketPhase);
        errors.AddMarginalPriceErrors(phase,dayResult.Market.MarginalPrices, refResults.MarketPhase);
        errors.AddStorePriceErrors(phase,dayResult.Market.StorePrices, refResults.MarketPhase);
        errors.AddLinkPriceErrors(phase,dayResult.Market.LinkPrices, refResults.MarketPhase);
        errors.AddZoneEmissionRateErrors(phase,dayResult.Market.ZoneEmissionRates, refResults.MarketPhase);
        errors.AddMiscErrors(phase,dayResult.Market.MiscData, refResults.MarketPhase);

        // Balance phase
        phase = "Balance";
        errors.AddGeneratorErrors(phase,dayResult.Balance.GeneratorResults, refResults.BalancePhase);
        errors.AddStoreErrors(phase,dayResult.Balance.StoreResults, refResults.BalancePhase);
        errors.AddLinkErrors(phase,dayResult.Balance.LinkResults, refResults.BalancePhase);
        errors.AddMarginalPriceErrors(phase,dayResult.Balance.MarginalPrices, refResults.BalancePhase);
        errors.AddStorePriceErrors(phase,dayResult.Balance.StorePrices, refResults.BalancePhase);
        errors.AddLinkPriceErrors(phase,dayResult.Balance.LinkPrices, refResults.BalancePhase);
        errors.AddZoneEmissionRateErrors(phase,dayResult.Balance.ZoneEmissionRates, refResults.BalancePhase);
        errors.AddMiscErrors(phase,dayResult.Balance.MiscData, refResults.BalancePhase);

        // Balance mechanism
        phase = "Balance mechanism";
        errors.AddMarketInfoErrors(phase,dayResult.BalanceMechanism.MarketInfo, refResults.BalanceMechanism);

        return errors;
    }

    private string getVariableName(ElsiGenType genType ) {        
        if ( genType == ElsiGenType.HydroRun) {
            return "Hydro-run";
        } else if ( genType == ElsiGenType.HardCoal) {
            return "Hard coal";
        } else if ( genType == ElsiGenType.HydroPump) {
            return "Hydro-pump";
        } else if ( genType == ElsiGenType.HydroRun) {
            return "Hydro-run";
        } else if ( genType == ElsiGenType.HydroTurbine) {
            return "Hydro-turbine";
        } else if ( genType == ElsiGenType.OtherNonRes) {
            return "Other non-RES";
        } else if ( genType == ElsiGenType.OtherRes) {
            return "Other RES";
        } else if ( genType == ElsiGenType.SolarPv) {
            return "Solar-PV";
        } else if ( genType == ElsiGenType.WindOffShore) {
            return "Wind-off-shore";
        } else if ( genType == ElsiGenType.WindOnShore) {
            return "Wind-on-shore";
        } else {
            return genType.ToString();
        }
    }

    public class ElsiErrors {
        private int _day;
        private double _tol;
        private string _filterPhase;
        private List<ElsiRefError> _allErrors;
        public  bool _showAllErrors;
        public ElsiErrors(int day, double tol, string filterPhase) {
            _allErrors = new List<ElsiRefError>();
            _day = day;
            _tol = tol;
            _filterPhase = filterPhase;
        }

        private IEnumerable<ElsiRefError> getFilterErrors() {
            var errors = _allErrors.Where(m=>true);
            if ( !string.IsNullOrEmpty(_filterPhase) && string.Compare(_filterPhase,"All")!=0 ) {
                errors = errors.Where(m=>string.Compare(m.Phase,_filterPhase,true)==0);
            }
            return errors;
        }

        public ElsiRefError MaxError {
            get {                
                return getFilterErrors().OrderByDescending(m=>m.AbsDiff).First();
            }
        }

        public IEnumerable<ElsiRefError> Errors {
            get {
                var errors = getFilterErrors().Where(m=>m.AbsDiff>_tol);
                return errors;
            }
        }

        private string getVariableName(ElsiGenType genType ) {        
            if ( genType == ElsiGenType.HydroRun) {
                return "Hydro-run";
            } else if ( genType == ElsiGenType.HardCoal) {
                return "Hard coal";
            } else if ( genType == ElsiGenType.HydroPump) {
                return "Hydro-pump";
            } else if ( genType == ElsiGenType.HydroRun) {
                return "Hydro-run";
            } else if ( genType == ElsiGenType.HydroTurbine) {
                return "Hydro-turbine";
            } else if ( genType == ElsiGenType.OtherNonRes) {
                return "Other non-RES";
            } else if ( genType == ElsiGenType.OtherRes) {
                return "Other RES";
            } else if ( genType == ElsiGenType.SolarPv) {
                return "Solar-PV";
            } else if ( genType == ElsiGenType.WindOffShore) {
                return "Wind-off-shore";
            } else if ( genType == ElsiGenType.WindOnShore) {
                return "Wind-on-shore";
            } else {
                return genType.ToString();
            }
        }
        public void AddDemandErrors( string phase, List<DemandResult> demandResults, Dictionary<string, ElsiRefResults.ElsiRefSection> phaseDict ) {
            var section = "Demands";
            foreach( var dr in demandResults) {
                string name = dr.ZoneName;
                foreach( var period in System.Enum.GetValues<ElsiPeriod>() ) {
                    int index = (int) period;
                    var actual = phaseDict[section].Entries[name].DayValues[_day][index];
                    var calc = dr.Demands[index];
                    //
                    AddError(phase, section, period, name, calc, actual );
                }
            }
        }

        public void AddGeneratorErrors(string phase, List<GeneratorResult> generatorResults, Dictionary<string, ElsiRefResults.ElsiRefSection> phaseDict ) {
            var section = "Generators";
            foreach( var gr in generatorResults) {
                string name = $"{gr.Zone}:{getVariableName(gr.GenType)}";
                // Cost
                var actual = phaseDict[section].Entries[name].Cost;
                var calc = gr.Cost;
                AddError(phase, section, name + ":Cost", calc, actual );

                // Capacity
                actual = phaseDict[section].Entries[name].Capacity;
                calc = gr.Capacity;
                AddError(phase, section, name + ":Capacity", calc, actual );

                foreach( var period in System.Enum.GetValues<ElsiPeriod>() ) {
                    int index = (int) period;
                    actual = phaseDict[section].Entries[name].DayValues[_day][index];
                    calc = gr.Capacities[index];
                    //
                    AddError(phase, section, period, name, calc, actual );
                }
            }
        }

        public void AddStoreErrors(string phase, List<StoreResult> storeResults, Dictionary<string, ElsiRefResults.ElsiRefSection> phaseDict) {
            var section = "Stores";
            foreach( var sr in storeResults) {
                string name = $"{sr.Zone}:{getVariableName(sr.GenType)}";
                name += sr.Mode == ElsiStorageMode.Generation ? "G" : "P";
                // Cost
                var actual = phaseDict[section].Entries[name].Cost;
                var calc = sr.Cost;
                AddError(phase, section, name + ":Cost", calc, actual );

                // Capacity
                actual = phaseDict[section].Entries[name].Capacity;
                calc = sr.Capacity;
                AddError(phase, section, name + ":Capacity", calc, actual );

                foreach( var period in System.Enum.GetValues<ElsiPeriod>() ) {
                    int index = (int) period;
                    actual = phaseDict[section].Entries[name].DayValues[_day][index];
                    calc = sr.Capacities[index];
                    //
                    AddError(phase, section, period, name, calc, actual );
                }
            }
        }

        public void AddLinkErrors(string phase, List<LinkResult> linkResults, Dictionary<string, ElsiRefResults.ElsiRefSection> phaseDict ) {
            var section = "Links";
            foreach( var lr in linkResults) {
                string name = $"{lr.From.Zone}:{lr.Name}";
                
                // From
                // Cost
                var actual = phaseDict[section].Entries[name].Cost;
                var calc = lr.From.Cost;
                AddError(phase, section, name + ":Cost", calc, actual );

                // Capacity
                actual = phaseDict[section].Entries[name].Capacity;
                calc = lr.From.Capacity;
                AddError(phase, section, name + ":Capacity", calc, actual );

                foreach( var period in System.Enum.GetValues<ElsiPeriod>() ) {
                    int index = (int) period;
                    actual = phaseDict[section].Entries[name].DayValues[_day][index];
                    calc = lr.From.Capacities[index];
                    //
                    AddError(phase, section, period, name, calc, actual );
                }
                // To
                name = $"{lr.To.Zone}:{lr.Name}";
                // Cost
                actual = phaseDict[section].Entries[name].Cost;
                calc = lr.To.Cost;
                AddError(phase, section, name + ":Cost", calc, actual );

                // Capacity
                actual = phaseDict[section].Entries[name].Capacity;
                calc = lr.To.Capacity;
                AddError(phase, section, name + ":Capacity", calc, actual );

                foreach( var period in System.Enum.GetValues<ElsiPeriod>() ) {
                    int index = (int) period;
                    actual = phaseDict[section].Entries[name].DayValues[_day][index];
                    calc = lr.To.Capacities[index];
                    //
                    AddError(phase, section, period, name, calc, actual );
                }
            }

        }

        public void AddLinkPriceErrors(string phase, List<LinkPrice> linkPrices, Dictionary<string, ElsiRefResults.ElsiRefSection> phaseDict ) {
            var section = "Links";
            foreach( var lr in linkPrices) {
                string name = $"{lr.From.Zone}:{lr.Name}";
                
                // From
                // Cost
                var actual = phaseDict[section].Entries[name].Cost;
                var calc = lr.From.Cost;
                AddError(phase, section, name + ":Cost", calc, actual );

                // Capacity
                actual = phaseDict[section].Entries[name].Capacity;
                calc = lr.From.Capacity;
                AddError(phase, section, name + ":Capacity", calc, actual );

                foreach( var period in System.Enum.GetValues<ElsiPeriod>() ) {
                    int index = (int) period;
                    actual = phaseDict[section].Entries[name].DayValues[_day][index];
                    calc = lr.From.Prices[index];
                    //
                    AddError(phase, section, period, name, calc, actual );
                }
                // To
                name = $"{lr.To.Zone}:{lr.Name}";
                // Cost
                actual = phaseDict[section].Entries[name].Cost;
                calc = lr.To.Cost;
                AddError(phase, section, name + ":Cost", calc, actual );

                // Capacity
                actual = phaseDict[section].Entries[name].Capacity;
                calc = lr.To.Capacity;
                AddError(phase, section, name + ":Capacity", calc, actual );

                foreach( var period in System.Enum.GetValues<ElsiPeriod>() ) {
                    int index = (int) period;
                    actual = phaseDict[section].Entries[name].DayValues[_day][index];
                    calc = lr.To.Prices[index];
                    //
                    AddError(phase, section, period, name, calc, actual );
                }
            }

        }
        public void AddMarginalPriceErrors( string phase, List<MarginalPrice> marginalPriceResults, Dictionary<string, ElsiRefResults.ElsiRefSection> phaseDict ) {
            var section = "Marginal prices £/MWh";
            foreach( var dr in marginalPriceResults) {
                string name = dr.ZoneName;
                foreach( var period in System.Enum.GetValues<ElsiPeriod>() ) {
                    int index = (int) period;
                    var actual = phaseDict[section].Entries[name].DayValues[_day][index];
                    var calc = dr.Prices[index];
                    //
                    AddError(phase, section, period, name, calc, actual );
                }
            }
        }

        public void AddStorePriceErrors(string phase, List<StorePrice> storePrices, Dictionary<string, ElsiRefResults.ElsiRefSection> phaseDict) {
            var section = "Stores_1";
            foreach( var sr in storePrices) {
                string name = $"{sr.Zone}:{getVariableName(sr.GenType)}";
                name += sr.Mode == ElsiStorageMode.Generation ? "G" : "P";
                // Cost
                var actual = phaseDict[section].Entries[name].Cost;
                var calc = sr.Cost;
                AddError(phase, section, name + ":Cost", calc, actual );

                // Capacity
                actual = phaseDict[section].Entries[name].Capacity;
                calc = sr.Capacity;
                AddError(phase, section, name + ":Capacity", calc, actual );

                foreach( var period in System.Enum.GetValues<ElsiPeriod>() ) {
                    int index = (int) period;
                    actual = phaseDict[section].Entries[name].DayValues[_day][index];
                    calc = sr.Prices[index];
                    //
                    AddError(phase, section, period, name, calc, actual );
                }
            }
        }

        public void AddZoneEmissionRateErrors( string phase, List<ZoneEmissionsRate> zoneEmissionsRateResults, Dictionary<string, ElsiRefResults.ElsiRefSection> phaseDict ) {
            var section = "tCO2/hour";
            foreach( var dr in zoneEmissionsRateResults) {
                string name = dr.ZoneName;
                foreach( var period in System.Enum.GetValues<ElsiPeriod>() ) {
                    int index = (int) period;
                    var actual = phaseDict[section].Entries[name].DayValues[_day][index];
                    var calc = dr.Rates[index];
                    //
                    AddError(phase, section, period, name, calc, actual );
                }
            }
        }

        public void AddMiscErrors( string phase, MiscData miscDataResults, Dictionary<string, ElsiRefResults.ElsiRefSection> phaseDict ) {
            var section = "Misc";
            string name = "Iters";
            foreach( var period in System.Enum.GetValues<ElsiPeriod>() ) {
                int index = (int) period;
                var actual = phaseDict[section].Entries[name].DayValues[_day][index];
                var calc = miscDataResults.Iters[index];
                //
                AddError(phase, section, period, name, calc, actual );
            }
            name = "Production costs";
            foreach( var period in System.Enum.GetValues<ElsiPeriod>() ) {
                int index = (int) period;
                var actual = phaseDict[section].Entries[name].DayValues[_day][index];
                var calc = miscDataResults.ProductionCosts[index];
                //
                AddError(phase, section, period, name, calc, actual );
            }
            name = "£DayErr";
            var a = phaseDict[section].Entries[name].DayValues[_day][0];
            var c = miscDataResults.DayError;
            //
            AddError(phase, section, name, c, a );

        }

        private string getMarketInfoName(BalanceMechanismInfo bi) {
            var name = $"{bi.Zone}:{getVariableName(bi.GenType)}";
            if ( bi.GenType == ElsiGenType.Battery || 
                 bi.GenType == ElsiGenType.HydroPump) {
                name+=bi.StorageMode==ElsiStorageMode.Generation ? "G" : "P";
            }
            return name;
        }

        public void AddMarketInfoErrors( string phase, List<BalanceMechanismMarketInfo> marketInfoResults, Dictionary<string, ElsiRefResults.ElsiRefSection> phaseDict ) {
            foreach( var mi in marketInfoResults) {
                var marketName = mi.MarketName;
                var section = $"{marketName} BOA";
                foreach( var bi in mi.Info) {
                    var name = getMarketInfoName(bi);
                    if ( phaseDict[section].Entries.ContainsKey(name)) {
                        foreach( var period in System.Enum.GetValues<ElsiPeriod>() ) {
                            int index = (int) period;
                            var actual = phaseDict[section].Entries[name].DayValues[_day][index];
                            var calc = bi.BOA[index];
                            //
                            AddError(phase, section, period, name, calc, actual );
                        }
                    } else {
                        Logger.Instance.LogInfoEvent($"Entry [{name}] does not exist in ref market info [{section}]");
                    }
                }
                var nm = "Loss change";
                foreach( var period in System.Enum.GetValues<ElsiPeriod>() ) {
                    int index = (int) period;
                    var actual = phaseDict[section].Entries[nm].DayValues[_day][index];
                    var calc = mi.LossChange[index];
                    //
                    AddError(phase, section, period, nm, calc, actual );
                }                    
                section = $"{marketName} Costs";
                foreach( var bi in mi.Info) {
                    var name = getMarketInfoName(bi);
                    if ( phaseDict[section].Entries.ContainsKey(name)) {
                        foreach( var period in System.Enum.GetValues<ElsiPeriod>() ) {
                            int index = (int) period;
                            var actual = phaseDict[section].Entries[name].DayValues[_day][index];
                            var calc = bi.Costs[index];
                            AddError(phase, section, period, name, calc, actual );
                        }                    
                    } else {
                        Logger.Instance.LogInfoEvent($"Entry [{name}] does not exist in ref market info [{section}]");
                    }
                }
                nm = $"{marketName} Total";;
                foreach( var period in System.Enum.GetValues<ElsiPeriod>() ) {
                    int index = (int) period;
                    var actual = phaseDict[section].Entries[nm].DayValues[_day][index];
                    var calc = mi.Total[index];
                    //
                    AddError(phase, section, period, nm, calc, actual );
                }                    
            }
        }

        public void AddError(string phase, string section, ElsiPeriod period, string var , double? calc, double? r) {
            _allErrors.Add( new ElsiRefError(phase, section, _day, period, var, calc, r));
        }

        public void AddError(string phase, string section, string var , double? calc, double? r) {
            _allErrors.Add( new ElsiRefError(phase, section, var, calc, r));
        }
    }

    public class ElsiRefError {  
        public ElsiRefError(string phase, string section, int day, ElsiPeriod period,string var, double? calc, double? r) {
            Phase = phase;
            Section =section;
            Day = day;
            Period = period;
            Variable=var;
            Ref = r;
            Calc = calc;
        }
        public ElsiRefError(string phase, string section,string var, double? calc, double? r) {
            Phase = phase;
            Section =section;
            Variable=var;
            Ref = r;
            Calc = calc;
        }
        public string Phase {get; set;}
        public string Section {get; set;}
        public int? Day {get; set;}
        public ElsiPeriod? Period {get; set;}
        public string Variable {get; set;}
        public double? Ref {get; set;}
        public double? Calc {get; set;}
        public double AbsDiff {
            get {
                if ( Ref!=null && Calc!=null) {
                    return Math.Abs((double) Calc - (double) Ref);
                } else if ( Ref!=null) {
                    return Math.Abs((double) Ref);
                } else if ( Calc!=null ) {
                    return Math.Abs((double) Calc);
                } else {
                    return 0;
                }
            }
        }
    }

}

