#%%
import pandas as pd
import EvDemandModel.Preprocessing
import EvDemandModel.Calibration
import EvDemandModel.Utils
import EvDemandModel.OnPlotParking
import EvDemandModel.SubstationMapping
from importlib import reload

def reload_modules(module_names):
    for module_name in module_names:
        reload(module_name)

# Reload modules
modules = [EvDemandModel.Preprocessing, EvDemandModel.Calibration, EvDemandModel.Utils, EvDemandModel.OnPlotParking, EvDemandModel.SubstationMapping]
reload_modules(modules)

from EvDemandModel.Preprocessing import Preprocess
from EvDemandModel.Calibration import CalculateCalibrationFactors
from EvDemandModel.Utils import CalibrationFactorApplier
from EvDemandModel.OnPlotParking import CalculateProportionOfVehiclesWithOnPlotParking, CalculateProportionOfEVsWithOnPlotParking
from EvDemandModel.SubstationMapping import LoadDistributionSubstationData, CreateSubstationObjects, SubstationObjectDataMapper

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

def runPrediction(line:str):
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
            EVDemandOutput.logMessage(f'NumPolgons=[{len(rd.polygons)}]')            
            for p in rd.polygons:
                EVDemandOutput.logMessage(f'NumPoints=[{len(p.points)}]')            

        #
        # Needs replacing with actual code to perform prediction
        #
        count=1
        while count<=2:
            EVDemandOutput.progressTextMessage(f"Processing input {count}...")
            EVDemandOutput.progressMessage((int) ((count*100.0)/10.0))
            time.sleep(1)
            count+=1

        #
        # Dummy object that should be replaced with object holding the results of the prediction (EVDemandResult?)
        #
        output= {'numRegionData': len(input.regionData)}        
        outputJson = json.dumps(output)
        EVDemandOutput.logMessage("Processed input")
        EVDemandOutput.resultMessage(outputJson)# 
    except BaseException as e:
        #
        # Catch any exceptions and write them to the server log
        
        #
        EVDemandOutput.errorMessage(e.args[0])
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
    ds_data = LoadDistributionSubstationData.load_data()

    # %%

    substation_numbers = ds_data['Substation Number'].sample(10).values
    substations = CreateSubstationObjects.create_substation_objects(ds_data, substation_numbers)

    substation_data_mapper = SubstationObjectDataMapper(
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
            runPrediction(line)

    # %%
