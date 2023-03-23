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
REM build angular separately
echo "Building Angular app .."
pushd ClientApp
npm install || raiseError;
npm run build || raiseError;
popd
echo "Building ASP.NET app .."
dotnet publish SmartEnergyLabGui.csproj -o "ASP_BUILD" -c "RELEASE" -f "net6.0"
if [ $? -ne 0 ]; then
    raiseError;
fi

echo "Zipping files .."
pushd ASP_BUILD
zip -r ../SmartEnergyLabGui.zip *
popd

echo "Copying to blackbox"
# sftp and ssh use key based authentication that needs setting up - see https://www.digitalocean.com/community/tutorials/how-to-configure-ssh-key-based-authentication-on-a-linux-server
sftp -b psftp.txt rob@blackbox.local
echo "Installing on blackbox"
#
ssh rob@blackbox.local "~/installs/autoInstall.sh SmartEnergyLabGui"

echo "Build & publish successful. Press any key to continue";
read

