#!/bin/bash
app=$1
folder="$HOME/installs"
installFile="$folder/$app.zip"
installedFile="$folder/${app}_installed.zip"
previousFile="$folder/${app}_previous.zip"

if [ -e "$installFile" ]
then
	sudo systemctl stop $app.service
	unzip -o "$installFile" -d ~/websites/$app
	sudo systemctl start $app.service
	if [ -e "$installedFile" ]
	then
		if [ -e "$previousFile" ]
		then
			rm "$previousFile"
		fi
		mv "$installedFile" "$previousFile"
	fi
	mv "$installFile" "$installedFile"
fi

