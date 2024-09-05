namespace SmartEnergyLabDataApi.Data
{
    public static class PeakDemandMethods {
        public static string GetKey(ElsiMainZone? mZone, ElsiProfile profile, ElsiScenario? scenario) {
            return $"{mZone}:{profile}:{scenario}";
        }

        public static string GetKey(this PeakDemand obj) {
            return PeakDemandMethods.GetKey(obj.MainZone,obj.Profile,obj.Scenario);
        }

        public static double GetPeakDemand(this PeakDemand obj, ElsiScenario scenario) {
            if ( scenario == ElsiScenario.CommunityRenewables) {
                return obj.CommunityRenewables;
            } else if ( scenario == ElsiScenario.ConsumerEvolution) {
                return obj.ConsumerEvolution;
            } else if ( scenario == ElsiScenario.SteadyProgression) {
                return obj.SteadyProgression;
            } else if ( scenario == ElsiScenario.TwoDegrees) {
                return obj.TwoDegrees;
            } else {
                throw new Exception($"Unexpected ElsiScenario [{scenario}]");
            }
        }

    }
}