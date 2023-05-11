#
dest="rob@odin.local"
app="SmartEnergyLabGui"

function raiseError()
{
    echo "Press any key to continue";
    read
    exit -1;
}

# Not implemented yet
# perl -I../Build ../Build/UpdateVersionFile.pl .. VersionData.csx VersionData.cs
if [ $? -ne 0 ]; then
    raiseError;
fi 
echo "Deleting old ASP.NET build ..."
rm -r ASP_BUILD
# build angular separately
echo "Building Angular app .."
pushd ClientApp
npm install;
npm run build -- --c staging || raiseError;
popd
echo "Building ASP.NET app .."
dotnet publish $app.csproj -o "ASP_BUILD" -c "RELEASE" -f "net6.0"
if [ $? -ne 0 ]; then
    raiseError;
fi

echo "Zipping files .."
pushd ASP_BUILD
zip -r ../$app.zip *
popd

echo "Copying to $dest"
# sftp and ssh use key based authentication that needs setting up - see https://www.digitalocean.com/community/tutorials/how-to-configure-ssh-key-based-authentication-on-a-linux-server
echo -e "cd installs\nput $app.zip" | sftp $dest
echo "Installing on $dest"
#
ssh $dest "bash ~/installs/autoInstall.sh $app"

echo "Build & publish successful. Press any key to continue";
read

