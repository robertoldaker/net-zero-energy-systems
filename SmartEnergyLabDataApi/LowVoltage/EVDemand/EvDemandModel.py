from enum import Enum

class Substation:
    def __init__(self) -> None:
        self.name:str
        self.id:int
        self.gisData:GISData
        self.evDemands:EVDemands
        self.params:SubstationParams

class SubstationParams:
    def __init__(self) -> None:
        self.numCustomers:int

class GISData:
    def __init__(self) -> None:
        self.latitude:float
        self.longitude:float
        self.boundaries:list[GISBoundary]

class GISBoundary:
    def __init__(self) -> None:
        self.latitudes:list[float]
        self.longitudes:list[float]

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

