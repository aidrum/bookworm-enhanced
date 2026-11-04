cd /d "%~dp0.."
call distribution\msvc_env.bat || exit /b 1
dotnet publish Bookworm.Desktop -c Release -r win-x64 -p:PublishAot=true -p:IlcUseEnvironmentalTools=true -o Bookworm.Desktop\bin\Publish\Bookworm
