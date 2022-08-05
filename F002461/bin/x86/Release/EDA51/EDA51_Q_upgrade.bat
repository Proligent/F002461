@echo off
echo File: %0
echo DeviceId: %1

echo ***************Start***************

echo oem enable-flash
fastboot -s %1 oem enable-flash

::USER DATA
echo flash userdata
set tempfileName=%1_temp
echo %tempfileName%
fastboot -s %1 getvar emmc-capacity 2
fastboot -s %1 getvar emmc-capacity 2> %tempfileName%
set /a a=1
set /p a= <%tempfileName%
del %tempfileName%
set a=%a:~18,8%
if "%a%"=="1" (
    echo not found emmc-capacity  
	fastboot -s %1 flash userdata %~dp0\AP\userdata.img 
	if %errorlevel% NEQ 0 goto :failed
) else (
    echo %a%
	if "%a%" lss "efff00000" (
		if "%a%" lss "7fff00000" (
			if "%a%" lss "3fff00000" (
				if "%a%" lss "1fff00000" (
					echo 8G userdata
					fastboot -s %1 flash userdata %~dp0\AP\userdata_8G.img
					if %errorlevel% NEQ 0 goto :failed
				) else (
					echo 16G userdata
					fastboot -s %1 flash userdata %~dp0\AP\userdata_16G.img
					if %errorlevel% NEQ 0 goto :failed
				)
			) else (
				echo 32G userdata
				fastboot flash -s %1 userdata %~dp0\AP\userdata_32G.img
				if %errorlevel% NEQ 0 goto :failed
			)
		) else (
			echo 64G userdata
			fastboot flash -s %1 userdata %~dp0\AP\userdata_64G.img
			if %errorlevel% NEQ 0 goto :failed
		)
    ) else (
		echo invalid emmc-capacity
		goto :failed
	)
)

::MP
echo flash modem_a
fastboot -s %1 flash modem_a %~dp0\MP\NON-HLOS.bin
if %errorlevel% NEQ 0 goto :failed
echo flash modem_b
fastboot -s %1 flash modem_b %~dp0\MP\NON-HLOS.bin
if %errorlevel% NEQ 0 goto :failed
echo flash rpm_a
fastboot -s %1 flash rpm_a %~dp0\MP\rpm.mbn
if %errorlevel% NEQ 0 goto :failed
echo flash rpm_b
fastboot -s %1 flash rpm_b %~dp0\MP\rpm.mbn
if %errorlevel% NEQ 0 goto :failed
echo flash sbl1_a
fastboot -s %1 flash sbl1_a %~dp0\MP\sbl1.mbn
if %errorlevel% NEQ 0 goto :failed
echo flash sbl1_b
fastboot -s %1 flash sbl1_b %~dp0\MP\sbl1.mbn
if %errorlevel% NEQ 0 goto :failed
echo flash tz_a
fastboot -s %1 flash tz_a %~dp0\MP\tz.mbn
if %errorlevel% NEQ 0 goto :failed
echo flash tz_b
fastboot -s %1 flash tz_b %~dp0\MP\tz.mbn
if %errorlevel% NEQ 0 goto :failed
echo flash devcfg_a
fastboot -s %1 flash devcfg_a %~dp0\MP\devcfg.mbn
if %errorlevel% NEQ 0 goto :failed
echo flash devcfg_b
fastboot -s %1 flash devcfg_b %~dp0\MP\devcfg.mbn
if %errorlevel% NEQ 0 goto :failed
echo flash dsp_a
fastboot -s %1 flash dsp_a %~dp0\MP\adspso.bin
if %errorlevel% NEQ 0 goto :failed
echo flash dsp_b
fastboot -s %1 flash dsp_b %~dp0\MP\adspso.bin
if %errorlevel% NEQ 0 goto :failed
echo flash lksecapp
fastboot -s %1 flash lksecapp %~dp0\MP\lksecapp.mbn
if %errorlevel% NEQ 0 goto :failed
echo flash cmnlib_a
fastboot -s %1 flash cmnlib_a %~dp0\MP\cmnlib_30.mbn
if %errorlevel% NEQ 0 goto :failed
echo flash cmnlib_b
fastboot -s %1 flash cmnlib_b %~dp0\MP\cmnlib_30.mbn
if %errorlevel% NEQ 0 goto :failed
echo flash cmnlib64_a
fastboot -s %1 flash cmnlib64_a %~dp0\MP\cmnlib64_30.mbn
if %errorlevel% NEQ 0 goto :failed
echo flash cmnlib64_b
fastboot -s %1 flash cmnlib64_b %~dp0\MP\cmnlib64_30.mbn
if %errorlevel% NEQ 0 goto :failed
echo flash keymaster_a
fastboot -s %1 flash keymaster_a %~dp0\MP\keymaster64.mbn
if %errorlevel% NEQ 0 goto :failed
echo flash keymaster_b
fastboot -s %1 flash keymaster_b %~dp0\MP\keymaster64.mbn
if %errorlevel% NEQ 0 goto :failed

::AP
echo flash boot_a
fastboot -s %1 flash boot_a %~dp0\AP\boot.img
if %errorlevel% NEQ 0 goto :failed
echo flash boot_b
fastboot -s %1 flash boot_b %~dp0\AP\boot.img
if %errorlevel% NEQ 0 goto :failed
echo flash aboot_a
fastboot -s %1 flash aboot_a %~dp0\AP\emmc_appsboot.mbn
if %errorlevel% NEQ 0 goto :failed
echo flash aboot_b
fastboot -s %1 flash aboot_b %~dp0\AP\emmc_appsboot.mbn
if %errorlevel% NEQ 0 goto :failed
echo flash vendor_a
fastboot -s %1 flash vendor_a %~dp0\AP\vendor.img
if %errorlevel% NEQ 0 goto :failed
echo flash vendor_b
fastboot -s %1 flash vendor_b %~dp0\AP\vendor.img
if %errorlevel% NEQ 0 goto :failed
echo flash mdtp_a
fastboot -s %1 flash mdtp_a %~dp0\AP\mdtp.img
if %errorlevel% NEQ 0 goto :failed
echo flash mdtp_b
fastboot -s %1 flash mdtp_b %~dp0\AP\mdtp.img
if %errorlevel% NEQ 0 goto :failed
echo flash IPSM
fastboot -s %1 flash IPSM %~dp0\AP\ipsm.img
if %errorlevel% NEQ 0 goto :failed
echo flash license
fastboot -s %1 flash license %~dp0\AP\license.img
if %errorlevel% NEQ 0 goto :failed
echo flash splash
fastboot -s %1 flash splash %~dp0\AP\splash.img
if %errorlevel% NEQ 0 goto :failed
echo flash system_a
fastboot -s %1 flash system_a %~dp0\AP\system.img
if %errorlevel% NEQ 0 goto :failed
echo flash system_b
fastboot -s %1 flash system_b %~dp0\AP\system.img
if %errorlevel% NEQ 0 goto :failed

::Erase
echo erase devinfo
fastboot -s %1 erase devinfo 
if %errorlevel% NEQ 0 goto :failed
echo erase config
fastboot -s %1 erase config
if %errorlevel% NEQ 0 goto :failed

goto :success

:failed  
echo *********************************  
echo FLASH FAILED!! 
echo ********************************* 
goto :end

:success
echo *********************************  
echo FLASH SUCCESS!!  
echo *********************************
echo reboot
fastboot -s %1 reboot 
echo ***************End***************
goto :end

:end
timeout /t 5
exit