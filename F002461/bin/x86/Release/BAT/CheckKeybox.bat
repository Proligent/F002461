@echo off
echo ***************Start***************
echo BatFile: %0
echo DeviceId: %1
echo KeyboxDir: %2
echo KeyboxFile: %3
echo KeyboxDevice: %4

SET Keybox=%2\%3

echo adb get-state
adb -s %1 get-state 2>&1 | findstr /c:"error"
if %errorlevel% == 0 (
	adb -s %1 get-state 2>&1 | findstr /c:"error"
	if %errorlevel% == 0 (
		adb -s %1 get-state 2>&1 | findstr /c:"error"
		if %errorlevel% == 0 (
			goto :failed
		)
	)
)

echo adb root
adb -s %1 root
if %errorlevel% == 1 goto :failed

::Check Keybox Whether Exist
echo Check Keybox Exist
adb -s %1 shell ls /mnt/vendor/persist/data/ 2>&1 | findstr /c:"DdHdVQd1FIOARksZgXG27GJ1A5UjYCGhP-ZoGvseqsY_"
if %errorlevel% NEQ 0 (
	timeout /t 2 >null
	adb -s %1 shell ls /mnt/vendor/persist/data/ 2>&1 | findstr /c:"DdHdVQd1FIOARksZgXG27GJ1A5UjYCGhP-ZoGvseqsY_"
	if %errorlevel% NEQ 0 (
		timeout /t 2 >null
		adb -s %1 shell ls /mnt/vendor/persist/data/ 2>&1 | findstr /c:"DdHdVQd1FIOARksZgXG27GJ1A5UjYCGhP-ZoGvseqsY_"
		if %errorlevel% NEQ 0 (
			echo Check Keybox Exist Result:
			adb -s %1 shell ls /mnt/vendor/persist/data/
			echo Keybox Not Exist!!
			goto :failed
		)
	)
) 

echo Keybox Already Exist!!
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
timeout /t 3
echo ***************End***************
exit