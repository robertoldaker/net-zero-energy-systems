using SmartEnergyLabDataApi.Models;

namespace SmartEnergyLabDataApi.Data
{
    public static class DistributionSubstationMethods {

        public static ClassificationToolInput GetClassificatonToolInput(this DistributionSubstation dss, DataAccess da) {
            var classifications = da.SubstationClassifications.GetSubstationClassifications(dss);
            ClassificationToolInput input = new ClassificationToolInput();
            input.ElexonProfile = classifications.GetElexonProfile();
            dss.SubstationParams.FillClassificationToolInput(input);
            return input;
        }
    }
}