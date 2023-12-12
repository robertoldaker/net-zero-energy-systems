#%%
import pandas as pd
import EvDemandModel.Preprocessing
import EvDemandModel.Calibration
import EvDemandModel.Utils
import EvDemandModel.OnPlotParking
# import EvDemandModel.SubstationMapping
from importlib import reload
import traceback

def reload_modules(module_names):
    for module_name in module_names:
        reload(module_name)

# Reload modules
modules = [EvDemandModel.Preprocessing, EvDemandModel.Calibration, EvDemandModel.Utils, EvDemandModel.OnPlotParking]
reload_modules(modules)

from EvDemandModel.Preprocessing import Preprocess
from EvDemandModel.Calibration import CalculateCalibrationFactors
from EvDemandModel.Utils import CalibrationFactorApplier
from EvDemandModel.OnPlotParking import CalculateProportionOfVehiclesWithOnPlotParking, CalculateProportionOfEVsWithOnPlotParking
# from EvDemandModel.SubstationMapping import LoadDistributionSubstationData, CreateSubstationObjects, SubstationObjectDataMapper

from EvDemandModel.Utils.EVDemandOutput import EVDemandOutput
from EvDemandModel.Utils.EVDemandInput import EVDemandInput

import sys
import time
import json

#%%

def calculate_opp_proportions(preprocessed_data, quarter):
    opp_props = {}
    opp_props['vehicle'] = CalculateProportionOfVehiclesWithOnPlotParking.calculate(
        preprocessed_data['accommodation_type_2021'],
        preprocessed_data['house_2021'],
        preprocessed_data['car_van_2021'],
    )
    
    for vehicle_type in ['bev', 'phev']:
        opp_props[f'{vehicle_type}'] = CalculateProportionOfEVsWithOnPlotParking.calculate(
            opp_props['vehicle'],
            preprocessed_data[f'vehicle_registrations_i'],
            preprocessed_data[f'{vehicle_type}_registrations_i'],
            quarter
        )
    return opp_props

def apply_calibration_factors(preprocessed_data, calibration_factors, quarter):
    adoptions = {}
    for vehicle_type in ['vehicle', 'bev', 'phev']:
        adoptions[f'{vehicle_type}'] = CalibrationFactorApplier.calibrate(
            preprocessed_data[f'{vehicle_type}_registrations_i'],
            calibration_factors,
            quarter
        )
    return adoptions
#%%
import geopandas as gpd
import numpy as np
from shapely.geometry import MultiPolygon, Polygon
from scipy.stats import binom

