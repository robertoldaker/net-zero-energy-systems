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

    # Find the common LSOAs
    common_lsoas = ListUtilities.intersection_of_lists(*[processed_data[key].index for key in processed_data])

    # Filter and sort the datasets using the common LSOAs
    for key in processed_data:
        processed_data[key] = processed_data[key].loc[common_lsoas].sort_index()
    
    registration_interpolator = RegistrationInterpolator()

    # The keys for the data that needs interpolation
    registration_keys = ['vehicle_registrations', 'bev_registrations', 'phev_registrations']

    # Interpolate and add the interpolated data with "_i" suffix
    for key in registration_keys:
        interpolated_key = key + "_i"
        processed_data[interpolated_key] = registration_interpolator.interpolate(processed_data[key])

    return processed_data