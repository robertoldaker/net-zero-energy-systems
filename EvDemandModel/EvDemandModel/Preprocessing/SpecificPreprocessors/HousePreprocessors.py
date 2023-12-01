import pandas as pd
from ..BasePreprocessor import BasePreprocessor
from ..DataContainer import DataContainer
from ...Utils.EVDemandOutput import EVDemandOutput

class HouseDataPreprocessor(BasePreprocessor):
    def __init__(self, data_container: DataContainer, lsoa_lookup_file_name: str) -> None:
        super().__init__(data_container)
        self.data_container = data_container
        self.lsoa_lookup_file_name = lsoa_lookup_file_name 
    
    def preprocess(self, dtype=None, na_values=None) -> pd.DataFrame:
        super().preprocess(dtype, na_values)
        lsoa_lookup = pd.read_csv(self.lsoa_lookup_file_name)
        self.data_container.data = (
            self.data_container.data
            .rename(columns={'Lower layer Super Output Areas Code':'LSOA21CD', 'Lower layer Super Output Areas':'LSOA21NM', 'Observation':'households'})
            .merge(lsoa_lookup.loc[:, ['LSOA11CD', 'LSOA21CD']], on = 'LSOA21CD', how='outer')
            .drop(columns=['LSOA21NM'])
            .set_index('LSOA11CD')
        )
        self._drop_duplicate_rows_by_index()
        EVDemandOutput.logMessage('HouseDataPreprocessor pre-processing complete')
        return self.data_container.data
    
class AccommodationTypeDataPreprocessor(BasePreprocessor):
    def __init__(self, data_container: DataContainer, lsoa_lookup_file_name: str) -> None:
        super().__init__(data_container)
        self.data_container = data_container
        self.lsoa_lookup_file_name = lsoa_lookup_file_name 
    
    def preprocess(self, dtype=None, na_values=None) -> pd.DataFrame:
        super().preprocess(dtype, na_values)
        lsoa_lookup = pd.read_csv(self.lsoa_lookup_file_name)
        self.data_container.data = (
            self.data_container.data
            .rename(columns={'Lower layer Super Output Areas Code':'LSOA21CD', 'Lower layer Super Output Areas':'LSOA21NM', 'Accommodation type (8 categories)':'accommodation_type'})
            .merge(lsoa_lookup.loc[:, ['LSOA11CD', 'LSOA21CD']], on = 'LSOA21CD', how='outer')
            .drop(columns=['LSOA21NM', 'Accommodation type (8 categories) Code'])
            .set_index('LSOA11CD')
        )
        EVDemandOutput.logMessage('AccommodationTypeDataPreprocessor pre-processing complete')
        return self.data_container.data