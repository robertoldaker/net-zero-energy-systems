using Microsoft.AspNetCore.Components;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Elsi;

namespace SmartEnergyLabDataApi.Models;
public class ElsiCsvWriter {

    private DataAccess _da;

    public ElsiCsvWriter( DataAccess da) {
        _da = da;
    }

    public MemoryStream WriteToMemoryStream(int datasetId, ElsiScenario scenario) {
        var edrs = _da.Elsi.GetElsiDayResults(datasetId, scenario);
        MemoryStream mms;
        using (var ms = new MemoryStream()) {
            using (var sw = new StreamWriter(ms, System.Text.Encoding.UTF8))
            {
                //
                writeMismatchResults(sw,edrs);
                writeHeader(sw,edrs);
                writeAvailabilities(sw,edrs);
                writeMarketPhase(sw,edrs);
                writeBalancePhase(sw,edrs);
                writeBalanceMechanism(sw,edrs);
                //
                sw.Flush();
                mms = new MemoryStream(ms.ToArray());
            }
        }        
        return mms;
    }

    private void writeMismatchResults(StreamWriter sw, List<ElsiDayResult> edrs) {
        //
        sw.WriteLine();
        //
        sw.Write($"\"mkt mism\",\"\",\"\",");
        foreach( var edr in edrs) {
            foreach( var mmr in edr.Mismatches.Market) {
                sw.Write($"{mmr:E2},");
            }
        }
        sw.WriteLine();
        //
        sw.Write($"\"bal mism\",\"\",\"\",");
        foreach( var edr in edrs) {
            foreach( var mmr in edr.Mismatches.Balance) {
                sw.Write($"{mmr:E2},");
            }
        }
        sw.WriteLine();
        //
        sw.WriteLine();
        sw.WriteLine();
        sw.WriteLine();
        sw.WriteLine();
        sw.WriteLine();
        sw.WriteLine();
    }

    private void writeHeader(StreamWriter sw, List<ElsiDayResult> edrs) {
        sw.Write($"\"Period\",\"\",\"\",");
        foreach( var edr in edrs) {
            foreach( var ep in Enum.GetNames(typeof(ElsiPeriod))) {
                sw.Write($"\"#{edr.Day}{ep}\",");
            }
        }
        sw.WriteLine();
        //
        sw.Write($"\"Hours\",\"\",\"\",");
        foreach( var edr in edrs) {
            foreach( var pr in edr.PeriodResults) {
                sw.Write($"\"{pr.Hours}\",");
            }
        }
        sw.WriteLine();
        //
        sw.Write($"\"Season\",\"\",\"\",");
        foreach( var edr in edrs) {
            foreach( var s in edr.Season) {
                sw.Write($"\"{s}\",");
            }
        }
        sw.WriteLine();
    }

