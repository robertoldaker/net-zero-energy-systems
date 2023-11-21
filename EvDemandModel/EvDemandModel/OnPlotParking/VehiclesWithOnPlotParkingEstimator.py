import pandas as pd
import numpy as np

class VehiclesWithOnPlotParkingEstimator:
    def __init__(
            self, 
            opp_samples: pd.DataFrame,
            car_van_2021_data: pd.DataFrame,
            house_data: pd.DataFrame,
        ) -> None:
        
        self.opp_samples = opp_samples
        self.car_van_2021_data = car_van_2021_data
        self.house_data = house_data
        self.houses_with_cars = None
        self.cars_per_house_with_car = None
        self.cars_with_opp_samples = None
        self.proportion_of_vehicles_with_opp = None

    def estimate(self):
        """ 
        This function assumes that all on-plot parking spaces are filled before
        off-plot parking is used.
        """
        self._estimate_houses_with_cars()       
        self._estimate_proportion_of_vehicles_with_opp()
        return self.proportion_of_vehicles_with_opp
    
    def _estimate_houses_with_cars(self):  
        self.houses_with_cars = self.house_data['households'] - self.car_van_2021_data['houses_without_cars']
        self.cars_per_house_with_car = self.car_van_2021_data['cars'] / self.houses_with_cars
    
    def _estimate_proportion_of_vehicles_with_opp(self):
        # Assume all houses with on-plot parking have cars. The number of cars with on-plot parking is equal to:
        # The minimum of [Number of houses with cars AND Number of houses with on-plot parking spaces]
        # Multiplied by the number of cars per house with a car.
        houses_with_cars_and_opp = self.opp_samples.clip(upper=self.houses_with_cars, axis=1)
        self.cars_with_opp_samples = houses_with_cars_and_opp.multiply(self.cars_per_house_with_car, axis=1)
        self.proportion_of_vehicles_with_opp = self.cars_with_opp_samples.divide(self.car_van_2021_data['cars']).replace([np.inf, -np.inf], np.nan)