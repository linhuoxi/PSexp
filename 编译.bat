@echo off
chcp 65001 >nul
setlocal EnableExtensions EnableDelayedExpansion
cd /d "%~dp0"

rem 纯编译脚本：自测 + 发布，产物写入系统临时目录，不碰本机任何已安装文件。
rem 不替换 EXE、不上传、不混淆；这些属于作者的私有发布流程，不在本脚本职责内。

set "PROJECT=PSdodo\PSdodo.csproj"
set "TEST_PROJECT=tests\PSdodo.Tests\PSdodo.Tests.csproj"
set "BUILD_ROOT=%TEMP%\PSexp-compile"
set "ARTIFACTS_DIR=%BUILD_ROOT%\artifacts"
set "PUBLISH_DIR=%ARTIFACTS_DIR%\publish\PSdodo\release_win-x64"
set "TEST_EXE=%ARTIFACTS_DIR%\bin\PSdodo.Tests\release\PSdodo.Tests.exe"

rem 产物文件名取自 csproj 的 AssemblyName，避免同一个名字写两处。
set "ASSEMBLY_NAME="
for /f "tokens=3 delims=<>" %%A in ('findstr /i /c:"<AssemblyName>" "%PROJECT%"') do set "ASSEMBLY_NAME=%%A"
if not defined ASSEMBLY_NAME goto :csproj_unreadable

echo =========================================
echo  PSexp 编译与自测
echo =========================================
echo.

where dotnet >nul 2>nul
if errorlevel 1 goto :dotnet_missing

for /f "tokens=1 delims=." %%V in ('dotnet --version') do set "DOTNET_MAJOR=%%V"
if not defined DOTNET_MAJOR goto :dotnet_missing
if %DOTNET_MAJOR% LSS 10 goto :dotnet_too_old

echo [1/2] 正在编译并运行自动化自测...
if exist "%BUILD_ROOT%" rmdir /s /q "%BUILD_ROOT%"
mkdir "%BUILD_ROOT%" >nul 2>nul
if errorlevel 1 goto :build_dir_failed

dotnet build "%TEST_PROJECT%" -c Release --nologo --artifacts-path "%ARTIFACTS_DIR%" -p:PublishSingleFile=false -p:EnableReleaseObfuscation=false -p:PublishTrimmed=false
if errorlevel 1 goto :test_failed
if not exist "%TEST_EXE%" goto :test_failed
"%TEST_EXE%"
if errorlevel 1 goto :test_failed

echo.
echo [2/2] 正在发布 Windows x64 自包含单文件程序...
dotnet publish "%PROJECT%" -c Release -r win-x64 --self-contained true --nologo --artifacts-path "%ARTIFACTS_DIR%" -p:PublishDir="%PUBLISH_DIR%/" -p:EnableReleaseObfuscation=false -p:PublishTrimmed=false
if errorlevel 1 goto :publish_failed
if not exist "%PUBLISH_DIR%\%ASSEMBLY_NAME%.exe" goto :output_missing

echo.
echo =========================================
echo  产物：%PUBLISH_DIR%\%ASSEMBLY_NAME%.exe
echo  自包含，终端用户无需安装 .NET
echo  中间产物留在临时目录，删掉即可
echo =========================================
exit /b 0

:dotnet_missing
set "ERROR_MESSAGE=未找到 dotnet 命令；编译需要 .NET 10 SDK，运行产物不需要。"
goto :failed

:dotnet_too_old
set "ERROR_MESSAGE=dotnet SDK 版本过低，需要 .NET 10 或更高。"
goto :failed

:csproj_unreadable
set "ERROR_MESSAGE=无法从 %PROJECT% 读取 AssemblyName 标签。"
goto :failed

:build_dir_failed
set "ERROR_MESSAGE=无法在系统临时目录创建 %BUILD_ROOT%。"
goto :failed

:test_failed
set "ERROR_MESSAGE=自动化自测未通过或测试程序未生成。"
goto :failed

:publish_failed
set "ERROR_MESSAGE=发布失败，请查看上方 dotnet 输出。"
goto :failed

:output_missing
set "ERROR_MESSAGE=发布命令未生成 %ASSEMBLY_NAME%.exe。"
goto :failed

:failed
echo.
echo [错误] %ERROR_MESSAGE%
if /i not "%PSexpNoPause%"=="1" pause
exit /b 1
