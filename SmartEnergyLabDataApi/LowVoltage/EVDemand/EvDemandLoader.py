import requests
import json

class DistributionSubstation:
    def __init__(self,d) -> None:
        self.__dict__ = d
        self.id:int
        self.name:str
        self.gisData=GISData(d['gisData'])

class GISData:
    def __init__(self,d) -> None:
        self.__dict__=d
        self.id:int
        self.latitude:float
        self.longitude:float

class SubstationClassification:
    def __init__(self,d) -> None:
        self.__dict__ = d
        self.id:int
        self.numCustomers:int

def main():
    baseUrl = "http://localhost:5095"
    gspId = 1
    url = f'{baseUrl}/Substations/DistributionSubstationsByGridSupplyPointId?gspId={gspId}'
    print(url)
    response=requests.get(url)
    jsonList=response.json()
    dssList=list()
    index=0
    gisDataIds=''
    dssIds=''
    for json in jsonList:
        # create dist substation
        dss=DistributionSubstation(json)
        dssList.append(dss)
        if index % 500 == 0:
            # GISData
            gisDataIds+=f'{dss.gisData.id}'
            url = f'{baseUrl}/GIS/Boundaries/List?gisDataIds={gisDataIds}'
            response = requests.get(url)
            if response.ok:
                jsonList=response.json()
                print(f'Boundaries={len(jsonList)}')
            else:
                print(f'Problem calling [{url}] [{response.reason}]')
            gisDataIds=''

            # classifications
            dssIds+=f'{dss.id}'
            url = f'{baseUrl}/Classifications/DistributionSubstationClassifications/List?dssIds={dssIds}'
            response = requests.get(url)
            if response.ok:
                jsonList=response.json()
                print(f'Classifications={len(jsonList)}')
            else:
                print(f'Problem calling [{url}] [{response.reason}]')
            dssIds=''
        else:
            gisDataIds+=f'{dss.gisData.id},'
            dssIds+=f'{dss.id},'
        index+=1

if __name__ == "__main__":
    main()