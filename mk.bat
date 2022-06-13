@echo off
:: build NGrep and related tests

call cscc.bat /define:_RDATEST crdargs.cs
call cscc.bat scand.cs cscandir.cs crdargs.cs
call cscc.bat ngrep.cs cscandir.cs crdargs.cs
