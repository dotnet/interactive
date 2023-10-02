@echo off
powershell -noprofile -executionPolicy RemoteSigned -file "%~dp0eng\build.ps1" -noJS -build -restore -binaryLog %*
