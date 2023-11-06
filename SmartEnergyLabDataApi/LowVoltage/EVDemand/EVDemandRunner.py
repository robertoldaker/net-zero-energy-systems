import fileinput
from json import JSONDecodeError
import sys

from EvDemandModel import EVDemandInput

def main():
    print("OK")
    cont = True
    while(cont):
        line = sys.stdin.readline().strip()        
        if line == 'EXIT':
            cont=False
        else:
            output=runPrediction(line)
            sys.stdout.write(output)
            sys.stdout.write("\n")

def runPrediction(line)->str:
    print(line)
    try:
        input = EVDemandInput.fromJson(line)
    except JSONDecodeError as e:
        return f"ERROR: {e.args[0]}"
    return "OK"

if __name__ == "__main__":
    main()