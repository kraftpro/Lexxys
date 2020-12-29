:@echo off

:set tpl=C:\Documents\Projects\Tpl
set src=%cd%

set f1=%src%\Logger.cs
set f3=
set f2=
set f4=
set f5=
set f6=
set f7=
set f8=
set f9=

set files=%f1% %f2% %f3% %f4% %f5% %f6% %f7% %f8% %f9%

cd /d %tpl%

perl sp2.pl %* %files%>nul
echo.
echo.
for %%f in (%files%) do if exist %%f.pl echo %%f&&call perl %%f.pl %1 %2 %3

cd /d %src%

