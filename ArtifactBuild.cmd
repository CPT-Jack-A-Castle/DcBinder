@echo off
pushd "%~dp0"
powershell Compress-7Zip "DcBinder\bin\Release" -ArchiveFileName "DcBinder.zip" -Format Zip
:exit
popd
@echo on
