@ECHO OFF
@SETLOCAL
@SETLOCAL ENABLEDELAYEDEXPANSION

:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
::
:: Description:
::     This script is a convenience for building all the 
::     flavors of codegen and the Canvas solution (all 
::     platforms and configurations), all in one step. Runs 
::     the tests as well.
::
:: Usage:
::     BuildAndTestAllConfigs.cmd [path]
::
::     where [path] is the absolute path to your 
::     local Git repository. This parameter is optional.
::
::     If [path] is not specified, the script will infer
::     your Git repository path from its location (works
::     so long as it hasn't been copied someplace else).
::
::
:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

WHERE /Q msbuild >NUL
IF %ERRORLEVEL% NEQ 0 ( 
    ECHO Error: It appears that 'msbuild' is not available in this environment. 
    ECHO Please run this script from a Visual Studio command prompt, or 
    ECHO ensure the PATH is configured correctly.
    GOTO END
)

IF "%1"=="/?" (
    ECHO Usage: %0 [path]
    ECHO    optional parameter [path] is the path to your local Git repository.
    ECHO    Example: BuildAndTestAllConfigs.cmd C:\Src
    GOTO END
)

SET GIT_TREE_LOCATION=%~dp0..\

IF NOT "%1"=="" (
   SET GIT_TREE_LOCATION=%1
)

set BIN_LOCATION=%GIT_TREE_LOCATION%\bin\

msbuild /maxcpucount %GIT_TREE_LOCATION%\canvas.proj
IF !ERRORLEVEL! NEQ 0 ( 
    ECHO.
    ECHO A build error occurred.
    GOTO END
)

:: tools\tools.sln
:: The tools solution is intended to build on one platform, Any CPU. 
for %%C in (Debug Release) DO (
    
    SET TEST_BINARY_DIR=%BIN_LOCATION%\WindowsAnyCPU\%%C\codegen.test\
    SET TEST_BINARY_PATH=!TEST_BINARY_DIR!codegen.test.dll
    
    mstest /testcontainer:!TEST_BINARY_PATH!
    
    IF !ERRORLEVEL! NEQ 0 ( 
            ECHO.
            ECHO One or more tests have failed.
            GOTO END
    )
)

:: CANVAS.sln
:: TODO: Enable tests for this solution. The Canvas tests are a bit different from the codegen ones in that
:: they require being run in an AppContainer. See task #1053.

ECHO.
ECHO All builds succeeded. All tests succeeded.

:END

@ENDLOCAL
