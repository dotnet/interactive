@echo off
powershell -noprofile -executionPolicy RemoteSigned -file "%~dp0eng\build.ps1" -restore -binaryLog %*
