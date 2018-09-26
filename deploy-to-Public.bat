SET BUILD_DIR=.\bin
SET LOCAL_DIR=\\fcafiles\Public\Computers\Projects\IntegratorEduTeacherNotify
copy /y "%BUILD_DIR%\data_path.txt" "%LOCAL_DIR%\"
if NOT ["%errorlevel%"]==["0"] pause
copy /y "install-from-Public.bat" "%LOCAL_DIR%\"
if NOT ["%errorlevel%"]==["0"] pause
copy /y "%BUILD_DIR%\IntegratorEduTeacherNotify.exe" "%LOCAL_DIR%\"
if NOT ["%errorlevel%"]==["0"] pause
copy /y "defaults\HKCU-Run-IntegratorEduTeacherNotify.reg" "%LOCAL_DIR%\"
REM see also HKLM-Run-IntegratorEduTeacherNotify.reg in \\fcafiles\Public\Computers\Projects\IntegratorEduTeacherNotify\
explorer "%LOCAL_DIR%"
