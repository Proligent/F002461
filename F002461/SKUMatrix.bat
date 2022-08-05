@echo off
echo ***************Start***************
echo Model:%1
echo Sku:%2
set model=%1
set sku=%2
set OptionFile=%~dp0\SKUOption.txt

:Model
if "%model%"=="EDA51" (
    goto :EDA51
)
if "%model%"=="EDA51K" (
    goto :EDA51K
)
if "%model%"=="EDA61K" (
    goto :EDA61K
)
if "%model%"=="EDA71" (
    goto :EDA71
)
if "%model%"=="CT40" (
    goto :CT40
)
if "%model%"=="CT40P" (
    goto :CT40P
)
if "%model%"=="EDA52" (
    goto :EDA52
)
if "%model%"=="EDA5S" (
    goto :EDA5S
)
if "%model%"=="EDA56" (
    goto :EDA56
)
if "%model%"=="UL" (	
    goto :UL
)
					   
goto :end

:EDA51
::EDA51-1-B723SOGE EDA51-1HC-B72OGE EDA51-1-B623SQGR
set WWAN=%sku:~6,1%
echo WWAN flag %WWAN%
set AndroidVer=%sku:~13,1%
echo AndroidVer Flag %AndroidVer%
if "%AndroidVer%"=="O" (
	if "%WWAN%"=="1" ( 
		echo EDA51-1_Option.ini > %OptionFile%
		goto :end
	)
	if "%WWAN%"=="0" ( 
		echo EDA51-0_Option.ini > %OptionFile%
		goto :end
	)
	goto :end
)
if "%AndroidVer%"=="Q" (
	if "%WWAN%"=="1" ( 
		echo EDA51-1-Q_Option.ini > %OptionFile%
		goto :end
	)
	if "%WWAN%"=="0" ( 
		echo EDA51-0-Q_Option.ini > %OptionFile%
		goto :end
	)
	goto :end
)
if "%AndroidVer%"=="R" (
	if "%WWAN%"=="1" ( 
		echo EDA51-1-R_Option.ini > %OptionFile%
		goto :end
	)
	if "%WWAN%"=="0" ( 
		echo EDA51-0-R_Option.ini > %OptionFile%
		goto :end
	)
	goto :end
)						
goto :end

:EDA61K
::EDA61K-1-UB21PCC
set WWAN=%sku:~7,1%
echo WWAN flag %WWAN%
if "%WWAN%"=="1" ( 
	echo EDA61K-1_Option.ini > %OptionFile%
	goto :end
)
if "%WWAN%"=="0" ( 
	echo EDA61K-0_Option.ini > %OptionFile%
	goto :end
)
goto :end

:EDA71
::EDA71-1-B731SOGR
set WWAN=%sku:~6,1%
echo WWAN flag %WWAN%
if "%WWAN%"=="1" (
	echo EDA71-1_Option.ini > %OptionFile%
	goto :end
)
if "%WWAN%"=="0" (
	echo EDA71-0_Option.ini > %OptionFile%
	goto :end
)
goto :end

:EDA51K
::EDA51K-1-B731SOGR
set WWAN=%sku:~7,1%
echo WWAN flag %WWAN%
if "%WWAN%"=="1" (
	echo EDA51K-1_Option.ini > %OptionFile%
	goto :end
)
if "%WWAN%"=="0" (
	echo EDA51K-0_Option.ini > %OptionFile%
	goto :end
)
goto :end

:CT40
::CT40-L0N-1NC11AF
set WWAN=%sku:~6,1%
set Custom=%sku:~14,1%
echo WWAN flag %WWAN%
echo Custom flag %Custom%
if "%Custom%"=="0" (
	if "%WWAN%"=="1" ( 
		echo CT40-1_Option.ini > %OptionFile%
		goto :end
	)
	if "%WWAN%"=="0" ( 
		echo CT40-0_Option.ini > %OptionFile%
		goto :end
	)
	goto :end
)
if "%Custom%"=="A" (
	if "%WWAN%"=="1" ( 
		echo CT40Gen2-1_Option.ini > %OptionFile%
		goto :end
	)
	if "%WWAN%"=="0" ( 
		echo CT40Gen2-0-A_Option.ini > %OptionFile%
		goto :end
	)
	goto :end
)
if "%Custom%"=="B" ( 
	if "%WWAN%"=="1" ( 
		echo CT40Gen2-1_Option.ini > %OptionFile%
		goto :end
	)
	if "%WWAN%"=="0" ( 
		echo CT40Gen2-0_Option.ini > %OptionFile%
		goto :end
	)
	goto :end
)
if "%Custom%"=="H" ( 
	if "%WWAN%"=="1" ( 
		echo CT40HC-1_Option.ini > %OptionFile%
		goto :end
	)
	if "%WWAN%"=="0" ( 
		echo CT40HC-0_Option.ini > %OptionFile%
		goto :end
	)
	goto :end
)
goto :end

:CT40P
::CT40P-L1N-28R11BF
set WWAN=%sku:~7,1%
echo WWAN flag %WWAN%
if "%WWAN%"=="1" ( 
	echo CT40P-1_Option.ini > %OptionFile%
	goto :end
)
if "%WWAN%"=="0" ( 
	echo CT40P-0_Option.ini > %OptionFile%
	goto :end
)
goto :end

:EDA52
::EDA52-00AE32N11CK
set WWAN=%sku:~6,1%
echo WWAN flag %WWAN%
if "%WWAN%"=="1" (
	echo EDA52-1_Option.ini > %OptionFile%
	goto :end
)
if "%WWAN%"=="0" (
	echo EDA52-0_Option.ini > %OptionFile%
	goto :end
)
goto :end


:EDA5S
::EDA5S-11AE64N21CK
set WWAN=%sku:~6,1%
echo WWAN flag %WWAN%
if "%WWAN%"=="1" (
	echo EDA5S-1_Option.ini > %OptionFile%
	goto :end
)
if "%WWAN%"=="0" (
	echo EDA5S-0_Option.ini > %OptionFile%
	goto :end
)		 
goto :end


:EDA56
::EDA56-00AE61N11CK
set WWAN=%sku:~6,1%
echo WWAN flag %WWAN%
if "%WWAN%"=="1" (
	echo EDA56-1_Option.ini > %OptionFile%
	goto :end
)
if "%WWAN%"=="0" (
	echo EDA56-0_Option.ini > %OptionFile%
	goto :end
)		 
goto :end


:UL
::UL-11A081011CK
::UL-11A081011RK
::UL-11A081012RK								
::UL-00A081011RK
set WWAN=%sku:~3,2%
echo WWAN flag %WWAN%
if "%WWAN%"=="11" (
	echo UL-1_Option.ini > %OptionFile%
	goto :end
)
if "%WWAN%"=="00" (
	echo UL-0_Option.ini > %OptionFile%
	goto :end
)		 
goto :end


:end
echo ***************End***************
timeout /t 3
pause