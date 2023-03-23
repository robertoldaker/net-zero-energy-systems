namespace SmartEnergyLabDataApi.Data
{
    public static class GenParamMethods {
        public static double GetEmissionsPerMWh(this GenParameter gp) {
            return gp.EmissionsRate*3.6/gp.Efficiency/1000;
        }

        public static double GetFuelCostPerMWh(this GenParameter gp) {
            return gp.FuelCost*3.6/gp.Efficiency;
        }

        public static double GetVarCostPerMWh(this GenParameter gp) {
            return gp.MaintenanceCost + gp.GetFuelCostPerMWh();
        }

        public static double GetStartCostPerMW(this GenParameter gp) {
            return gp.FuelCost*gp.WarmStart*3.6 + gp.WearAndTearStart;
        }

        public static double Get2ShiftPremium(this GenParameter gp) {
            return gp.GetStartCostPerMW()/14/gp.GetVarCostPerMWh();
        }

        public static double GetWAvail( this GenParameter gp) {
             return 1 - gp.ForcedDays/365.0;
        }

        public static double GetOAvail( this GenParameter gp) {
            return (1 - gp.PlannedDays/(0.75*365)) * gp.GetWAvail();
        }
    }
}
