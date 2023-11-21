import pandas as pd
import numpy as np
import scipy.stats as stats

class EVsWithOnPlotParkingEstimator:
    def __init__(
            self, 
            proportion_of_vehicles_with_opp: pd.DataFrame,
            vehicle_registrations_data: pd.DataFrame
        ) -> None:
        self.proportion_of_vehicles_with_opp = proportion_of_vehicles_with_opp
        self.vehicle_registrations_data = vehicle_registrations_data

    def estimate(self, ev_registrations_data: pd.DataFrame, quarter: str):
        proportion_of_evs_with_opp = self._calculate_proportion_of_evs_with_on_plot_parking(ev_registrations_data, quarter)
        return proportion_of_evs_with_opp

    def _calculate_proportion_of_evs_with_on_plot_parking(self, ev_registrations_data: pd.DataFrame, quarter: str):
        # Define alpha and beta values for beta distribution that describes likely starting points for EV on-plot parking access
        mu = 0.9 # prior mean
        var = 0.1 * mu*(1-mu)
        alpha = mu*(mu*(1-mu)/var - 1)
        beta = (1-mu)*(mu*(1-mu)/var - 1)

        c = stats.beta.rvs(a=alpha, b=beta, size=self.proportion_of_vehicles_with_opp.shape) # y intercept
        m = (self.proportion_of_vehicles_with_opp - c).div(self.vehicle_registrations_data[quarter], axis=1) # gradient
        p = m.mul(ev_registrations_data[quarter], axis=1) + c
        p = p.clip(lower=self.proportion_of_vehicles_with_opp)
        p = p.fillna(self.proportion_of_vehicles_with_opp)
        return np.clip(p, a_min=0, a_max=1)