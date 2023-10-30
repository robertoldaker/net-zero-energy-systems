import pandas as pd

class ErrorCalculator:
    def __init__(
            self, 
            car_van_2011_data: pd.DataFrame, 
            car_van_2021_data: pd.DataFrame, 
            vehicle_registrations_data: pd.DataFrame, 
        ):
        self.car_van_2011_data = car_van_2011_data
        self.car_van_2021_data = car_van_2021_data
        self.vehicle_registrations_data = vehicle_registrations_data.astype(float)
        self.relative_error_data = None

    def calculate(self) -> pd.DataFrame:
        self._calculate_relative_errors(2011)
        self._calculate_relative_errors(2021)
        self._merge_relative_error_data()
        return self.relative_error_data 

    def _calculate_mean_registered_vehicles_for_year(self, year: int) -> None:
        columns = [f"{year} {quarter}" for quarter in ['Q1', 'Q2', 'Q3', 'Q4']]
        df = getattr(self, f'car_van_{year}_data')
        df['registered_vehicles'] = self.vehicle_registrations_data.loc[:, columns].mean(axis=1).round()
    
    def _calculate_absolute_errors(self, year: int) -> None:
        df = getattr(self, f'car_van_{year}_data')
        df['abs_error'] = df['cars'] - df['registered_vehicles']
    
    def _calculate_relative_errors(self, year: int) -> None:
        self._calculate_mean_registered_vehicles_for_year(year)
        self._calculate_absolute_errors(year)
        df = getattr(self, f'car_van_{year}_data')
        df['relative_error'] = df['abs_error'] / df['registered_vehicles']
    
    def _merge_relative_error_data(self) -> None:
        relative_error_2011_df = pd.DataFrame({'LSOA11CD': self.car_van_2011_data['relative_error'].index, 'relative_error_2011': self.car_van_2011_data['relative_error'].values})
        relative_error_2021_df = pd.DataFrame({'LSOA11CD': self.car_van_2021_data['relative_error'].index, 'relative_error_2021': self.car_van_2021_data['relative_error'].values})
        self.relative_error_data = pd.merge(relative_error_2011_df, relative_error_2021_df, how='outer', on='LSOA11CD').set_index('LSOA11CD')