class EVDemandResult:
    PERCENTILE_STEP = 1

    def __init__(self, lsoa_data_dict: dict, lsoa_boundaries: gpd.GeoDataFrame, house_data: pd.DataFrame) -> None:
        self.lsoa_data_dict = lsoa_data_dict
        self.lsoa_boundaries = lsoa_boundaries
        self.house_data = house_data
        self.data = {}

    def map_data_from_lsoa_to_substation(self, substation_data_list: list):
        for substation_data in substation_data_list:
            # Find intersections between substations and LSOAs
            intersections = self._find_intersections_with_lsoas(substation_data.polygons) # Change to geometry

            # Find Parent LSOAs (LSOA11CD's) based on non-empty intersections
            parent_lsoas = self._find_parent_lsoas(intersections)

            # Calculate the size of the intersections relative to the area of the substation
            substation_relative_intersections = self._calculate_substation_relative_intersections(substation_data.polygons, parent_lsoas, intersections)
            
            # Calculate the size of the intersections relative to the area of the parent LSOAs
            lsoa_relative_intersections = self._calculate_lsoa_relative_intersections(parent_lsoas, intersections)
            
            # Estimate the number of substation customers that are present in each parent LSOA
            substation_customers_in_lsoas = self._calculate_substation_customers_in_lsoas(substation_data.numCustomers, substation_relative_intersections)
            
            # Estimate the number of LSOA households that are supplied by the substation
            intersection_households = self._calculate_intersection_households(parent_lsoas, substation_customers_in_lsoas)

            # For each vehicle_type of interest
            for vehicle_type in self.lsoa_data_dict:

                # Generate samples for the total data inherited by the substation from the parent LSOAs
                total_data_from_lsoas = self._allocate_data_from_lsoa_to_substation(self.lsoa_data_dict[vehicle_type], 
                                                                                    parent_lsoas, 
                                                                                    substation_data.numCustomers, 
                                                                                    intersection_households, 
                                                                                    lsoa_relative_intersections)
                # From the generated samples, calculate the percentiles.
                percentile_data = self._calculate_percentiles(total_data_from_lsoas)

                # Add the percentile data to the .data attribute using the substation_id as the primary key, 
                # Followed by the vehicle_type, and then percentile data
                self._add_result(substation_data.id, vehicle_type, percentile_data)
        return self.data

    def _find_intersections_with_lsoas(self, substation_geometry: Polygon) -> gpd.GeoSeries:
        intersections = self.lsoa_boundaries.geometry.intersection(substation_geometry)
        return intersections
    
    def _find_parent_lsoas(self, intersections: gpd.GeoSeries) -> np.array:
        pip_mask = ~intersections.is_empty
        parent_lsoas = self.lsoa_boundaries[pip_mask].index.values
        return parent_lsoas
    
    def _calculate_substation_relative_intersections(self, substation_geometry: Polygon, parent_lsoas: list, intersections: gpd.GeoSeries) -> pd.Series:
        intersection_areas = intersections.loc[parent_lsoas].area
        substation_area = substation_geometry.area
        substation_relative_intersections = intersection_areas / substation_area
        return substation_relative_intersections
    
    def _calculate_lsoa_relative_intersections(self, parent_lsoas: list, intersections: gpd.GeoSeries) -> pd.Series:
        intersection_areas = intersections.loc[parent_lsoas].area
        lsoa_relative_intersections = intersection_areas / self.lsoa_boundaries.loc[parent_lsoas].geometry.area
        return lsoa_relative_intersections
    
    def _calculate_substation_customers_in_lsoas(self, substation_numCustomers: int, substation_relative_intersections: pd.Series) -> pd.Series:
        # If substation_numCustomers == NaN, assume substation_numCustomers = 0
        substation_customers_in_lsoas = (substation_relative_intersections * substation_numCustomers).fillna(0)
        return substation_customers_in_lsoas
    
    def _calculate_intersection_households(self, parent_lsoas: list, substation_customers_in_lsoas: pd.Series) -> pd.Series:
        # household_intersections is the approximate proportion of LSOA households in each intersection
        households = self.house_data.loc[parent_lsoas].households
        intersection_households = substation_customers_in_lsoas.divide(households)
        return intersection_households
    
    def _allocate_data_from_lsoa_to_substation(self, lsoa_data: pd.DataFrame, parent_lsoas: list, substation_numCustomers: int, intersection_households: pd.Series, lsoa_relative_intersections: gpd.GeoSeries) -> list:
        # A list to store the resulting binomial samples for all LSOAs
        data_from_lsoas = []

        if not np.nan(substation_numCustomers):
            P = intersection_households

        elif np.nan(substation_numCustomers):
            P = lsoa_relative_intersections

            for lsoa in parent_lsoas:
                # n_values is a series of 1000 elements (samples)
                n_values = np.maximum(lsoa_data[lsoa].fillna(0).astype(int), 0)
                
                # Probability remains the same across all 1000 samples for this LSOA
                p = np.clip(P.loc[lsoa], 0, 1)
                
                # Generating binomial samples for this LSOA based on n_values and p
                lsoa_samples = binom.rvs(n=n_values, p=p)
                
                # Appending this LSOA's binomial samples to the list
                data_from_lsoas.append(lsoa_samples)

            # Summing over the columns (i.e., summing the binomial samples of each LSOA)
            total_data_from_lsoas = np.sum(data_from_lsoas, axis=0)

        # Calculating percentiles for the resulting summed data
        return total_data_from_lsoas

    def _calculate_percentiles(self, data: list) -> dict:
        # Calculate percentiles and create a dictionary with the percentile strings as keys
        percentiles_dict = {f"{i}%": int(np.percentile(data, i))
                            for i in range(0, 100+self.PERCENTILE_STEP, self.PERCENTILE_STEP)}
        return percentiles_dict
    
    def _add_result(self, substation_id: int, vehicle_type: str, percentiles: dict) -> None:
        if substation_id not in self.data:
            self.data[substation_id] = {}
        self.data[substation_id][vehicle_type] = percentiles

    def get_result(self, substation_id: int, vehicle_type: str, percentile: str):
        return self.data[substation_id][vehicle_type].get(percentile, None)

    def __str__(self):
        return str(self.data)
#%%

