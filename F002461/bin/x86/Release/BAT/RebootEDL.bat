@echo off
echo ***************Start***************
echo BatFile: %0
echo DeviceId: %1

echo adb get-state
adb -s %1 get-state 2>&1 | findstr /c:"error"
if %errorlevel% == 0 (
	timeout /t 2 >null
	adb -s %1 get-state 2>&1 | findstr /c:"error"
	if %errorlevel% == 0 (
		timeout /t 2 >null
		adb -s %1 get-state 2>&1 | findstr /c:"error"
		if %errorlevel% == 0 (
			goto :failed
		)
	)
)

echo adb reboot edl
adb -s %1 reboot edl
if %errorlevel% NEQ 0 (
	timeout /t 2 >null
	adb -s %1 reboot edl
	if %errorlevel% NEQ 0 (
		goto :failed
	)
)

goto :success

:failed  
echo *********************************  
echo FAILED!! 
echo ********************************* 

echo adb kill-server
adb -s %1 kill-server
echo adb start-server
adb -s %1 start-server

goto :end

:success
echo *********************************  
echo SUCCESS!!  
echo *********************************
goto :end

:end
timeout /t 5 >null
echo ***************End***************
exit