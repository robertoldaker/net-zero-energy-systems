namespace SmartEnergyLabDataApi.Data
{
    public static class SubstationHeatingParamsMethods {
        public static void CopyFieldsFrom(this SubstationHeatingParams obj, SubstationHeatingParams sParams) {
            obj.NumType1HPs = sParams.NumType1HPs;
            obj.NumType2HPs = sParams.NumType2HPs;
            obj.NumType3HPs = sParams.NumType3HPs;
        }
    }
}
