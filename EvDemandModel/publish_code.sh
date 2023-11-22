#
app="EvDemandModel"

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

echo "Zipping files .."
zip -r $app.zip . -x "EvDemandModel/Data/*" -x "*/__pycache__/*"

echo "Copying to $dest"
# sftp and ssh use key based authentication that needs setting up - see https://www.digitalocean.com/community/tutorials/how-to-configure-ssh-key-based-authentication-on-a-linux-server
echo -e "cd websites\nput $app.zip" | sftp $dest
echo "Installing on $dest"
#

ssh $dest "unzip -o websites/$app.zip -d websites/$app"

echo "Publish successful. Press any key to continue";
read


