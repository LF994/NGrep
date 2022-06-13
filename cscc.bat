@echo off
setlocal

set CSC_FWR=C:\WINDOWS\Microsoft.NET\Framework
:: select version below, check your FrameworkDir for the latest/proper:
:: set CSC_VER=v2.0.50727
:: set CSC_VER=v3.5
set CSC_VER=v4.0.30319
set CSC_DIR=%CSC_FWR%\%CSC_VER%
set CSC_RUN="%CSC_DIR%\csc.exe" /nologo

:: the only environment variable below is required for CS compiler (?)
set LIBPATH=%CSC_DIR%
:: may add more (of your own) to above path, if needed 


echo Run: %CSC_RUN%
echo Src: %*
%CSC_RUN%  %* 

set CS_LVL=%ERRORLEVEL%
echo Done: ErrorLevel=%CS_LVL%
:: eof
