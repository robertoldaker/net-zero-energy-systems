import os

SCRIPT_DIR = os.path.dirname(os.path.realpath(__file__))
FILE_PATHS = {
        'car_van_2011': os.path.join(SCRIPT_DIR, 'Data/Vehicle/CarsAndVans2011.csv'),
        'car_van_2021': os.path.join(SCRIPT_DIR, 'Data/Vehicle/CarsAndVans2021.csv'),
        'vehicle_registrations': os.path.join(SCRIPT_DIR, 'Data/Vehicle/df_VEH0125.csv'),
        'bev_registrations': os.path.join(SCRIPT_DIR, 'Data/Vehicle/df_VEH0145.csv'),
        'phev_registrations': os.path.join(SCRIPT_DIR, 'Data/Vehicle/df_VEH0145.csv'),
        'house_2021': os.path.join(SCRIPT_DIR, 'Data/Demographic/LSOA_households.csv'),
        'accommodation_type_2021': os.path.join(SCRIPT_DIR, 'Data/Demographic/LSOA_accommodation_type.csv'),
        'lsoa_boundaries': os.path.join(SCRIPT_DIR, 'Data/Spatial/LSOA/LSOA_2011_EW_BFC_V3_WGS84/LSOA_2011_EW_BFC_V3_WGS84.shp'),
        'lsoa_lookup': os.path.join(SCRIPT_DIR, 'Data/Spatial/LSOA/LSOA_(2011)_to_LSOA_(2021)_to_Local_Authority_District_(2022)_Lookup_for_England_and_Wales_(Version_2).csv')
    }