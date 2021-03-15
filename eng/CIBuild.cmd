@echo off
powershell -noprofile -executionPolicy RemoteSigned -file "%~dp0common\build.ps1" -ci -restore -build -pack -publish -binaryLog %*
