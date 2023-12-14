import pandas as pd
import numpy as np
from .DataContainer import DataContainer
from .SpecificPreprocessors import (CarVan2011DataPreprocessor, CarVan2021DataPreprocessor, 
                                   VehicleRegistrationsDataPreprocessor, EVRegistrationsDataPreprocessor, 
                                   HouseDataPreprocessor, AccommodationTypeDataPreprocessor,
                                   LSOABoundaryDataPreprocessor
)
from .Utilities import ListUtilities, RegistrationInterpolator
from ..config import FILE_PATHS

def preprocess() -> dict:
    # Create Data Containers
    data_containers = {key: DataContainer(file_path=value) for key, value in list(FILE_PATHS.items())[:-1]}
    lsoa_lookup_file = FILE_PATHS['lsoa_lookup']
    
    # Load and preprocess data
    data_preprocessors = {
        'car_van_2011': CarVan2011DataPreprocessor(data_containers['car_van_2011']),
        'car_van_2021': CarVan2021DataPreprocessor(data_containers['car_van_2021'], lsoa_lookup_file),
        'vehicle_registrations': VehicleRegistrationsDataPreprocessor(data_containers['vehicle_registrations']),
        'bev_registrations': EVRegistrationsDataPreprocessor(data_containers['bev_registrations'], fuel_type='Battery electric'),
        'phev_registrations': EVRegistrationsDataPreprocessor(data_containers['phev_registrations'], fuel_type='Plug-in hybrid electric (petrol)'),
        'house_2021': HouseDataPreprocessor(data_containers['house_2021'], lsoa_lookup_file),
        'accommodation_type_2021': AccommodationTypeDataPreprocessor(data_containers['accommodation_type_2021'], lsoa_lookup_file),
        'lsoa_boundaries': LSOABoundaryDataPreprocessor(data_containers['lsoa_boundaries'])
    }

    # Preprocess the data
    processed_data = {key: processor.preprocess() for key, processor in data_preprocessors.items()}

    # # Find the common LSOAs
    # common_lsoas = ListUtilities.intersection_of_lists(*[processed_data[key].index for key in processed_data])

    # # Filter and sort the datasets using the common LSOAs
    # for key in processed_data:
    #     processed_data[key] = processed_data[key].loc[common_lsoas].sort_index()

    # Assuming lsoa_boundaries is your master dataframe
    lsoa_ids = processed_data['lsoa_boundaries'].index

    # Accommodation types (retrieved from the image you've provided)
    accommodation_types = [
        'Detached', 
        'Semi-detached', 
        'Terraced',
        'In a purpose-built block of flats or tenement',
        'Part of a converted or shared house, including bedsits',
        'Part of another converted building, for example, former school, church or warehouse',
        'In a commercial building, for example, in an office building, hotel or over a shop',
        'A caravan or other mobile or temporary structure'
    ]

    # Iterating through the dictionary of dataframes
    for key, df in processed_data.items():
        # Check if this is the accommodation type dataframe
        if key == 'accommodation_type_2021':
            # Find missing LSOA IDs
            missing_lsoa_ids = set(lsoa_ids) - set(df.index.unique())

            # For each missing LSOA ID, create new rows for each accommodation type
            new_rows = []
            for lsoa_id in missing_lsoa_ids:
                for acc_type in accommodation_types:
                    new_row = pd.Series({
                        'LSOA21CD': lsoa_id,  # Assuming LSOA21CD should match the index
                        'accommodation_type': acc_type,
                        'Observation': np.nan
                    }, name=lsoa_id)
                    new_rows.append(new_row)

            # After constructing new_rows with the loop
            if new_rows:  # Check if there are any new rows to add
                new_rows_df = pd.DataFrame(new_rows)
                df = pd.concat([df, new_rows_df], ignore_index=False)
                df.sort_index(inplace=True)  # Sort if needed

                # Update the dictionary
                processed_data[key] = df
            
        else:
            # For other dataframes, simply reindex to align with lsoa_boundaries
            processed_data[key] = df.reindex(lsoa_ids, fill_value=np.nan)

    # Now all dataframes within preprocessed_data are aligned with lsoa_boundaries
    
    registration_interpolator = RegistrationInterpolator()

    # The keys for the data that needs interpolation
    registration_keys = ['vehicle_registrations', 'bev_registrations', 'phev_registrations']

    # Interpolate and add the interpolated data with "_i" suffix
    for key in registration_keys:
        interpolated_key = key + "_i"
        processed_data[interpolated_key] = registration_interpolator.interpolate(processed_data[key])

    return processed_data