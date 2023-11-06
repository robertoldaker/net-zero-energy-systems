from enum import Enum
import json

class EVDemandInput:
    def __init__(self,dict=None)->None:
        self.predictorParams: EVDemandInput.PredictorParams
        self.regionData: list[EVDemandInput.RegionData]
        if dict:
            self.__dict__ = dict

    class RegionType(Enum):
        Dist=0
        Primary=1
        GSP=2

    class RegionData:
        def __init__(self,dict=None)->None:
            self.id: int
            self.type: EVDemandInput.RegionType
            self.latitudes: list[float]
            self.longitudes: list[float]
            self.numCustomers: int
            if dict:
                self.__dict__ = dict
    
    class VehicleUsage(Enum):
        Low=0
        Medium=1
        High=2

    class PredictorParams:
        def __init__(self, dict: None)->None:
            self.vehicleUsage: EVDemandInput.VehicleUsage
            self.years: list[int]
            if dict:
                self.__dict__ = dict
    
    @staticmethod
    def fromJson(jsonStr: str):
        return json.loads(jsonStr, object_hook=EVDemandInput._objectHook)
    
    @staticmethod
    def _objectHook(dict):
        if ( 'className' in dict):
            className = dict['className']
            if (className=='EVDemandInput'):
                return EVDemandInput(dict)
            elif (className=='EVDemandInput.RegionData'):            
                return EVDemandInput.RegionData(dict)
            elif (className=='EVDemandInput.PredictorParams'):
                return EVDemandInput.PredictorParams(dict)

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

