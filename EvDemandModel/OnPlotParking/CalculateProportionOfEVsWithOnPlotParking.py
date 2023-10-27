import pandas as pd
from .EVsWithOnPlotParkingEstimator import EVsWithOnPlotParkingEstimator

def calculate(
        proportion_of_vehicles_with_opp: pd.DataFrame,
        vehicle_registrations_data: pd.DataFrame,
        ev_registrations_data: pd.DataFrame,
        quarter: str
    ):

    evs_with_opp_estimator = EVsWithOnPlotParkingEstimator(proportion_of_vehicles_with_opp,
                                                           vehicle_registrations_data)
    
    proportion_of_evs_with_opp = evs_with_opp_estimator.estimate(ev_registrations_data, quarter)
    
    return proportion_of_evs_with_opp