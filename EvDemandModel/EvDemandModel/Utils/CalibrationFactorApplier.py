import pandas as pd

def calibrate(data: pd.DataFrame, calibration_factors: pd.DataFrame, quarter: str) -> pd.DataFrame:
    calibrated_data = (1 + calibration_factors).mul(data[quarter]).round(0).astype('Int64')
    return calibrated_data