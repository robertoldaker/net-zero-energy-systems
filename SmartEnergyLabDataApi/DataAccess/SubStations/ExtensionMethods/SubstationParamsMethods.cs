using SmartEnergyLabDataApi.Models;

namespace SmartEnergyLabDataApi.Data
{
    public static class SubstationParamsMethods {
        public static void CopyFieldsFrom(this SubstationParams obj, SubstationParams sParams) {
            obj.Mount = sParams.Mount;
            obj.NumberOfFeeders = sParams.NumberOfFeeders;
            obj.PercentageHalfHourlyLoad = sParams.PercentageHalfHourlyLoad;
            obj.PercentageOverhead = sParams.PercentageOverhead;
            obj.PercentIndustrialCustomers = sParams.PercentIndustrialCustomers;
            obj.Rating = sParams.Rating;
            obj.TotalLength = sParams.TotalLength;
        }

        public static void FillClassificationToolInput(this SubstationParams obj, ClassificationToolInput input) {
            input.SubstationMount = (SubstationMountEnum) obj.Mount;
            input.NumberOfFeeders = obj.NumberOfFeeders;
            input.PercentageHalfHourlyLoad = obj.PercentageHalfHourlyLoad;
            input.PercentageOverhead = obj.PercentageOverhead;
            input.PercentIndustrialCustomers = obj.PercentIndustrialCustomers;
            input.TransformerRating = obj.Rating;
            input.TotalLength = obj.TotalLength;
        }

    }
}