    private void writeAvailabilities(StreamWriter sw, List<ElsiDayResult> edrs) {
        sw.WriteLine();
        sw.WriteLine("\"Availabilities\"");
        sw.WriteLine();

        // Demands
        sw.Write("\"Demands\",,,");
        foreach( var edr in edrs) {
            foreach( var ep in Enum.GetNames(typeof(ElsiPeriod))) {
                sw.Write($"\"#{edr.Day}{ep} Dem\",");
            }
        }
        sw.WriteLine();
        var drs = edrs[0].Availability.DemandResults;
        for ( int i=0;i<drs.Count;i++) {
            var dr = drs[i];
            sw.Write($"\"{dr.ZoneName}\",,,");
            for( int j=0;j<edrs.Count;j++) {
                var demands = edrs[j].Availability.DemandResults[i].Demands;
                foreach( var d in demands) {
                    sw.Write($"{d:F0},");
                }
            }
            sw.WriteLine();
        }
        sw.WriteLine();

        // Generators
        sw.Write("\"Generators\",\"Costs\",\"Capacities\",");
        foreach( var edr in edrs) {
            foreach( var ep in Enum.GetNames(typeof(ElsiPeriod))) {
                sw.Write($"\"#{edr.Day}{ep} Avail\",");
            }
        }
        sw.WriteLine();
        var ges = edrs[0].Availability.GeneratorResults;
        for ( int i=0;i<ges.Count;i++) {
            var ge = ges[i];
            sw.Write($"\"{ge.ZoneName}:{ge.GenTypeName}\",{ge.Cost:F2},{ge.Capacity:F0},");
            for( int j=0;j<edrs.Count;j++) {
                var cap = edrs[j].Availability.GeneratorResults[i].Capacities;
                foreach( var c in cap) {
                    sw.Write($"{c:F0},");
                }
            }
            sw.WriteLine();
        }
        sw.WriteLine();

        // Stores
        sw.Write("\"Stores\",\"Costs\",\"Capacities\",");
        foreach( var edr in edrs) {
            foreach( var ep in Enum.GetNames(typeof(ElsiPeriod))) {
                sw.Write($"\"#{edr.Day}{ep} Avail\",");
            }
        }
        sw.WriteLine();
        var stores = edrs[0].Availability.StoreResults;
        for ( int i=0;i<stores.Count;i++) {
            var store = stores[i];
            sw.Write($"\"{store.ZoneName}:{store.GenTypeName}{store.ModeName.First()}\",{store.Cost:F2},{store.Capacity:F0},");
            for( int j=0;j<edrs.Count;j++) {
                var cap = edrs[j].Availability.StoreResults[i].Capacities;
                foreach( var c in cap) {
                    sw.Write($"{c:F0},");
                }
            }
            sw.WriteLine();
        }
        sw.WriteLine();

        // Links
        sw.Write("\"Links\",\"Costs\",\"Capacities\",");
        foreach( var edr in edrs) {
            foreach( var ep in Enum.GetNames(typeof(ElsiPeriod))) {
                sw.Write($"\"#{edr.Day}{ep} Avail\",");
            }
        }
        sw.WriteLine();
        var links = edrs[0].Availability.LinkResults;
        for ( int i=0;i<links.Count;i++) {
            var link = links[i];
            sw.Write($"\"{link.From.ZoneName}:{link.Name}\",{link.From.Cost:F2},{link.From.Capacity:F0},");
            for( int j=0;j<edrs.Count;j++) {
                var cap = edrs[j].Availability.LinkResults[i].From.Capacities;
                foreach( var c in cap) {
                    sw.Write($"{c:F0},");
                }
            }
            sw.WriteLine();
            sw.Write($"\"{link.To.ZoneName}:{link.Name}\",{link.To.Cost:F2},{link.To.Capacity:F0},");
            for( int j=0;j<edrs.Count;j++) {
                var cap = edrs[j].Availability.LinkResults[i].To.Capacities;
                foreach( var c in cap) {
                    sw.Write($"{c:F0},");
                }
            }
            sw.WriteLine();
        }
        sw.WriteLine();
    }
    private void writeMarketPhase(StreamWriter sw, List<ElsiDayResult> edrs) {
        sw.WriteLine();
        sw.WriteLine("\"Market phase:\"");
        sw.WriteLine();

        // Generators
        sw.Write("\"Generators\",\"Costs\",\"Capacities\",");
        foreach( var edr in edrs) {
            foreach( var ep in Enum.GetNames(typeof(ElsiPeriod))) {
                sw.Write($"\"#{edr.Day}{ep} Mkt\",");
            }
        }
        sw.WriteLine();
        var ges = edrs[0].Market.GeneratorResults;
        for ( int i=0;i<ges.Count;i++) {
            var ge = ges[i];
            sw.Write($"\"{ge.ZoneName}:{ge.GenTypeName}\",{ge.Cost:F2},{ge.Capacity:F0},");
            for( int j=0;j<edrs.Count;j++) {
                var cap = edrs[j].Market.GeneratorResults[i].Capacities;
                foreach( var c in cap) {
                    sw.Write($"{c:F0},");
                }
            }
            sw.WriteLine();
        }
        sw.WriteLine();

        // Stores
        sw.Write("\"Stores\",\"Costs\",\"Capacities\",");
        foreach( var edr in edrs) {
            foreach( var ep in Enum.GetNames(typeof(ElsiPeriod))) {
                sw.Write($"\"#{edr.Day}{ep} Mkt\",");
            }
        }
        sw.WriteLine();
        var stores = edrs[0].Market.StoreResults;
        for ( int i=0;i<stores.Count;i++) {
            var store = stores[i];
            sw.Write($"\"{store.ZoneName}:{store.GenTypeName}{store.ModeName.First()}\",{store.Cost:F2},{store.Capacity:F0},");
            for( int j=0;j<edrs.Count;j++) {
                var cap = edrs[j].Market.StoreResults[i].Capacities;
                foreach( var c in cap) {
                    sw.Write($"{c:F0},");
                }
            }
            sw.WriteLine();
        }
        sw.WriteLine();

        // Links
        sw.Write("\"Links\",\"Costs\",\"Capacities\",");
        foreach( var edr in edrs) {
            foreach( var ep in Enum.GetNames(typeof(ElsiPeriod))) {
                sw.Write($"\"#{edr.Day}{ep} Mkt\",");
            }
        }
        sw.WriteLine();
        var links = edrs[0].Market.LinkResults;
        for ( int i=0;i<links.Count;i++) {
            var link = links[i];
            sw.Write($"\"{link.From.ZoneName}:{link.Name}\",{link.From.Cost:F2},{link.From.Capacity:F0},");
            for( int j=0;j<edrs.Count;j++) {
                var cap = edrs[j].Market.LinkResults[i].From.Capacities;
                foreach( var c in cap) {
                    sw.Write($"{c:F0},");
                }
            }
            sw.WriteLine();
            sw.Write($"\"{link.To.ZoneName}:{link.Name}\",{link.To.Cost:F2},{link.To.Capacity:F0},");
            for( int j=0;j<edrs.Count;j++) {
                var cap = edrs[j].Market.LinkResults[i].To.Capacities;
                foreach( var c in cap) {
                    sw.Write($"{c:F0},");
                }
            }
            sw.WriteLine();
        }
        sw.WriteLine();

        // Marginal prices
         sw.Write("\"Marginal prices £/MWh\",,,");
        foreach( var edr in edrs) {
            foreach( var ep in Enum.GetNames(typeof(ElsiPeriod))) {
                sw.Write($"\"#{edr.Day}{ep} Mkt\",");
            }
        }        
        sw.WriteLine();       
        var mps = edrs[0].Market.MarginalPrices;
        for ( int i=0;i<mps.Count;i++) {
            var mp = mps[i];
            sw.Write($"\"{mp.ZoneName}\",,,");
            for( int j=0;j<edrs.Count;j++) {
                var prices = edrs[j].Market.MarginalPrices[i].Prices;
                foreach( var p in prices) {
                    sw.Write($"{p:F2},");
                }
            }
            sw.WriteLine();
        }
        sw.WriteLine();

         // Stores
        sw.Write("\"Stores\",\"Costs\",\"Capacities\",");
        foreach( var edr in edrs) {
            foreach( var ep in Enum.GetNames(typeof(ElsiPeriod))) {
                sw.Write($"\"#{edr.Day}{ep} Mkt\",");
            }
        }
        sw.WriteLine();
        var storePrices = edrs[0].Market.StorePrices;
        for ( int i=0;i<storePrices.Count;i++) {
            var sps = storePrices[i];
            sw.Write($"\"{sps.ZoneName}:{sps.GenTypeName}{sps.ModeName.First()}\",{sps.Cost:F2},{sps.Capacity:F0},");
            for( int j=0;j<edrs.Count;j++) {
                var prices = edrs[j].Market.StorePrices[i].Prices;
                foreach( var p in prices) {
                    sw.Write($"{p:F2},");
                }
            }
            sw.WriteLine();
        }
        sw.WriteLine();

        // Links
        sw.Write("\"Links\",\"Costs\",\"Capacities\",");
        foreach( var edr in edrs) {
            foreach( var ep in Enum.GetNames(typeof(ElsiPeriod))) {
                sw.Write($"\"#{edr.Day}{ep} Mkt\",");
            }
        }
        sw.WriteLine();

        var linkPrices = edrs[0].Market.LinkPrices;
        for ( int i=0;i<linkPrices.Count;i++) {
            var link = linkPrices[i];
            sw.Write($"\"{link.From.ZoneName}:{link.Name}\",{link.From.Cost:F2},{link.From.Capacity:F0},");
            for( int j=0;j<edrs.Count;j++) {
                var prices = edrs[j].Market.LinkPrices[i].From.Prices;
                foreach( var p in prices) {
                    sw.Write($"{p:F2},");
                }
            }
            sw.WriteLine();
            sw.Write($"\"{link.To.ZoneName}:{link.Name}\",{link.To.Cost:F2},{link.To.Capacity:F0},");
            for( int j=0;j<edrs.Count;j++) {
                var prices = edrs[j].Market.LinkPrices[i].To.Prices;
                foreach( var p in prices) {
                    sw.Write($"{p:F2},");
                }
            }
            sw.WriteLine();
        }
        sw.WriteLine();

        // tonnes Co2 per hour
         sw.Write("\"tCO2/hour\",,,");
        foreach( var edr in edrs) {
            foreach( var ep in Enum.GetNames(typeof(ElsiPeriod))) {
                sw.Write($"\"#{edr.Day}{ep} Mkt\",");
            }
        }        
        sw.WriteLine();       
        var zer = edrs[0].Market.ZoneEmissionRates;
        for ( int i=0;i<zer.Count;i++) {
            var mp = zer[i];
            sw.Write($"\"{mp.ZoneName}\",,,");
            for( int j=0;j<edrs.Count;j++) {
                var rates = edrs[j].Market.ZoneEmissionRates[i].Rates;
                foreach( var r in rates) {
                    sw.Write($"{r:F1},");
                }
            }
            sw.WriteLine();
        }
        sw.WriteLine();

        // misc
         sw.Write("\"Misc\",,,");
        foreach( var edr in edrs) {
            foreach( var ep in Enum.GetNames(typeof(ElsiPeriod))) {
                sw.Write($"\"#{edr.Day}{ep} Mkt\",");
            }
        }        
        sw.WriteLine();       
        sw.Write($"\"Iters\",,,");
        for( int j=0;j<edrs.Count;j++) {
            var iters = edrs[j].Market.MiscData.Iters;
            foreach( var r in iters) {
                sw.Write($"{r:F0},");
            }
        }
        sw.WriteLine();
        sw.Write($"\"Production costs\",,,");
        for( int j=0;j<edrs.Count;j++) {
            var costs = edrs[j].Market.MiscData.ProductionCosts;
            foreach( var r in costs) {
                sw.Write($"{r:F2},");
            }
        }
        sw.WriteLine();
        sw.Write($"\"£DayErr\",,,");
        for( int j=0;j<edrs.Count;j++) {
            var dayError = edrs[j].Market.MiscData.DayError;
            sw.Write($"{dayError:F2},,,,,");
        }
        sw.WriteLine();

    }
    private void writeBalancePhase(StreamWriter sw, List<ElsiDayResult> edrs) {

        sw.WriteLine();
        sw.WriteLine("\"Balance phase:\"");
        sw.WriteLine();

        // Generators
        sw.Write("\"Generators\",\"Costs\",\"Capacities\",");
        foreach( var edr in edrs) {
            foreach( var ep in Enum.GetNames(typeof(ElsiPeriod))) {
                sw.Write($"\"#{edr.Day}{ep} Bal\",");
            }
        }
        sw.WriteLine();
        var ges = edrs[0].Balance.GeneratorResults;
        for ( int i=0;i<ges.Count;i++) {
            var ge = ges[i];
            sw.Write($"\"{ge.ZoneName}:{ge.GenTypeName}\",{ge.Cost:F2},{ge.Capacity:F0},");
            for( int j=0;j<edrs.Count;j++) {
                var cap = edrs[j].Balance.GeneratorResults[i].Capacities;
                foreach( var c in cap) {
                    sw.Write($"{c:F0},");
                }
            }
            sw.WriteLine();
        }
        sw.WriteLine();

        // Stores
        sw.Write("\"Stores\",\"Costs\",\"Capacities\",");
        foreach( var edr in edrs) {
            foreach( var ep in Enum.GetNames(typeof(ElsiPeriod))) {
                sw.Write($"\"#{edr.Day}{ep} Bal\",");
            }
        }
        sw.WriteLine();
        var stores = edrs[0].Balance.StoreResults;
        for ( int i=0;i<stores.Count;i++) {
            var store = stores[i];
            sw.Write($"\"{store.ZoneName}:{store.GenTypeName}{store.ModeName.First()}\",{store.Cost:F2},{store.Capacity:F0},");
            for( int j=0;j<edrs.Count;j++) {
                var cap = edrs[j].Balance.StoreResults[i].Capacities;
                foreach( var c in cap) {
                    sw.Write($"{c:F0},");
                }
            }
            sw.WriteLine();
        }
        sw.WriteLine();

        // Links
        sw.Write("\"Links\",\"Costs\",\"Capacities\",");
        foreach( var edr in edrs) {
            foreach( var ep in Enum.GetNames(typeof(ElsiPeriod))) {
                sw.Write($"\"#{edr.Day}{ep} Bal\",");
            }
        }
        sw.WriteLine();
        var links = edrs[0].Balance.LinkResults;
        for ( int i=0;i<links.Count;i++) {
            var link = links[i];
            sw.Write($"\"{link.From.ZoneName}:{link.Name}\",{link.From.Cost:F2},{link.From.Capacity:F0},");
            for( int j=0;j<edrs.Count;j++) {
                var cap = edrs[j].Balance.LinkResults[i].From.Capacities;
                foreach( var c in cap) {
                    sw.Write($"{c:F0},");
                }
            }
            sw.WriteLine();
            sw.Write($"\"{link.To.ZoneName}:{link.Name}\",{link.To.Cost:F2},{link.To.Capacity:F0},");
            for( int j=0;j<edrs.Count;j++) {
                var cap = edrs[j].Balance.LinkResults[i].To.Capacities;
                foreach( var c in cap) {
                    sw.Write($"{c:F0},");
                }
            }
            sw.WriteLine();
        }
        sw.WriteLine();

        // Marginal prices
         sw.Write("\"Marginal prices £/MWh\",,,");
        foreach( var edr in edrs) {
            foreach( var ep in Enum.GetNames(typeof(ElsiPeriod))) {
                sw.Write($"\"#{edr.Day}{ep} Bal\",");
            }
        }        
        sw.WriteLine();       
        var mps = edrs[0].Balance.MarginalPrices;
        for ( int i=0;i<mps.Count;i++) {
            var mp = mps[i];
            sw.Write($"\"{mp.ZoneName}\",,,");
            for( int j=0;j<edrs.Count;j++) {
                var prices = edrs[j].Balance.MarginalPrices[i].Prices;
                foreach( var p in prices) {
                    sw.Write($"{p:F2},");
                }
            }
            sw.WriteLine();
        }
        sw.WriteLine();

         // Stores
        sw.Write("\"Stores\",\"Costs\",\"Capacities\",");
        foreach( var edr in edrs) {
            foreach( var ep in Enum.GetNames(typeof(ElsiPeriod))) {
                sw.Write($"\"#{edr.Day}{ep} Bal\",");
            }
        }
        sw.WriteLine();
        var storePrices = edrs[0].Balance.StorePrices;
        for ( int i=0;i<storePrices.Count;i++) {
            var sps = storePrices[i];
            sw.Write($"\"{sps.ZoneName}:{sps.GenTypeName}{sps.ModeName.First()}\",{sps.Cost:F2},{sps.Capacity:F0},");
            for( int j=0;j<edrs.Count;j++) {
                var prices = edrs[j].Balance.StorePrices[i].Prices;
                foreach( var p in prices) {
                    sw.Write($"{p:F2},");
                }
            }
            sw.WriteLine();
        }
        sw.WriteLine();

        // Links
        sw.Write("\"Links\",\"Costs\",\"Capacities\",");
        foreach( var edr in edrs) {
            foreach( var ep in Enum.GetNames(typeof(ElsiPeriod))) {
                sw.Write($"\"#{edr.Day}{ep} Bal\",");
            }
        }
        sw.WriteLine();

        var linkPrices = edrs[0].Balance.LinkPrices;
        for ( int i=0;i<linkPrices.Count;i++) {
            var link = linkPrices[i];
            sw.Write($"\"{link.From.ZoneName}:{link.Name}\",{link.From.Cost:F2},{link.From.Capacity:F0},");
            for( int j=0;j<edrs.Count;j++) {
                var prices = edrs[j].Balance.LinkPrices[i].From.Prices;
                foreach( var p in prices) {
                    sw.Write($"{p:F2},");
                }
            }
            sw.WriteLine();
            sw.Write($"\"{link.To.ZoneName}:{link.Name}\",{link.To.Cost:F2},{link.To.Capacity:F0},");
            for( int j=0;j<edrs.Count;j++) {
                var prices = edrs[j].Balance.LinkPrices[i].To.Prices;
                foreach( var p in prices) {
                    sw.Write($"{p:F2},");
                }
            }
            sw.WriteLine();
        }
        sw.WriteLine();

        // tonnes Co2 per hour
         sw.Write("\"tCO2/hour\",,,");
        foreach( var edr in edrs) {
            foreach( var ep in Enum.GetNames(typeof(ElsiPeriod))) {
                sw.Write($"\"#{edr.Day}{ep} Bal\",");
            }
        }        
        sw.WriteLine();       
        var zer = edrs[0].Balance.ZoneEmissionRates;
        for ( int i=0;i<zer.Count;i++) {
            var mp = zer[i];
            sw.Write($"\"{mp.ZoneName}\",,,");
            for( int j=0;j<edrs.Count;j++) {
                var rates = edrs[j].Balance.ZoneEmissionRates[i].Rates;
                foreach( var r in rates) {
                    sw.Write($"{r:F1},");
                }
            }
            sw.WriteLine();
        }
        sw.WriteLine();

        // misc
         sw.Write("\"Misc\",,,");
        foreach( var edr in edrs) {
            foreach( var ep in Enum.GetNames(typeof(ElsiPeriod))) {
                sw.Write($"\"#{edr.Day}{ep} Bal\",");
            }
        }        
        sw.WriteLine();       
        sw.Write($"\"Iters\",,,");
        for( int j=0;j<edrs.Count;j++) {
            var iters = edrs[j].Balance.MiscData.Iters;
            foreach( var r in iters) {
                sw.Write($"{r:F0},");
            }
        }
        sw.WriteLine();
        sw.Write($"\"Production costs\",,,");
        for( int j=0;j<edrs.Count;j++) {
            var costs = edrs[j].Balance.MiscData.ProductionCosts;
            foreach( var r in costs) {
                sw.Write($"{r:F2},");
            }
        }
        sw.WriteLine();
        sw.Write($"\"£DayErr\",,,");
        for( int j=0;j<edrs.Count;j++) {
            var dayError = edrs[j].Balance.MiscData.DayError;
            sw.Write($"{dayError:F2},,,,,");
        }
        sw.WriteLine();

    }
    private void writeBalanceMechanism(StreamWriter sw, List<ElsiDayResult> edrs) {

        sw.WriteLine();
        sw.WriteLine("\"Balance mechanism implied actions:\"");

        var mktInfo = edrs[0].BalanceMechanism.MarketInfo;
        for ( int i=0;i<mktInfo.Count;i++) {
            sw.WriteLine();
            // BOA
            var mi = mktInfo[i];
            sw.Write($"\"{mi.MarketName} BOA\",,,");
            foreach( var edr in edrs) {
                foreach( var ep in Enum.GetNames(typeof(ElsiPeriod))) {
                    sw.Write($"\"#{edr.Day}{ep} BOA\",");
                }
            }
            sw.WriteLine();
            for( int j=0;j<mi.Info.Count;j++) {
                var info = mi.Info[j];
                sw.Write($"\"{info.ZoneName}:{info.GenTypeName}\",,,");
                for( int k=0;k<edrs.Count;k++) {
                    var boa = edrs[k].BalanceMechanism.MarketInfo[i].Info[j].BOA;
                    foreach( var c in boa) {
                        sw.Write($"{c:F0},");
                    }
                }
                sw.WriteLine();
            }

            // Loss change
            sw.WriteLine();
            sw.Write($"\"Loss change\",,,");
            for( int k=0;k<edrs.Count;k++) {
                var lc = edrs[k].BalanceMechanism.MarketInfo[i].LossChange;
                foreach( var c in lc) {
                    sw.Write($"{c:F0},");
                }
            }
            sw.WriteLine();
            sw.WriteLine();

            // Costs
            sw.Write($"\"{mi.MarketName} Costs\",,,");
            foreach( var edr in edrs) {
                foreach( var ep in Enum.GetNames(typeof(ElsiPeriod))) {
                    sw.Write($"\"#{edr.Day}{ep} Costs\",");
                }
            }
            sw.WriteLine();
            for( int j=0;j<mi.Info.Count;j++) {
                var info = mi.Info[j];
                sw.Write($"\"{info.ZoneName}:{info.GenTypeName}\",,,");
                for( int k=0;k<edrs.Count;k++) {
                    var costs = edrs[k].BalanceMechanism.MarketInfo[i].Info[j].Costs;
                    foreach( var c in costs) {
                        sw.Write($"{c:F0},");
                    }
                }
                sw.WriteLine();
            }

            // Total
            sw.WriteLine();
            sw.Write($"\"{mi.MarketName} Total\",,,");
            for( int k=0;k<edrs.Count;k++) {
                var total = edrs[k].BalanceMechanism.MarketInfo[i].Total;
                foreach( var c in total) {
                    sw.Write($"{c:F0},");
                }
            }
            sw.WriteLine();
            sw.WriteLine();

       }

       sw.WriteLine();
    }
}