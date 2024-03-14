import argparse
from enum import IntEnum
import os
import subprocess
import sys

class GitState(IntEnum):
    OK=0
    NEEDS_UPDATE=1
    NEEDS_COMMIT=2
    NEEDS_PUSH=3
    NEEDS_ADD=4

class GitUtils:
    def __init__(self, f: str):
        self.folder = f

    def GetStatus(self)->tuple[GitState,list[str],list[str]]:

        gitState = GitState.NEEDS_UPDATE
        filesToCommit:list[str] = []
        filesToAdd:list[str] = []
        response=subprocess.run(['git','status',self.folder],capture_output=True)
        if response.returncode!=0:
            raise Exception(f'Problem running git status command: {response.stderr}')
        lines = response.stdout.splitlines()
        lineIndex = 0
        while lineIndex<len(lines):
            line = lines[lineIndex].decode()
            if line.startswith('Your branch is ahead of'):
                gitState = GitState.NEEDS_PUSH
            if line.startswith('Your branch is up-to-date with'):
                gitState = GitState.OK
            if line.startswith('Changes to be committed:'):
                gitState = GitState.NEEDS_COMMIT
                while lineIndex<len(lines):
                    line = lines[lineIndex].decode()
                    if line.startswith('\t'):
                        filesToCommit.append(line)
                    if line == b'':
                        break
                    lineIndex+=1
            if line.startswith('Untracked files:'):
                gitState = GitState.NEEDS_ADD
                while lineIndex<len(lines):
                    line = lines[lineIndex].decode()
                    if line.startswith('\t'):
                        filesToAdd.append(line)
                    if line == b'':
                        break
                    lineIndex+=1
            lineIndex+=1
        return (gitState,filesToCommit,filesToAdd)
    
    def GetLatestLogInfo(self)->tuple[bytes,bytes]:
        response=subprocess.run(['git','log','-1',self.folder],capture_output=True)
        if response.returncode!=0:
            raise Exception(f'Problem running git log command: {response.stderr}')
        lines = response.stdout.splitlines()

        for line in lines:
            if line.startswith(b'commit '):
                commitId = line.replace(b'commit ',b'')
            if line.startswith(b'Date:  '):
                date = line.replace(b'Date:   ',b'')
        return (commitId,date)

def writeToStdError(str):
    sys.stderr.write(str)
    sys.stderr.write("\n")
    sys.stderr.flush()

def main():
    def UpdateVersionFile():

        # readin file
        with open(inputVersionFile, 'r') as file:
            inputData = file.read()

        # make substitutions
        inputData = inputData.replace("$COMMIT_ID$",commitId).replace("$COMMIT_DATE$",commitDate)

        # write out file
        with open(outputVersionFile, 'w') as file:
            file.write(inputData)

    ap = argparse.ArgumentParser(description="CheckVersion")
    ap.add_argument("folder")
    ap.add_argument("inputVersionFile")
    ap.add_argument("outputVersionFile")
    args = vars(ap.parse_args())
    folder = args['folder']
    if not os.path.exists(folder):
        raise Exception(f'folder [{folder}] does not exist')
    inputVersionFile = args['inputVersionFile']
    if not os.path.exists(inputVersionFile):
        raise Exception(f'Input file [{inputVersionFile}] does not exist')
    outputVersionFile = args['outputVersionFile']
    
    #
    gu = GitUtils(folder)
    (gs,filesToCommit,filesToAdd) = gu.GetStatus()
    print(gs)
    if gs == GitState.NEEDS_COMMIT:
        writeToStdError(f'Git repository needs a commit and push to publish staged changes before app. can be published. Files to commit ...')
        for file in filesToCommit:
            writeToStdError(file)
        exit(1)
    elif gs == GitState.NEEDS_PUSH:
        writeToStdError(f'Git repository needs local commits pushed to server before app. can be published.')
        exit(2)
    elif gs==GitState.NEEDS_UPDATE:
        writeToStdError(f'Git repository needs update from server before app. can be published.')
        exit(3)
    elif gs==GitState.NEEDS_ADD:
        writeToStdError(f'Git repository has files that need adding or ignoring')
        for file in filesToAdd:
            writeToStdError(file)
        exit(3)
    
    info = gu.GetLatestLogInfo()
    commitId = info[0].decode()
    commitDate = info[1].decode()
    UpdateVersionFile()
    
if __name__ == '__main__':
    main()