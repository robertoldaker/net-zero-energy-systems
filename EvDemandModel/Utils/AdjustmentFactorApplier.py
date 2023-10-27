import pandas as pd

def adjust(data: pd.DataFrame, adjustment_factors: pd.DataFrame, quarter: str) -> pd.DataFrame:
    adjusted_data = (1 + adjustment_factors).mul(data[quarter]).round(0).astype('Int64')
    return adjusted_data