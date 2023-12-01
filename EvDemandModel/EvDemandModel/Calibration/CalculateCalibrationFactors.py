import pandas as pd
from .ErrorCalculator import ErrorCalculator
from .CalibrationFactorSampleGenerator import CalibrationFactorSampleGenerator
from ..Utils.EVDemandOutput import EVDemandOutput

def calculate(car_van_2011_data: pd.DataFrame, 
              car_van_2021_data: pd.DataFrame, 
              vehicle_registrations_data: pd.DataFrame
    ) -> pd.DataFrame:
    
    relative_error_calculator = ErrorCalculator(
        car_van_2011_data,
        car_van_2021_data,
        vehicle_registrations_data # Actively choosing not to use interpolated data here
    )

    EVDemandOutput.logMessage('Calculating relative errors...')
    relative_error_data = relative_error_calculator.calculate()

    calibration_factor_sample_generator = CalibrationFactorSampleGenerator(
        relative_error_data=relative_error_data,
        n_samples=1000
    )

    EVDemandOutput.logMessage('Generating calibration factor samples...')
    calibration_factor_samples = calibration_factor_sample_generator.generate_samples()

    return calibration_factor_samples