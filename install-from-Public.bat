SET SOURCE_DIR=\\fcafiles\Public\Computers\Projects\IntegratorEduTeacherNotify
REM SET LOCAL_DIR=C:\ProgramData\IntegratorEduTeacherNotify
SET PROGRAMS_DIR=C:\ProgramData
SET LOCAL_DIR=%PROGRAMS_DIR%\IntegratorEduTeacherNotify
IF NOT EXIST "LOCAL_DIR" md "%LOCAL_DIR%"
copy /y "%SOURCE_DIR%\IntegratorEduTeacherNotify.exe" "%LOCAL_DIR%\"
copy /y "%SOURCE_DIR%\data_path.txt" "%LOCAL_DIR%\"
copy /y "%SOURCE_DIR%\HKCU-Run-IntegratorEduTeacherNotify.reg" "%LOCAL_DIR%\"
regedit "%LOCAL_DIR%\HKCU-Run-IntegratorEduTeacherNotify.reg"
REM regedit /s would be silent
"%LOCAL_DIR%\IntegratorEduTeacherNotify.exe"