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

echo adb shell su 0 mfg-tool -g EX_PART_NUMBER
adb -s %1 shell su 0 mfg-tool -g EX_PART_NUMBER 2>&1 | findstr /c:%2
if %errorlevel% NEQ 0 (
	timeout /t 2 >null
	adb -s %1 shell su 0 mfg-tool -g EX_PART_NUMBER 2>&1 | findstr /c:%2
	if %errorlevel% NEQ 0 (
		timeout /t 2 >null
		adb -s %1 shell su 0 mfg-tool -g EX_PART_NUMBER 2>&1 | findstr /c:%2
		if %errorlevel% NEQ 0 (
			echo Unit SKU:
			adb -s %1 shell su 0 mfg-tool -g EX_PART_NUMBER
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