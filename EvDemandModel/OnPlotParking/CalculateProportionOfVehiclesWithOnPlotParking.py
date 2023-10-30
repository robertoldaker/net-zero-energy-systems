from .AccommodationTypeCounter import AccommodationTypeCounter
from .OnPlotParkingEstimator import OnPlotParkingEstimator
from .VehiclesWithOnPlotParkingEstimator import VehiclesWithOnPlotParkingEstimator

N_SAMPLES = 1000

def calculate(
        accomodation_type_2021_data,
        house_2021_data,
        car_van_2021_data
    ):

    accommodation_type_counter = AccommodationTypeCounter(accomodation_type_2021_data, house_2021_data)
    accommodation_type_counts = accommodation_type_counter.count()

    opp_estimator = OnPlotParkingEstimator(accommodation_type_counts, n_samples=N_SAMPLES)
    opp_samples = opp_estimator.estimate()

    vehicles_with_opp_estimator = VehiclesWithOnPlotParkingEstimator(opp_samples, car_van_2021_data, house_2021_data)
    proportion_of_vehicles_with_opp = vehicles_with_opp_estimator.estimate()

    return proportion_of_vehicles_with_opp