import pandas as pd
import numpy as np

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