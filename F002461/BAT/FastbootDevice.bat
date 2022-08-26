@echo off
echo ***************Start***************
echo BatFile: %0
echo DeviceId: %1

echo fastboot devices
fastboot devices 2>&1 | findstr %1
if %errorlevel% NEQ 0 (
	timeout /t 2 >null
	fastboot devices 2>&1 | findstr %1
	if %errorlevel% NEQ 0 (
		timeout /t 2 >null
		fastboot devices 2>&1 | findstr %1
		if %errorlevel% NEQ 0 (
			goto :failed
		)
	)
)

goto :success

:failed  
echo *********************************  
echo FAILED!! 
echo ********************************* 
goto :end

:success
echo *********************************  
echo SUCCESS!!  
echo *********************************
goto :end

:end
timeout /t 1 >null
echo ***************End***************
exit