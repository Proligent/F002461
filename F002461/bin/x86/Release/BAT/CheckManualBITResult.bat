@echo off
echo ***************Start***************
echo BatFile: %0
echo DeviceId: %1
echo SKU: %2

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

echo adb shell getprop persist.sys.BITAutoRlt
adb -s %1 shell getprop persist.sys.BITAutoRlt 2>&1 | findstr /c:"true"
if %errorlevel% NEQ 0 (
	timeout /t 2 >null
	adb -s %1 shell getprop persist.sys.BITAutoRlt 2>&1 | findstr /c:"true"
	if %errorlevel% NEQ 0 (
		timeout /t 2 >null
		adb -s %1 shell getprop persist.sys.BITAutoRlt 2>&1 | findstr /c:"true"
		if %errorlevel% NEQ 0 (
			echo Manual BIT Result:
			adb -s %1 shell getprop persist.sys.BITAutoRlt
			goto :failed
		)
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
timeout /t 3 >null
echo ***************End***************
exit