import pandas as pd
import geopandas as gpd
from .DataContainer import DataContainer

class BasePreprocessor:

    def __init__(self, data_container: DataContainer) -> None:
        self.data_container = data_container
    
    def preprocess(self, dtype=None, na_values=None, geo=False) -> None:
        if geo==False:
            self._load_csv_to_df(self.data_container, dtype, na_values)
        elif geo==True:
            self._load_geo_to_gdf(self.data_container)

    def _load_csv_to_df(self, container: DataContainer, dtype=None, na_values=None) -> None:
        try:
            container.data = pd.read_csv(container.file_path, dtype=dtype, na_values=na_values)
        except FileNotFoundError as e:
            raise FileNotFoundError(f"File not found: {container.file_path}") from e
    
    def _load_geo_to_gdf(self, container: DataContainer) -> None:
        try:
            container.data = gpd.read_file(container.file_path)
        except FileNotFoundError as e:
            raise FileNotFoundError(f"File not found: {container.file_path}") from e
    
    def _rename_df_columns(self, column_mapper: dict) -> None:
        self.data_container.data = self.data_container.data.rename(columns=column_mapper)

    def _select_df_columns(self, columns: list) -> None:
        self.data_container.data = self.data_container.data[columns]

    def _drop_duplicate_rows(self, subset) -> None:
        self.data_container.data.drop_duplicates(subset=subset, keep='first', inplace=True)
    
    def _drop_duplicate_rows_by_index(self) -> None:
        self.data_container.data = self.data_container.data[~self.data_container.data.index.duplicated(keep='first')]

    def _set_df_index(self, index_name: str, drop: bool) -> None:
        self.data_container.data = self.data_container.data.set_index(index_name, drop=drop)
    
    def _apply_dtypes(self, start_col: int, end_col: int) -> dict:
        dtypes = {i: str for i in range(start_col)} # first 'first' columns as strings
        dtypes.update({i: float for i in range(start_col, end_col)}) # 'last' columns as floats
        return dtypes