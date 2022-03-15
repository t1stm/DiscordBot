#!/bin/bash
SCRIPT_DIR=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )
cd "$SCRIPT_DIR" || exit
dotnet publish -c Release -r linux-x64
rm -rf /home/kris/BatTosho/executable/*
cp -r ./bin/Release/net6.0/linux-x64/publish/* /home/kris/BatTosho/executable/
killall BatToshoRESTApp
