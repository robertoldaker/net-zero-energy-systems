import fileinput
from json import JSONDecodeError
import json
import sys
import time

from EvDemandModel.EvDemandModel import EVDemandInput

class EVDemandOutput:
    def __init__(self) -> None:
        pass

    def printLogMessage(self,mess:str)->None:
        sys.stdout.write("LOG:")
        sys.stdout.write(mess);
        sys.stdout.write("\n");
        sys.stdout.flush();

    def printErrorMessage(self,mess:str)->None:
        sys.stdout.write("ERROR:")
        sys.stdout.write(mess);
        sys.stdout.write("\n");
        sys.stdout.flush();

    def printResultMessage(self,mess:str)->None:
        sys.stdout.write("RESULT:")
        sys.stdout.write(mess);
        sys.stdout.write("\n");
        sys.stdout.flush();

    def printOkMessage(self)->None:
        sys.stdout.write("OK:\n")
        sys.stdout.flush();

    def printProgressMessage(self,progress:int)->None:
        sys.stdout.write("PROGRESS:")
        sys.stdout.write(str(progress))
        sys.stdout.write("\n");
        sys.stdout.flush();

    def printProgressTextMessage(self,progressText:str)->None:
        sys.stdout.write("PROGRESS_TEXT:")
        sys.stdout.write(progressText)
        sys.stdout.write("\n");
        sys.stdout.flush();


def main():
    out=EVDemandOutput()
    count=0
    while count<10:
        out.printLogMessage(f"count={count}")
        count+=1
        time.sleep(1)

    out.printOkMessage()
    cont = True
    while(cont):
        line = sys.stdin.readline().strip()        
        if line == 'EXIT:':
            cont=False
        else:
            runPrediction(out,line)

def runPrediction(out: EVDemandOutput,line:str):
    try:
        input = EVDemandInput.fromJson(line)
        out.printLogMessage("Start processing input")
        out.printProgressTextMessage("Processing input ...")
        count=1
        while count<=10:
            out.printProgressTextMessage(f"Processing input {count}...")
            out.printProgressMessage((int) ((count*100.0)/10.0))
            time.sleep(1)
            count+=1
        
        output= {'numRegionData': len(input.regionData)}        
        outputJson = json.dumps(output)
        out.printLogMessage("Processed input")
        out.printResultMessage(outputJson)
    except BaseException as e:
        out.printErrorMessage(e.args[0])
        out.printOkMessage();


if __name__ == "__main__":
    main()