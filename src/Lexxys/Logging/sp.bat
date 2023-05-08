@echo off

set tpl=C:\Projects\Tpl
set src=%cd%

set files=%src%\ILoggingExtensions.cs %src%\ILoggerExtensions.cs %src%\LoggingInterpolatedStringHandler.cs

cd /d %tpl%

call perl sp2.pl %* %files% >nul

for %%f in (%files%) do if exist %%f.pl call perl %%f.pl %1 %2 %3&&del %%f.pl

cd /d %src%