def runPrediction(line:str, lsoa_data_dict: dict, lsoa_boundaries: gpd.GeoDataFrame, house_data: pd.DataFrame):
    try:
        # get EVDemandInput object from the input line
        input = EVDemandInput.fromJson(line)

        EVDemandOutput.logMessage("Start processing input")
        EVDemandOutput.progressTextMessage("Processing input ...")

        #
        # Code to write out the input to check the serialization from c# object to puthon object works
        # (can be removed when we know its working)
        #
        for rd in input.regionData:
            EVDemandOutput.logMessage(f'id=[{rd.id}]')
            EVDemandOutput.logMessage(f'RegionType=[{rd.type}]')
            EVDemandOutput.logMessage(f'NumCustomers=[{rd.numCustomers}]')            
            EVDemandOutput.logMessage(f'Polgon bounds=[{rd.shapely_polygon.bounds}]')            

        #
        # Needs replacing with actual code to perform prediction
        #
        # count=1
        # while count<=2:
        #     EVDemandOutput.progressTextMessage(f"Processing input {count}...")
        #     EVDemandOutput.progressMessage((int) ((count*100.0)/10.0))
        #     time.sleep(1)
        #     count+=1
        result = EVDemandResult(lsoa_data_dict, lsoa_boundaries, house_data).map_data_from_lsoa_to_substation(input.regionData)

        #
        # Dummy object that should be replaced with object holding the results of the prediction (EVDemandResult?)
        #
        # output= {'numRegionData': len(input.regionData)}
        outputJson = json.dumps(result)
        EVDemandOutput.logMessage("Processed input")
        EVDemandOutput.resultMessage(outputJson)# 
    except BaseException as e:
        #
        # Catch any exceptions and write them to the server log        
        #
        lines = traceback.format_exception(e)
        for l in lines:
            EVDemandOutput.errorMessage(l)
        EVDemandOutput.okMessage();

if __name__ == "__main__":

    out=EVDemandOutput()

    preprocessed_data = Preprocess.preprocess()

    calibration_factors = CalculateCalibrationFactors.calculate(
        preprocessed_data['car_van_2011'],
        preprocessed_data['car_van_2021'],
        preprocessed_data['vehicle_registrations']
    )
    #%%

    opp_proportions = calculate_opp_proportions(preprocessed_data, '2023 Q1')    

    adoptions = apply_calibration_factors(preprocessed_data, calibration_factors, '2023 Q1')

    #%%
    bev_with_opp = adoptions['bev'].mul(opp_proportions['bev']).round(0)
    phev_with_opp = adoptions['phev'].mul(opp_proportions['phev']).round(0)

    # %%
    # ds_data = LoadDistributionSubstationData.load_data()

    # %%

    #substation_numbers = ds_data['Substation Number'].sample(10).values
    #substations = CreateSubstationObjects.create_substation_objects(ds_data, substation_numbers)

    # substation_data_mapper = SubstationObjectDataMapper(
    #     ds_data=ds_data,
    #     lsoa_boundaries=preprocessed_data['lsoa_boundaries'],
    #     house_data=preprocessed_data['house_2021']
    # )

    data = {
        'vehicles': adoptions['vehicle'], 
        'bevs': adoptions['bev'],
        'phevs': adoptions['phev'],
        'bevsWithOnPlotParking': bev_with_opp,
        'phevsWithOnPlotParking': phev_with_opp
    }

    # substation_data_mapper.map_to_substation(substations=substations, data=data)

    # index_values = [f"{i}%" for i in range(0, 101, 5)]

    # attributes = {
    #     'vehicles': 'vehicles',
    #     'bevs': 'bevs',
    #     'phevs': 'phevs',
    #     'bevsWithOnPlotParking': 'bevsWithOnPlotParking',
    #     'phevsWithOnPlotParking': 'phevsWithOnPlotParking'
    # }

    # substation_vehicle_data = {key: pd.DataFrame(index=index_values, columns=substation_numbers) for key in attributes}

    # for df_name, attr in attributes.items():
    #     for substation in substations:
    #         substation_vehicle_data[df_name][substation.id] = getattr(substation.vehicles, attr)
    
    #
    # Write OK message so server knows this script is ready and wait for input
    #
    EVDemandOutput.okMessage()
    cont = True
    while(cont):
        # wait for server to  write something to stdin
        line = sys.stdin.readline().strip()        
        if line == 'EXIT:':
            cont=False
        else:
            runPrediction(line, data, preprocessed_data['lsoa_boundaries'], preprocessed_data['house_2021'])

    # %%
