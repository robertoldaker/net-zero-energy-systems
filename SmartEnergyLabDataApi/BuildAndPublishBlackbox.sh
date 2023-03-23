#
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
echo "Building ASP.NET app .."
dotnet publish SmartEnergyLabDataApi.csproj -o "ASP_BUILD" -c "RELEASE" -f "net6.0"
if [ $? -ne 0 ]; then
    raiseError;
fi

echo "Zipping files .."
pushd ASP_BUILD
zip -r ../SmartEnergyLabDataApi.zip *
popd

echo "Copying to blackbox"
sftp -b psftp.txt rob@blackbox.local
echo "Installing on blackbox"
# Need to use install.txt since printing from plink not treating \n correctly?
ssh rob@blackbox.local "~/installs/autoInstall.sh SmartEnergyLabDataApi"

echo "Build & publish successful. Press any key to continue";
read

