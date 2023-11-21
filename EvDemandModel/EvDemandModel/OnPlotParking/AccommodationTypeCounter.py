import pandas as pd

class AccommodationTypeCounter:
    
    def __init__(self, accommodation_type_df: pd.DataFrame, houses_df: pd.DataFrame) -> None:
        self.df = accommodation_type_df
        self.houses = houses_df
        self.data = None

    def count(self) -> pd.DataFrame:
        types_data = [
            self._filter_by_type('Detached', 'detached'),
            self._filter_by_type('Semi-detached', 'semi_detached'),
            self._filter_by_type('Terraced', 'terraced'),
            self._filter_by_type('In a purpose-built block of flats or tenement', 'purpose_built_flat'),
            self._combine_converted_flats()
        ]
        concatenated = pd.concat(types_data, axis=1)
        # Group by index and calculate mean (for LSOA21CDs that share a LSOA11CD)
        mean = concatenated.groupby(concatenated.index).mean()
        proportions = mean.div(mean.sum(axis=1), axis=0)
        self.data = round(proportions.mul(self.houses['households'], axis=0))
        return self.data

    def _filter_by_type(self, accommodation_type: str, new_name: str) -> pd.Series:
        result = self.df[self.df['accommodation_type'] == accommodation_type]['Observation']
        result.name = new_name
        return result

    def _combine_converted_flats(self) -> pd.Series:
        types = [
            'Part of a converted or shared house, including bedsits',
            'Part of another converted building, for example, former school, church or warehouse',
            'In a commercial building, for example, in an office building, hotel or over a shop'
        ]
        return sum([self._filter_by_type(atype, 'converted_flat') for atype in types])