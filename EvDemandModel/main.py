#%%
import pandas as pd
import Preprocessing
import AdjustmentFactors
import Utils
import OnPlotParking
import SubstationMapping
from importlib import reload

def reload_modules(module_names):
    for module_name in module_names:
        reload(module_name)

# Reload modules
modules = [Preprocessing, AdjustmentFactors, Utils, OnPlotParking, SubstationMapping]
reload_modules(modules)

from Preprocessing import Preprocess
from AdjustmentFactors import CalculateAdjustmentFactors
from Utils import AdjustmentFactorApplier
from OnPlotParking import CalculateProportionOfVehiclesWithOnPlotParking, CalculateProportionOfEVsWithOnPlotParking
from SubstationMapping import LoadDistributionSubstationData, CreateSubstationObjects, SubstationDataMapper

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

def apply_adjustment_factors(preprocessed_data, adjustment_factors, quarter):
    adoptions = {}
    for vehicle_type in ['vehicle', 'bev', 'phev']:
        adoptions[f'{vehicle_type}'] = AdjustmentFactorApplier.adjust(
            preprocessed_data[f'{vehicle_type}_registrations_i'],
            adjustment_factors,
            quarter
        )
    return adoptions
#%%

if __name__ == "__main__":

    preprocessed_data = Preprocess.preprocess()

    adjustment_factors = CalculateAdjustmentFactors.calculate(
        preprocessed_data['car_van_2011'],
        preprocessed_data['car_van_2021'],
        preprocessed_data['vehicle_registrations']
    )
    #%%

    opp_proportions = calculate_opp_proportions(preprocessed_data, '2023 Q1')

    adoptions = apply_adjustment_factors(preprocessed_data, adjustment_factors, '2023 Q1')

    #%%
    bev_with_opp = adoptions['bev'].mul(opp_proportions['bev']).round(0)
    phev_with_opp = adoptions['phev'].mul(opp_proportions['phev']).round(0)

    # %%
    ds_data = LoadDistributionSubstationData.load_data()

    # %%

    substation_numbers = ds_data['Substation Number'].sample(100).values
    substations = CreateSubstationObjects.create_substation_objects(ds_data, substation_numbers)

    substation_data_mapper = SubstationDataMapper(
        ds_data=ds_data,
        lsoa_boundaries=preprocessed_data['lsoa_boundaries'],
        house_data=preprocessed_data['house_2021']
    )

    data = {
        'vehicles': adoptions['vehicle'], 
        'bevs': adoptions['bev'],
        'phevs': adoptions['phev'],
        'bevsWithOnPlotParking': bev_with_opp,
        'phevsWithOnPlotParking': phev_with_opp
    }

    substation_data_mapper.map_to_substation(substations=substations, data=data)

    index_values = [f"{i}%" for i in range(0, 101, 5)]

    attributes = {
        'vehicles': 'vehicles',
        'bevs': 'bevs',
        'phevs': 'phevs',
        'bevsWithOnPlotParking': 'bevsWithOnPlotParking',
        'phevsWithOnPlotParking': 'phevsWithOnPlotParking'
    }

    substation_vehicle_data = {key: pd.DataFrame(index=index_values, columns=substation_numbers) for key in attributes}

    for df_name, attr in attributes.items():
        for substation in substations:
            substation_vehicle_data[df_name][substation.id] = getattr(substation.vehicles, attr)
    # %%
