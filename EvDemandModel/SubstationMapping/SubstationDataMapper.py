import pandas as pd
import geopandas as gpd
import numpy as np
from scipy.stats import binom
from .CreateSubstationObjects import Vehicles
import warnings
warnings.filterwarnings("ignore", category=UserWarning)

class SubstationDataMapper:
    PERCENTILE_INCREMENT = 5

    def __init__(self, ds_data: pd.DataFrame, lsoa_boundaries: gpd.GeoDataFrame, house_data: pd.DataFrame) -> None:
        self.ds_data = ds_data.set_index('Substation Number')
        self.lsoa_boundaries = lsoa_boundaries
        self.house_data = house_data

    def map_to_substation(self, substations: object, data: dict):
        for substation in substations:
            parent_lsoas, intersections = self._find_parent_lsoas(substation)
            substation.params.parentLSOAs = parent_lsoas
            vehicles_instance = Vehicles()
            for key in data:
                setattr(vehicles_instance, key, self._allocate_data_from_lsoa_to_ds(data[key], substation, parent_lsoas, intersections))
            substation.vehicles = vehicles_instance

    def _find_parent_lsoas(self, substation: object):
        intersections = self.lsoa_boundaries.geometry.intersection(self.ds_data.loc[substation.id].geometry)
        pip_mask = ~intersections.is_empty
        parent_lsoas = self.lsoa_boundaries[pip_mask].index.values
        return parent_lsoas, intersections
    
    def _allocate_data_from_lsoa_to_ds(self, data: pd.DataFrame, substation: object, parent_lsoas: list, intersections):
        # A list to store the resulting binomial samples for all LSOAs
        total_data_from_lsoas = []

        # Calculating household intersection values once for performance reasons
        household_intersection = self._calculate_household_intersection(substation, parent_lsoas, intersections)

        for lsoa in parent_lsoas:
            # n_values is a series of 1000 elements (samples)
            n_values = np.maximum(data[lsoa].astype(int), 0)
            
            # Probability remains the same across all 1000 samples for this LSOA
            p = np.clip(household_intersection.loc[lsoa], 0, 1)
            
            # Generating binomial samples for this LSOA based on n_values and p
            lsoa_samples = binom.rvs(n=n_values, p=p)
            
            # Appending this LSOA's binomial samples to the list
            total_data_from_lsoas.append(lsoa_samples)

        # Summing over the columns (i.e., summing the binomial samples of each LSOA)
        summed_data_from_lsoas = np.sum(total_data_from_lsoas, axis=0)

        # Calculating percentiles for the resulting summed data
        return self._calculate_percentiles(data=summed_data_from_lsoas)

    def _calculate_household_intersection(self, substation: object, parent_lsoas: list, intersections):
        ds_customers_in_lsoas = self._calculate_ds_customers_in_lsoas(substation, parent_lsoas, intersections)
        households = self.house_data.loc[parent_lsoas].households
        return ds_customers_in_lsoas.divide(households)

    def _calculate_ds_customers_in_lsoas(self, substation: object, parent_lsoas: list, intersections):
        relative_intersections = self._calculate_relative_intersection(substation, parent_lsoas, intersections)
        ds_customers_in_lsoas = relative_intersections * substation.params.numCustomers
        ds_customers_in_lsoas = ds_customers_in_lsoas.fillna(0) # If numCustomers == NaN, assume numCustomers = 0
        return ds_customers_in_lsoas # Can sometimes be NaN which causes problems. Need to code an alternative way to calulate based purely on area intersection!

    def _calculate_relative_intersection(self, substation: object, parent_lsoas: list, intersections):
        intersection_areas = intersections.loc[parent_lsoas].area
        substation_area = self.ds_data.loc[substation.id].geometry.area
        return intersection_areas / substation_area

    def _calculate_percentiles(self, data):
        percentiles = [np.percentile(data, p).astype(int) for p in range(0, 101, self.PERCENTILE_INCREMENT)]
        percentile_series = pd.Series(percentiles, index=[f"{i}%" for i in range(0, 101, self.PERCENTILE_INCREMENT)])
        return percentile_series