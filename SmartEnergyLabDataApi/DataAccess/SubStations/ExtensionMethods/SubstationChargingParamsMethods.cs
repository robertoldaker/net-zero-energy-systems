namespace SmartEnergyLabDataApi.Data
{
    public static class SubstationChargingParamsMethods {
        public static void CopyFieldsFrom(this SubstationChargingParams obj, SubstationChargingParams sParams) {
            obj.NumHomeChargers = sParams.NumHomeChargers;
            obj.NumType1EVs = sParams.NumType1EVs;
            obj.NumType2EVs = sParams.NumType2EVs;
            obj.NumType3EVs = sParams.NumType3EVs;
        }
    }
}
