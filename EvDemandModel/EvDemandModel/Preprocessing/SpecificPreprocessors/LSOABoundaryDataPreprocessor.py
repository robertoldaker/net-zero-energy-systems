import geopandas as gpd
from ..BasePreprocessor import BasePreprocessor
from ..DataContainer import DataContainer
from ...Utils.EVDemandOutput import EVDemandOutput

class LSOABoundaryDataPreprocessor(BasePreprocessor):

    def preprocess(self) -> gpd.GeoDataFrame:
        super().preprocess(geo=True)
        self.data_container.data = self.data_container.data.set_index('LSOA11CD')
        EVDemandOutput.logMessage('LSOABoundaryDataPreprocessor pre-processing complete')
        return self.data_container.data