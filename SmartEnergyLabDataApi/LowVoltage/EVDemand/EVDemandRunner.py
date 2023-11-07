import fileinput
from json import JSONDecodeError
import sys
import time

from EvDemandModel import EVDemandInput

def main():
    time.sleep(10)   
    sys.stdout.write("OK\n")
    sys.stdout.flush()
    cont = True
    while(cont):
        line = sys.stdin.readline().strip()        
        if line == 'EXIT':
            cont=False
        else:
            output=runPrediction(line)
            sys.stdout.write(output)
            sys.stdout.write("\n")
            sys.stdout.flush();

def runPrediction(line)->str:
    try:
        input = EVDemandInput.fromJson(line)
        return f"num regionData={len(input.regionData)}"
    except BaseException as e:
        return f"ERROR: {e.args[0]}"

if __name__ == "__main__":
    main()