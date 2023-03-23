REM perl -I..\Build ..\Build\UpdateVersionFile.pl .. VersionData.csx VersionData.cs
REM if errorlevel 1 goto error
echo "Deleting old ASP.NET build ..."
del ASP_BUILD /f /s /q > nul
rmdir ASP_BUILD /q /s
echo "Building Angular app .."
pushd ClientApp
REM build angular separately using staging build
call npm install
call npm run build-staging
popd
echo "Building ASP.NET app .."
REM deploy DEBUG build for now but RELEASE if we need this as a backup site
dotnet publish SmartEnergyLabGui.csproj -o "ASP_BUILD" -c "RELEASE" -f "net6.0"
if errorlevel 1 goto error
echo "Building installer ..."
pushd ASP_BUILD
"C:\Program Files\7-Zip\7z" a ..\SmartEnergyLabGui.zip .
popd
if errorlevel 1 goto error
"c:\Program files\PuTTY\psftp.exe" -b psftp.txt -pw speedy -bc rob@odin.local
if errorlevel 1 goto error
rem install on server
"c:\Program files\PuTTY\plink.exe" -pw speedy rob@odin.local "~/installs/autoInstall.sh SmartEnergyLabGui"
pause
exit /b
:error
echo "Error building SmartEnergyLabGui"
pause