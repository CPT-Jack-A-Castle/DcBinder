@echo off
pushd "%~dp0"
powershell Compress-7Zip "Binaries\Release" -ArchiveFileName "DcBinder.zip" -Format Zip
:exit
popd
@echo on
