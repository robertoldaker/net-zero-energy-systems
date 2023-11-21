#################
# Load Packages #
#################

# Data and Maths
import pandas as pd
import numpy as np
import os
from dataclasses import dataclass

#######################
# Data Pre-processing #
#######################

@dataclass
class DataContainer:
    file_path: str
    raw_data: pd.DataFrame = None
    data: pd.DataFrame = None

class BasePreprocessor:

    def __init__(self, data_container: DataContainer) -> None:
        self.data_container = data_container
    
    def preprocess(self, keep_raw: bool, dtype=None, na_values=None) -> None:
        self._load_csv_to_df(self.data_container, keep_raw, dtype, na_values)

    def _load_csv_to_df(self, container: DataContainer, keep_raw: bool, dtype=None, na_values=None) -> None:
        try:
            container.data = pd.read_csv(container.file_path, dtype=dtype, na_values=na_values)
            if keep_raw:
                container.raw_data = container.data.copy()
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

class CarVan2011DataPreprocessor(BasePreprocessor):
    def preprocess(self, keep_raw: bool) -> pd.DataFrame:
        super().preprocess(keep_raw)
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
        print('Pre-processing complete')
        return self.data_container.data

class CarVan2021DataPreprocessor(BasePreprocessor):  
    def __init__(self, data_container: DataContainer, lsoa_lookup_file_name: str) -> None:
        super().__init__(data_container)
        self.lsoa_lookup_file_name = lsoa_lookup_file_name 

    def preprocess(self, keep_raw: bool) -> pd.DataFrame:
        super().preprocess(keep_raw)
        self._count_number_of_cars()
        self._condense_data()
        self._reindex_data()
        self._drop_duplicate_rows_by_index()
        print('Pre-processing complete')
        return self.data_container.data

    def _count_number_of_cars(self) -> None:
        self.data_container.raw_data['cars'] = self.data_container.raw_data['Observation'] * self.data_container.raw_data['Car or van availability (5 categories) Code']

    def _condense_data(self) -> None:
        self.data_container.data = (
            pd.DataFrame(index=self.data_container.raw_data['Lower Layer Super Output Areas Code'].unique(), columns=['LSOA21CD', 'LSOA21NM', 'cars', 'houses_without_cars'])
            .assign(
                LSOA21CD=lambda df: df.index, 
                LSOA21NM=self.data_container.raw_data['Lower Layer Super Output Areas'].unique(),
                cars=self.data_container.raw_data.groupby('Lower Layer Super Output Areas Code')['cars'].sum(),
                houses_without_cars=self.data_container.raw_data.loc[self.data_container.raw_data['Car or van availability (5 categories) Code'] == 0, ['Lower Layer Super Output Areas Code', 'Observation']].set_index('Lower Layer Super Output Areas Code')
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

class VehicleRegistrationsDataPreprocessor(BasePreprocessor):
    def __init__(self, data_container: DataContainer):
        super().__init__(data_container)
        self.data_container = data_container
    
    def preprocess(self, keep_raw: bool) -> pd.DataFrame:
        super().preprocess(keep_raw, dtype=self._apply_dtypes(5, 57), na_values=['[c]', '[x]'])
        self.data_container.data = self._preprocess_by_bodytype('Cars') + self._preprocess_by_bodytype('Other body types')
        self._drop_duplicate_rows_by_index()
        self.data_container.data = self.data_container.data.drop('Miscellaneous')
        self.data_container.data = self.data_container.data.dropna(how='all')
        print('Pre-processing complete')
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
    
    def preprocess(self, keep_raw: bool) -> pd.DataFrame:
        super().preprocess(keep_raw, dtype=self._apply_dtypes(4, 56), na_values=['[c]', '[x]'])
        self._filter_data()
        self._set_df_index('LSOA11CD', drop=True)
        self._split_by_fuel_type()
        self._drop_duplicate_rows_by_index()
        print('Pre-processing complete')
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

class HouseDataPreprocessor(BasePreprocessor):
    def __init__(self, data_container: DataContainer, lsoa_lookup_file_name: str) -> None:
        super().__init__(data_container)
        self.lsoa_lookup_file_name = lsoa_lookup_file_name 
    
    def preprocess(self, keep_raw: bool, dtype=None, na_values=None) -> pd.DataFrame:
        super().preprocess(keep_raw, dtype, na_values)
        lsoa_lookup = pd.read_csv(self.lsoa_lookup_file_name)
        self.data_container.data = (
            self.data_container.data
            .rename(columns={'Lower layer Super Output Areas Code':'LSOA21CD', 'Lower layer Super Output Areas':'LSOA21NM', 'Observation':'households'})
            .merge(lsoa_lookup.loc[:, ['LSOA11CD', 'LSOA21CD']], on = 'LSOA21CD', how='outer')
            .drop(columns=['LSOA21NM'])
            .set_index('LSOA11CD')
        )
        self._drop_duplicate_rows_by_index()
        print('Pre-processing complete')
        return self.data_container.data

class AccommodationTypeDataPreprocessor(BasePreprocessor):
    def __init__(self, data_container: DataContainer, lsoa_lookup_file_name: str) -> None:
        super().__init__(data_container)
        self.lsoa_lookup_file_name = lsoa_lookup_file_name 
    
    def preprocess(self, keep_raw: bool, dtype=None, na_values=None) -> pd.DataFrame:
        super().preprocess(keep_raw, dtype, na_values)
        lsoa_lookup = pd.read_csv(self.lsoa_lookup_file_name)
        self.data_container.data = (
            self.data_container.data
            .rename(columns={'Lower layer Super Output Areas Code':'LSOA21CD', 'Lower layer Super Output Areas':'LSOA21NM', 'Accommodation type (8 categories)':'accommodation_type'})
            .merge(lsoa_lookup.loc[:, ['LSOA11CD', 'LSOA21CD']], on = 'LSOA21CD', how='outer')
            .drop(columns=['LSOA21NM', 'Accommodation type (8 categories) Code'])
            .set_index('LSOA11CD')
        )
        print('Pre-processing complete')
        return self.data_container.data

class ListUtilities:

    @staticmethod
    def intersection_of_lists(*args):
        
        # If no lists are provided, return an empty list
        if not args:
            return []

        # Start with the set of the first list
        result_set = set(args[0])

        # Iterate over the remaining lists, updating the result_set
        for lst in args[1:]:
            result_set &= set(lst)

        return list(result_set)

class RegistrationInterpolator:
    def __init__(self, sample_rate=4) -> None:
        self.registration_data = None
        self.sample_rate = sample_rate

    # Interpolates missing registration data
    def interpolate(self, registration_data: pd.DataFrame) -> pd.DataFrame:
        print('Interpolating Data...')
        self.registration_data = registration_data.T.iloc[::-1]
        interpolated_df = self.registration_data.apply(self._interpolate_column, axis=0)
        interpolated_df = interpolated_df.fillna(0)
        interpolated_df = interpolated_df.astype('Int64')
        return interpolated_df.iloc[::-1].T
    
    def _interpolate_column(self, col) -> pd.Series:
        dates = self._calculate_date_range(self._calculate_t0(col), self._calculate_t_present(col))
        mask = ~col.isna().values
        if mask.any():
            xp = dates[mask]
            fp = col[mask]
            x = dates
            interpolated_array = np.round(np.interp(x, xp, fp))
            interpolated_series = pd.Series(data=interpolated_array, index=col.index)
        else:
            interpolated_series = pd.Series(data=np.nan, index=col.index)
        return interpolated_series

    # apply_dtypes converts select columns from str to float values
    def _apply_dtypes(first_col: int, last_col: int) -> dict:
        dtypes = {i: str for i in range(first_col)}  # first 'first' columns as strings
        dtypes.update({i: float for i in range(first_col, last_col)}) # 'last' columns is currently hard coded. Float is needed for NaNs
        return dtypes
    
    def _quarter_to_decimal(self, year: int, quarter: str) -> float:
        quarters = {'Q1': 0, 'Q2': 0.25, 'Q3': 0.5, 'Q4': 0.75}
        return year + quarters.get(quarter, 0)

    def _calculate_t0(self, col) -> float:
        year = int(col.head(1).index[0][:4])
        quarter = col.head(1).index[0][-2:]
        return self._quarter_to_decimal(year, quarter)

    def _calculate_t_present(self, col) -> float:
        year = int(col.tail(1).index[0][:4])
        quarter = col.tail(1).index[0][-2:]
        return self._quarter_to_decimal(year, quarter)

    def _convert_dates_to_numeric(self) -> list:
        dates = []
        for date in self.registration_data.index:
            year = int(date[:4])
            quarter = date[-2:]
            dates.append(self._quarter_to_decimal(year, quarter))
        return dates

    # Returns an array of numeric dates between t0 and t1 at a specified sample rate
    def _calculate_date_range(self, t0: float, t1: float) -> list:
        return np.linspace(t0, t1, int((t1-t0)*self.sample_rate) + 1)

########
# Main #
########

def main():
    # Find script directory
    script_dir = os.path.dirname(os.path.realpath(__file__))

    # Define file paths
    file_paths = {
        'car_van_2011': os.path.join(script_dir, '../Data/Vehicle/CarsAndVans2011.csv'),
        'car_van_2021': os.path.join(script_dir, '../Data/Vehicle/CarsAndVans2021.csv'),
        'vehicle_registrations': os.path.join(script_dir, '../Data/Vehicle/df_VEH0125.csv'),
        'bev_registrations': os.path.join(script_dir, '../Data/Vehicle/df_VEH0145.csv'),
        'phev_registrations': os.path.join(script_dir, '../Data/Vehicle/df_VEH0145.csv'),
        'house_2021': os.path.join(script_dir, '../Data/Demographic/LSOA_households.csv'),
        'accomodation_type_2021': os.path.join(script_dir, '../Data/Demographic/LSOA_accommodation_type.csv')
    }

    # Create Data Containers
    data_containers = {key: DataContainer(file_path=value) for key, value in file_paths.items()}

    # Load and preprocess data
    lsoa_lookup_file = '../Data/Spatial/LSOA/LSOA_(2011)_to_LSOA_(2021)_to_Local_Authority_District_(2022)_Lookup_for_England_and_Wales_(Version_2).csv'

    data_preprocessors = {
        'car_van_2011': CarVan2011DataPreprocessor(data_containers['car_van_2011']),
        'car_van_2021': CarVan2021DataPreprocessor(data_containers['car_van_2021'], os.path.join(script_dir, lsoa_lookup_file)),
        'vehicle_registrations': VehicleRegistrationsDataPreprocessor(data_containers['vehicle_registrations']),
        'bev_registrations': EVRegistrationsDataPreprocessor(data_containers['bev_registrations'], fuel_type='Battery electric'),
        'phev_registrations': EVRegistrationsDataPreprocessor(data_containers['phev_registrations'], fuel_type='Plug-in hybrid electric (petrol)'),
        'house_2021': HouseDataPreprocessor(data_containers['house_2021'], os.path.join(script_dir, lsoa_lookup_file)),
        'accomodation_type_2021': AccommodationTypeDataPreprocessor(data_containers['accomodation_type_2021'], os.path.join(script_dir, lsoa_lookup_file))
    }

    # Get processed data
    keep_raw_values = {
        'car_van_2011': False,
        'car_van_2021': True,
        'vehicle_registrations': False,
        'bev_registrations': True,
        'phev_registrations': True,
        'house_2021': False,
        'accomodation_type_2021': False
    }

    processed_data = {key: processor.preprocess(keep_raw=keep_raw_values[key]) for key, processor in data_preprocessors.items()}

    # Find the common LSOAs
    common_lsoas = ListUtilities.intersection_of_lists(
        *[processed_data[key].index for key in processed_data]
    )

    # Filter and sort the datasets using the common LSOAs
    for key in processed_data:
        processed_data[key] = processed_data[key].loc[common_lsoas].sort_index()

    # Interpolation using RegistrationInterpolator
    registration_interpolator = RegistrationInterpolator()

    # The keys for the data that needs interpolation
    registration_keys = ['vehicle_registrations', 'bev_registrations', 'phev_registrations']

    # Interpolate and add the interpolated data with "_i" suffix
    for key in registration_keys:
        interpolated_key = key + "_i"
        processed_data[interpolated_key] = registration_interpolator.interpolate(processed_data[key])

    return processed_data

if __name__ == "__main__":
    main()