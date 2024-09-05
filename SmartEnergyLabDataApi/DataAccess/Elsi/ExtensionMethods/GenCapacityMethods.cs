namespace SmartEnergyLabDataApi.Data
{
    public static class GenCapacityMethods {
        public static string GetKey(ElsiZone zone, ElsiGenType genType) {
            return $"{zone}:{genType}";
        }

        public static string GetKey(this GenCapacity obj) {
            return GenCapacityMethods.GetKey(obj.Zone,obj.GenType);
        }

        public static bool IsStorage(this ElsiGenType gt) {
            if ( gt == ElsiGenType.Battery || gt == ElsiGenType.HydroPump)  {
                return true;
            } else {
                return false;
            }
        }

        public static double GetCapacity(this GenCapacity obj, ElsiScenario scenario) {
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