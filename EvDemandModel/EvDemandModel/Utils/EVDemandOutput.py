import sys

class EVDemandOutput:
    def __init__(self) -> None:
        pass

    @staticmethod
    def logMessage(mess:str)->None:
        sys.stdout.write("LOG:")
        sys.stdout.write(mess);
        sys.stdout.write("\n");
        sys.stdout.flush();

    @staticmethod
    def errorMessage(mess:str)->None:
        sys.stdout.write("ERROR:")
        sys.stdout.write(mess);
        sys.stdout.write("\n");
        sys.stdout.flush();

    @staticmethod
    def resultMessage(mess:str)->None:
        sys.stdout.write("RESULT:")
        sys.stdout.write(mess);
        sys.stdout.write("\n");
        sys.stdout.flush();

    @staticmethod
    def okMessage()->None:
        sys.stdout.write("OK:\n")
        sys.stdout.flush();

    @staticmethod
    def progressMessage(progress:int)->None:
        sys.stdout.write("PROGRESS:")
        sys.stdout.write(str(progress))
        sys.stdout.write("\n");
        sys.stdout.flush();

    @staticmethod
    def progressTextMessage(progressText:str)->None:
        sys.stdout.write("PROGRESS_TEXT:")
        sys.stdout.write(progressText)
        sys.stdout.write("\n");
        sys.stdout.flush();
