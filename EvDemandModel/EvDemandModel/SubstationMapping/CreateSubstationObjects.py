from enum import Enum
import pandas as pd
import geopandas as gpd

class DistSubstation:
    def __init__(self) -> None:
        self.name:str
        self.id:str # str instead of int (for now)
        self.gisData:GISData
        self.vehicles:Vehicles
        self.evDemands:EVDemands
        self.params:SubstationParams

class SubstationParams:
    def __init__(self) -> None:
        self.numCustomers:int
        self.parentLSOAs:list

class GISData:
    def __init__(self) -> None:
        self.latitude:float
        self.longitude:float
        self.boundaries:gpd.GeoSeries

class Vehicles:
    def __init__(self) -> None:
        self.vehicles:pd.Series
        self.bevs:pd.Series
        self.phevs:pd.Series
        self.bevsWithOnPlotParking:pd.Series
        self.phevsWithOnPlotParking:pd.Series

class EVDemands:
    def __init__(self) -> None:
        self.evDemandList:list[EVDemand]

class Quarter(Enum):
    Q1=0
    Q2=1
    Q3=2
    Q4=3

class EVDemand:
    def __init__(self) -> None:
        self.bev:DataValue
        self.phev:DataValue
        self.year:int
        self.quarter:Quarter

class DataValue:
    def __init__(self) -> None:
        self.mean:float = 0
        self.std:float = 0

def create_substation_objects(ds_data, substation_numbers):
    substations = []
    for substation_number in substation_numbers:
        substation = DistSubstation()
        data = ds_data[ds_data['Substation Number'] == substation_number]
        substation.name = data['Name'].values[0]
        substation.id = data['Substation Number'].values[0]

        gis_data = GISData()
        gis_data.latitude = data['LATITUDE'].values[0]
        gis_data.longitude = data['LONGITUDE'].values[0]
        gis_data.boundaries = data['geometry'].values[0]
        substation.gisData = gis_data

        params = SubstationParams()
        params.numCustomers = data['Customers'].values[0]
        params.parentLSOAs = None
        substation.params = params
        substations.append(substation)

    return substations