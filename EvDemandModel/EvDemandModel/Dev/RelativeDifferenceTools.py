import pandas as pd
import numpy as np

class RelativeDifferenceCalculator:
    def __init__(
            self, 
            car_van_2011_data: pd.DataFrame, 
            car_van_2021_data: pd.DataFrame, 
            vehicle_registrations_data: pd.DataFrame, 
        ):
        self.car_van_2011_data = car_van_2011_data
        self.car_van_2021_data = car_van_2021_data
        self.vehicle_registrations_data = vehicle_registrations_data.astype(float)
        self.relative_difference_data = None

    def calculate(self) -> pd.DataFrame:
        self._calculate_relative_differences(2011)
        self._calculate_relative_differences(2021)
        self._merge_relative_difference_data()
        return self.relative_difference_data 

    def _calculate_mean_registered_vehicles_for_year(self, year: int) -> None:
        columns = [f"{year} {quarter}" for quarter in ['Q1', 'Q2', 'Q3', 'Q4']]
        df = getattr(self, f'car_van_{year}_data')
        df['registered_vehicles'] = self.vehicle_registrations_data.loc[:, columns].mean(axis=1).round()
    
    def _calculate_absolute_differences(self, year: int) -> None:
        df = getattr(self, f'car_van_{year}_data')
        df['abs_difference'] = df['cars'] - df['registered_vehicles']
    
    def _calculate_relative_differences(self, year: int) -> None:
        self._calculate_mean_registered_vehicles_for_year(year)
        self._calculate_absolute_differences(year)
        df = getattr(self, f'car_van_{year}_data')
        df['relative_difference'] = df['abs_difference'] / df['registered_vehicles']
    
    def _merge_relative_difference_data(self) -> None:
        relative_difference_2011_df = pd.DataFrame({'LSOA11CD': self.car_van_2011_data['relative_difference'].index, 'relative_difference_2011': self.car_van_2011_data['relative_difference'].values})
        relative_difference_2021_df = pd.DataFrame({'LSOA11CD': self.car_van_2021_data['relative_difference'].index, 'relative_difference_2021': self.car_van_2021_data['relative_difference'].values})
        self.relative_difference_data = pd.merge(relative_difference_2011_df, relative_difference_2021_df, how='outer', on='LSOA11CD').set_index('LSOA11CD')
    
class RelativeDifferenceSampleGenerator:
    def __init__(self, relative_difference_data: pd.DataFrame, n_samples: int):
        self.relative_difference_data = relative_difference_data
        self.relative_difference_samples = None
        self.n_samples = n_samples
    
    def generate_samples(self) -> pd.DataFrame:
        self._calculate_relative_difference_mu_and_sigma()
        self._calculate_relative_difference_samples()
        return self.relative_difference_samples

    def _calculate_relative_difference_mu_and_sigma(self) -> None:
        # Priors
        mu_prior = pd.concat([self.relative_difference_data['relative_difference_2011'], self.relative_difference_data['relative_difference_2021']]).mean()
        sigma_prior = pd.concat([self.relative_difference_data['relative_difference_2011'], self.relative_difference_data['relative_difference_2021']]).std()
        var_prior = sigma_prior**2
        precision_prior = 1/var_prior

        # Data
        mu_lsoa = self.relative_difference_data[['relative_difference_2011', 'relative_difference_2021']].mean(axis=1)
        sigma_lsoa = self.relative_difference_data[['relative_difference_2011', 'relative_difference_2021']].std(axis=1)
        var_lsoa = sigma_lsoa**2
        precision_lsoa = 1/var_lsoa

        # Posterior
        mu_post = (2*mu_lsoa*precision_lsoa + mu_prior*precision_prior)/(2*precision_lsoa + precision_prior)
        sigma_post = np.sqrt(1/(2*precision_lsoa + precision_prior))

        self.relative_difference_data['mu_post'] = mu_post
        self.relative_difference_data['sigma_post'] = sigma_post

        # Fill in missing data with mu_prior and sigma_prior (the mean mu and sigma for all LSOAs) if mu_post or sigma_post is NaN
        # This is a result from missing either Census and Registration data in both 2011 and 2021
        self.relative_difference_data.loc[self.relative_difference_data['mu_post'].isna(), 'mu_post'] = mu_lsoa[self.relative_difference_data['mu_post'].isna()]
        self.relative_difference_data.loc[self.relative_difference_data['mu_post'].isna(), 'mu_post'] = mu_prior
        self.relative_difference_data.loc[self.relative_difference_data['sigma_post'].isna(), 'sigma_post'] = sigma_prior

    def _calculate_relative_difference_samples(self) -> None:
        # Initialize an empty DataFrame for samples

        lsoa_list = self.relative_difference_data.index.values
        self.relative_difference_samples = pd.DataFrame(index=np.arange(0, self.n_samples), columns=lsoa_list)

        # Loop over each LSOA to generate samples
        for lsoa in lsoa_list:
            sigma = self.relative_difference_data.sigma_post.loc[lsoa]  # Assuming sigma_post is indexed by LSOA
            mu = self.relative_difference_data.mu_post.loc[lsoa]
            sample_array = np.random.normal(mu, sigma, size=self.n_samples)
            self.relative_difference_samples[lsoa] = sample_array

class VehicleEstimator:
    def __init__(self, relative_difference_samples) -> None:
        self.relative_difference_samples = relative_difference_samples
    
    def estimate(self, registration_data: pd.DataFrame, quarter: str) -> pd.DataFrame:
        adjusted_registration_data = (1 + self.relative_difference_samples).mul(registration_data[quarter]).round(0).astype('Int64')
        return adjusted_registration_data
    
# def main():
#     relative_difference_calculator = RelativeDifferenceCalculator(
#         car_van_2011_data,
#         car_van_2021_data,
#         vehicle_registrations_data
#     )

#     relative_difference_data = relative_difference_calculator.calculate()

#     relative_difference_data.head()

# if __name__ == "__main__":
#     main()