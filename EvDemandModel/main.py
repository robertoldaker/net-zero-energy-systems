#%%
import pandas as pd
import Preprocessing
import AdjustmentFactors
import Utils
import OnPlotParking
import SubstationMapping

from importlib import reload
reload(Preprocessing)
reload(AdjustmentFactors)
reload(Utils)
reload(OnPlotParking)
reload(SubstationMapping)

from Preprocessing import Preprocess
from AdjustmentFactors import CalculateAdjustmentFactors
from Utils import AdjustmentFactorApplier
from OnPlotParking import CalculateProportionOfVehiclesWithOnPlotParking, CalculateProportionOfEVsWithOnPlotParking
from SubstationMapping import LoadDistributionSubstationData, CreateSubstationObjects, SubstationDataMapper

#%%
preprocessed_data = Preprocess.preprocess()

#%%
adjustment_factors = CalculateAdjustmentFactors.calculate(
    preprocessed_data['car_van_2011'],
    preprocessed_data['car_van_2021'],
    preprocessed_data['vehicle_registrations']
)

# %%
proportion_of_vehicles_with_opp = CalculateProportionOfVehiclesWithOnPlotParking.calculate(
    preprocessed_data['accommodation_type_2021'],
    preprocessed_data['house_2021'],
    preprocessed_data['car_van_2021'],
)

#%%
quarter = '2023 Q1'
proportion_of_bevs_with_opp = CalculateProportionOfEVsWithOnPlotParking.calculate(
    proportion_of_vehicles_with_opp,
    preprocessed_data['vehicle_registrations_i'],
    preprocessed_data['bev_registrations_i'],
    quarter
)

proportion_of_phevs_with_opp = CalculateProportionOfEVsWithOnPlotParking.calculate(
    proportion_of_vehicles_with_opp,
    preprocessed_data['vehicle_registrations_i'],
    preprocessed_data['phev_registrations_i'],
    quarter
)

#%%
vehicle_adoption = AdjustmentFactorApplier.adjust(
    preprocessed_data['vehicle_registrations_i'],
    adjustment_factors,
    quarter
)

bev_adoption = AdjustmentFactorApplier.adjust(
    preprocessed_data['bev_registrations_i'],
    adjustment_factors,
    quarter
)

phev_adoption = AdjustmentFactorApplier.adjust(
    preprocessed_data['phev_registrations_i'],
    adjustment_factors,
    quarter
)

#%%
bev_with_opp = bev_adoption.mul(proportion_of_bevs_with_opp).round(0)
phev_with_opp = phev_adoption.mul(proportion_of_phevs_with_opp).round(0)

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
    'vehicles': vehicle_adoption, 
    'bevs': bev_adoption,
    'phevs': phev_adoption,
    'bevsWithOnPlotParking': bev_with_opp,
    'phevsWithOnPlotParking': phev_with_opp
}

substation_data_mapper.map_to_substation(substations=substations, data=data)

# %%
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
