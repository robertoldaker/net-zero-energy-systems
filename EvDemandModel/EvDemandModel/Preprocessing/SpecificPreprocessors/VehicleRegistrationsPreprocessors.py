import pandas as pd
from ..BasePreprocessor import BasePreprocessor
from ..DataContainer import DataContainer
from ...Utils.EVDemandOutput import EVDemandOutput

class VehicleRegistrationsDataPreprocessor(BasePreprocessor):
    
    def preprocess(self) -> pd.DataFrame:
        super().preprocess(dtype=self._apply_dtypes(5, 57), na_values=['[c]', '[x]'])
        self.data_container.data = self._preprocess_by_bodytype('Cars') + self._preprocess_by_bodytype('Other body types')
        self._drop_duplicate_rows_by_index()
        self.data_container.data = self.data_container.data.drop('Miscellaneous')
        self.data_container.data = self.data_container.data.dropna(how='all')
        EVDemandOutput.logMessage('VehicleRegistrationsDataPreprocessor pre-processing complete')
        return self.data_container.data
    
    def _preprocess_by_bodytype(self, bodytype: str) -> pd.DataFrame:
        bodytype_data = self.data_container.data.query("BodyType == '" + bodytype + "' & Keepership == 'Private' & LicenceStatus == 'Licensed'")
        bodytype_data = bodytype_data.drop(columns = ['BodyType', 'Keepership', 'LicenceStatus', 'LSOA11NM'])
        bodytype_data = bodytype_data.set_index('LSOA11CD', drop=True)
        return bodytype_data
    
class EVRegistrationsDataPreprocessor(BasePreprocessor):

    def __init__(self, data_container: DataContainer, fuel_type: str):
        super().__init__(data_container)
        self.data_container = data_container
        self.fuel_type = fuel_type
    
    def preprocess(self) -> pd.DataFrame:
        super().preprocess(dtype=self._apply_dtypes(4, 56), na_values=['[c]', '[x]'])
        self._filter_data()
        self._set_df_index('LSOA11CD', drop=True)
        self._split_by_fuel_type()
        self._drop_duplicate_rows_by_index()
        EVDemandOutput.logMessage('EVRegistrationsDataPreprocessor pre-processing complete')
        return self.data_container.data

    def _filter_data(self):
        self.data_container.data = (
            self.data_container.data
            .query("Keepership == 'Private'")
            .drop(columns=['Keepership', 'LSOA11NM'])
        )
    
    def _split_by_fuel_type(self):
        self.data_container.data = (
            self.data_container.data
            .query("Fuel == '" + self.fuel_type + "' or Fuel.isnull()")
            .drop(columns=["Fuel"])
        )