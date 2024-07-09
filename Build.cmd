@echo off
pushd %~dp0

set program-path=%ProgramFiles(x86)%
if not exist "%program-path%" set program-path=%ProgramFiles%
set msbuild="%program-path%\Microsoft Visual Studio\2017\BuildTools\MSBuild\15.0\Bin\MSBuild.exe"

set log-dir=build\BuildOutput\log
set solution=SharedSource-Build.sln

if not [%1]==[] goto %1
	
echo Welcome to the re-motion build tool!
echo.
echo Using %msbuild%
echo.
echo Choose your desired build:
echo [1] ... Test build ^(x86-debug^)
echo [2] ... Full build ^(x86-debug/release, x64-debug/release, create packages^)
echo [3] ... Package for development ^(create NuGet packages in .\Build\BuildOutput^ with development version number)
echo [4] ... Package for release ^(create NuGet packages in .\Build\BuildOutput^)
echo [5] ... Oops, nothing please - exit.
echo.

choice /c:123456 /n /m "Your choice: "

if %ERRORLEVEL%==1 goto run_test_build
if %ERRORLEVEL%==2 goto run_full_build
if %ERRORLEVEL%==3 goto run_pkg_development_build
if %ERRORLEVEL%==4 goto run_pkg_official_build
if %ERRORLEVEL%==5 goto run_exit
goto build_succeeded

:run_test_build
mkdir %log-dir%
%msbuild% %solutionFile% /t:restore /p:RestorePackagesPath=packages
%msbuild% build\Remotion.Local.build /t:TestBuild /maxcpucount /verbosity:normal /flp:verbosity=normal;logfile=build\BuildOutput\log\build.log
if not %ERRORLEVEL%==0 goto build_failed
goto build_succeeded

:run_full_build
mkdir %log-dir%
%msbuild% %solutionFile% /t:restore /p:RestorePackagesPath=packages
%msbuild% build\Remotion.Local.build /t:FullBuildWithoutDocumentation /maxcpucount /verbosity:normal /flp:verbosity=normal;logfile=build\BuildOutput\log\build.log
if not %ERRORLEVEL%==0 goto build_failed
goto build_succeeded

:run_pkg_development_build
mkdir %log-dir%
%msbuild% %solutionFile% /t:restore /p:RestorePackagesPath=packages
%msbuild% build\Remotion.Local.build /t:PackageBuild /maxcpucount /verbosity:minimal /flp:verbosity=normal;logfile=build\BuildOutput\log\build.log
if not %ERRORLEVEL%==0 goto build_failed
goto build_succeeded

:run_pkg_official_build
mkdir %log-dir%
%msbuild% %solutionFile% /t:restore /p:RestorePackagesPath=packages
%msbuild% build\Remotion.Local.build /t:PackageBuild /p:UseDevelopmentVersion=False /maxcpucount /verbosity:minimal /flp:verbosity=normal;logfile=build\BuildOutput\log\build.log
if not %ERRORLEVEL%==0 goto build_failed
goto build_succeeded

:run_exit
exit /b 0


:build_failed
echo.
echo Building %solution% has failed.
start build\BuildOutput\log\build.log
pause
popd
exit /b 1

:build_succeeded
echo.
pause
popd
exit /b 0
