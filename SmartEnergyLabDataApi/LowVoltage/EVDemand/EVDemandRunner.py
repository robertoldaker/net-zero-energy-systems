import fileinput
from json import JSONDecodeError
import json
import sys
import time

from EvDemandModel import EVDemandInput

def main():
    count=0
    while count<10:
        printLogMessage(f"count={count}")
        count+=1
        time.sleep(1)

    printOkMessage()
    cont = True
    while(cont):
        line = sys.stdin.readline().strip()        
        if line == 'EXIT:':
            cont=False
        else:
            runPrediction(line)

def runPrediction(line):
    try:
        input = EVDemandInput.fromJson(line)
        printLogMessage("Start processing input")
        printProgressTextMessage("Processing input ...")
        count=1
        while count<=10:
            printProgressTextMessage(f"Processing input {count}...")
            printProgressMessage((int) ((count*100.0)/10.0))
            time.sleep(1)
            count+=1
        
        output= {'numRegionData': len(input.regionData)}        
        outputJson = json.dumps(output)
        printLogMessage("Processed input")
        printResultMessage(outputJson)
    except BaseException as e:
        printErrorMessage(e.args[0])
        printOkMessage();

def printLogMessage(mess:str):
    sys.stdout.write("LOG:")
    sys.stdout.write(mess);
    sys.stdout.write("\n");
    sys.stdout.flush();

def printErrorMessage(mess:str):
    sys.stdout.write("ERROR:")
    sys.stdout.write(mess);
    sys.stdout.write("\n");
    sys.stdout.flush();

def printResultMessage(mess:str):
    sys.stdout.write("RESULT:")
    sys.stdout.write(mess);
    sys.stdout.write("\n");
    sys.stdout.flush();

def printOkMessage():
    sys.stdout.write("OK:\n")
    sys.stdout.flush();

def printProgressMessage(progress:int):
    sys.stdout.write("PROGRESS:")
    sys.stdout.write(str(progress))
    sys.stdout.write("\n");
    sys.stdout.flush();

def printProgressTextMessage(progressText:str):
    sys.stdout.write("PROGRESS_TEXT:")
    sys.stdout.write(progressText)
    sys.stdout.write("\n");
    sys.stdout.flush();

if __name__ == "__main__":
    main()