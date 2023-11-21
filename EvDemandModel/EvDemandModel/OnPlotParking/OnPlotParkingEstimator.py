import pandas as pd
import numpy as np
from scipy.stats import binom

class OnPlotParkingEstimator:

    def __init__(self, accommodation_type_counts: pd.DataFrame, n_samples: int) -> None:
        self.accommodation_type_counts = accommodation_type_counts
        self.n_samples = n_samples
        # dict contains the proportion of houses with OPP in the 2021 English household survey
        self.dict = {
            'end_terraced': 0.505,
            'mid_terraced': 0.338,
            'semi_detached': 0.822,
            'detached': 0.961,
            'converted_flat': 0.289,
            'purpose_built_flat': 0.256
            }
        self.dict['terraced'] = round(0.3765*self.dict['end_terraced'] + 0.6235*self.dict['mid_terraced'], 3)
        del self.dict['end_terraced']
        del self.dict['mid_terraced']
        self.series = pd.Series(self.dict)
        self.opp_samples = None
    
    def estimate(self):
        # Convert series values to a 2D array with matching shape
        probabilities = self.series[self.accommodation_type_counts.columns].values[np.newaxis, :]
        
        # Generate samples for each accommodation type and LSOA using broadcasting
        samples = binom.rvs(n=self.accommodation_type_counts.values.astype(int),
                            p=probabilities,
                            size=(self.n_samples, *self.accommodation_type_counts.shape))
        
        # Sum across accommodation types (axis 2)
        summed_samples = samples.sum(axis=2)
        
        # Convert summed_samples to a DataFrame with appropriate columns and index
        self.opp_samples = pd.DataFrame(summed_samples, columns=self.accommodation_type_counts.index)

        return self.opp_samples