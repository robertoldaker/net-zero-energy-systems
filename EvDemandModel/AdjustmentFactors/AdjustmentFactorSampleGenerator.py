import pandas as pd
import numpy as np

class AdjustmentFactorSampleGenerator:
    def __init__(self, relative_error_data: pd.DataFrame, n_samples: int):
        self.relative_error_data = relative_error_data
        self.relative_error_samples = None
        self.n_samples = n_samples
    
    def generate_samples(self) -> pd.DataFrame:
        self._calculate_relative_error_mu_and_sigma()
        self._calculate_relative_error_samples()
        return self.relative_error_samples

    def _calculate_relative_error_mu_and_sigma(self) -> None:
        # Priors
        mu_prior = pd.concat([self.relative_error_data['relative_error_2011'], self.relative_error_data['relative_error_2021']]).mean()
        sigma_prior = pd.concat([self.relative_error_data['relative_error_2011'], self.relative_error_data['relative_error_2021']]).std()
        var_prior = sigma_prior**2
        precision_prior = 1/var_prior

        # Data
        mu_lsoa = self.relative_error_data[['relative_error_2011', 'relative_error_2021']].mean(axis=1)
        sigma_lsoa = self.relative_error_data[['relative_error_2011', 'relative_error_2021']].std(axis=1)
        var_lsoa = sigma_lsoa**2
        precision_lsoa = 1/var_lsoa

        # Posterior
        mu_post = (2*mu_lsoa*precision_lsoa + mu_prior*precision_prior)/(2*precision_lsoa + precision_prior)
        sigma_post = np.sqrt(1/(2*precision_lsoa + precision_prior))

        self.relative_error_data['mu_post'] = mu_post
        self.relative_error_data['sigma_post'] = sigma_post

        # Fill in missing data with mu_prior and sigma_prior (the mean mu and sigma for all LSOAs) if mu_post or sigma_post is NaN
        # This is a result from missing either Census and Registration data in both 2011 and 2021
        self.relative_error_data.loc[self.relative_error_data['mu_post'].isna(), 'mu_post'] = mu_lsoa[self.relative_error_data['mu_post'].isna()]
        self.relative_error_data.loc[self.relative_error_data['mu_post'].isna(), 'mu_post'] = mu_prior
        self.relative_error_data.loc[self.relative_error_data['sigma_post'].isna(), 'sigma_post'] = sigma_prior

    def _calculate_relative_error_samples(self) -> None:
        # Initialize an empty DataFrame for samples
        lsoa_list = self.relative_error_data.index.values
        self.relative_error_samples = pd.DataFrame(index=np.arange(0, self.n_samples), columns=lsoa_list)

        # Loop over each LSOA to generate samples
        for lsoa in lsoa_list:
            sigma = self.relative_error_data.sigma_post.loc[lsoa]  # Assuming sigma_post is indexed by LSOA
            mu = self.relative_error_data.mu_post.loc[lsoa]
            sample_array = np.random.normal(mu, sigma, size=self.n_samples)
            self.relative_error_samples[lsoa] = sample_array