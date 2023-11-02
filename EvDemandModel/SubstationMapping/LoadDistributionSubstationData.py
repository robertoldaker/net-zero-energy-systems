import pandas as pd
import geopandas as gpd
import numpy as np
import os

SCRIPT_DIR = os.path.dirname(os.path.realpath(__file__))

def load_data():
    ds = (
        pd.read_csv(os.path.join(SCRIPT_DIR, '../Data/DistributionNetwork/distribution_substations.csv'))
        .drop(
            columns = [
                'Transformer Headroom', 'LCT Count Total', 'Energy Storage', 'Heat Pumps', 
                'Total LCT Capacity', 'Total Generation Capacity', 'Solar', 
                'Wind', 'Bio Fuels', 'Water Generation', 'Waste Generation',
                'Storage Generation', 'Fossil Fuels', 'Other Generation']
        )
        .replace('Hidden', np.nan)
        .astype({'Customers':'float64', 'Substation Number':'Int64'})
        .astype({'Substation Number': str})
    )

    ds_geo = (
        gpd.read_file(os.path.join(SCRIPT_DIR, '../Data/DistributionNetwork/dist_swest_march2023.gpkg'))
        .rename(columns={'NR':'Substation Number'})
        .dissolve('Substation Number').reset_index()
        .merge(ds, how='left', on ='Substation Number')
        .rename(columns={'Substation Name':'Name'})
        .fillna(value={'Discount':'Unknown'}) # For the "key_on" part of the choropleth map
        .to_crs('EPSG:4326')
    )

    ds_geo['Location'] = gpd.GeoSeries(gpd.points_from_xy(ds_geo.LONGITUDE, ds_geo.LATITUDE, crs="EPSG:4326"))

    return ds_geo