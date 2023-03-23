REM perl -I..\Build ..\Build\UpdateVersionFile.pl .. VersionData.csx VersionData.cs
REM if errorlevel 1 goto error
echo "Deleting old ASP.NET build ..."
del ASP_BUILD /f /s /q > nul
rmdir ASP_BUILD /q /s
echo "Building Angular app .."
REM build angular separately
pushd ClientApp
call npm install
call npm run build
popd
echo "Building ASP.NET app .."
REM build .net server and copy everything to output
dotnet publish SmartEnergyLabGui.csproj -o "ASP_BUILD" -c "RELEASE" -f "net6.0"
if errorlevel 1 goto error
echo "Building installer ..."
pushd ASP_BUILD
"C:\Program Files\7-Zip\7z" a ..\SmartEnergyLabGui.zip .
popd
if errorlevel 1 goto error
"c:\Program files\PuTTY\psftp.exe" -b psftp.txt -pw speedy -bc rob@blackbox.local
if errorlevel 1 goto error
rem install on server
"c:\Program files\PuTTY\plink.exe" -pw speedy rob@blackbox.local "~/installs/autoInstall.sh SmartEnergyLabGui"
pause
exit /b
:error
echo "Error building SmartEnergyLabGui"
pause