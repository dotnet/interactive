@echo off
pwsh -noprofile -executionPolicy RemoteSigned -file "%~dp0eng\build.ps1" -test -binaryLog %*
