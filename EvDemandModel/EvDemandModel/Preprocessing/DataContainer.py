import pandas as pd
from dataclasses import dataclass

@dataclass
class DataContainer:
    file_path: str
    data: pd.DataFrame = None