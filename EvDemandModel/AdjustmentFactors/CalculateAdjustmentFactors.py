import pandas as pd
from .ErrorCalculator import ErrorCalculator
from .AdjustmentFactorSampleGenerator import AdjustmentFactorSampleGenerator

def calculate(car_van_2011_data: pd.DataFrame, 
              car_van_2021_data: pd.DataFrame, 
              vehicle_registrations_data: pd.DataFrame
    ) -> pd.DataFrame:
    
    relative_error_calculator = ErrorCalculator(
        car_van_2011_data,
        car_van_2021_data,
        vehicle_registrations_data # Actively choosing not to use interpolated data here
    )

    print('Calculating relative errors...')
    relative_error_data = relative_error_calculator.calculate()

    adjustment_factor_sample_generator = AdjustmentFactorSampleGenerator(
        relative_error_data=relative_error_data,
        n_samples=1000
    )

    print('Generating adjustment factor samples...')
    adjustment_factor_samples = adjustment_factor_sample_generator.generate_samples()

    return adjustment_factor_samples