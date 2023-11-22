function raiseError()
{
    echo "$1";
    echo "Press any key to continue";
    read
    exit -1;
}

usage="Usage:

bash $0 [staging|production]
"

if [ $# -ne 1 ]; then
    raiseError "$usage";
fi 

# Check params
if [ "$1" = "staging" ]; then
    dest="rob@odin.local"
elif [ "$1" = "production" ]; then
    dest="roberto@77.68.31.100"
else
    raiseError "$usage"
fi

rsync -aP ../EvDemandModel/EvDemandModel/Data -e 'ssh' $dest:~/websites/EvDemandModel/EvDemandModel
