import pandas as pd
from ..BasePreprocessor import BasePreprocessor
from ..DataContainer import DataContainer
from ...Utils.EVDemandOutput import EVDemandOutput

class CarVan2011DataPreprocessor(BasePreprocessor):

    def preprocess(self) -> pd.DataFrame:
        super().preprocess()
        self._rename_df_columns({
            'GEO_CODE': 'LSOA11CD',
            'GEO_LABEL': 'LSOA11NM',
            'Car or van availability : Sum of all cars or vans - Unit : Cars or vans': 'cars',
            'Car or van availability : No cars or vans in household - Unit : Households': 'households_without_cars',
            'Car or van availability : Total\ Car or van availability - Unit : Households': 'households'
            }
        )
        self._drop_duplicate_rows('LSOA11CD')
        self._set_df_index('LSOA11CD', drop=True)
        self._select_df_columns(['LSOA11NM', 'cars', 'households', 'households_without_cars'])
        self._drop_duplicate_rows_by_index()
        EVDemandOutput.logMessage('CarVan2011DataPreprocessor pre-processing complete')
        return self.data_container.data
    
class CarVan2021DataPreprocessor(BasePreprocessor):  

    def __init__(self, data_container: DataContainer, lsoa_lookup_file_name: str) -> None:
        super().__init__(data_container)
        self.lsoa_lookup_file_name = lsoa_lookup_file_name

    def preprocess(self) -> pd.DataFrame:
        super().preprocess()
        self._count_number_of_cars()
        self._condense_data()
        self._reindex_data()
        self._drop_duplicate_rows_by_index()
        EVDemandOutput.logMessage('CarVan2021DataPreprocessor pre-processing complete')
        return self.data_container.data

    def _count_number_of_cars(self) -> None:
        self.data_container.data['cars'] = self.data_container.data['Observation'] * self.data_container.data['Car or van availability (5 categories) Code']

    def _condense_data(self) -> None:
        self.data_container.data = (
            pd.DataFrame(index=self.data_container.data['Lower Layer Super Output Areas Code'].unique(), columns=['LSOA21CD', 'LSOA21NM', 'cars', 'houses_without_cars'])
            .assign(
                LSOA21CD=lambda df: df.index, 
                LSOA21NM=self.data_container.data['Lower Layer Super Output Areas'].unique(),
                cars=self.data_container.data.groupby('Lower Layer Super Output Areas Code')['cars'].sum(),
                houses_without_cars=self.data_container.data.loc[self.data_container.data['Car or van availability (5 categories) Code'] == 0, ['Lower Layer Super Output Areas Code', 'Observation']].set_index('Lower Layer Super Output Areas Code')
            )
        )

    def _reindex_data(self) -> None:
        lsoa_lookup = pd.read_csv(self.lsoa_lookup_file_name)
        data = self.data_container.data
        self.data_container.data = (
            data
            .merge(lsoa_lookup[['LSOA11CD', 'LSOA21CD']], on='LSOA21CD', how='right')
            .assign(cars=lambda df: df.groupby('LSOA11CD')['cars'].transform('sum'))
            .drop(columns=['LSOA21CD'])
        )
        self._set_df_index('LSOA11CD', drop=True)