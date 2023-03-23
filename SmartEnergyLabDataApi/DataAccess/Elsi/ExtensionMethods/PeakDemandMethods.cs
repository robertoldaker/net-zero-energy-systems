namespace SmartEnergyLabDataApi.Data
{
    public static class PeakDemandMethods {
        public static string GetKey(ElsiMainZone? mZone, ElsiProfile profile, ElsiScenario? scenario) {
            return $"{mZone}:{profile}:{scenario}";
        }

        public static string GetKey(this PeakDemand obj) {
            return PeakDemandMethods.GetKey(obj.MainZone,obj.Profile,obj.Scenario);
        }

    }
}