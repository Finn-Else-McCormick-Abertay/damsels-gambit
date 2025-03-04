@echo off
if EXIST "Godot.exe" ( del "Godot.exe" )
if EXIST "GodotSharp" ( rmdir /s /q "GodotSharp" )
echo Downloading Godot v4.4 .NET...
powershell -Command "$ProgressPreference = 'SilentlyContinue'; Invoke-WebRequest https://github.com/godotengine/godot/releases/download/4.4-stable/Godot_v4.4-stable_mono_win64.zip -OutFile Godot.zip"
echo Unzipping...
powershell -Command "$ProgressPreference = 'SilentlyContinue'; Expand-Archive Godot.zip"
move /Y Godot\Godot_v4.4-stable_mono_win64\GodotSharp .
move /Y Godot\Godot_v4.4-stable_mono_win64\Godot_v4.4-stable_mono_win64.exe .
rmdir /S /Q "Godot"
del "Godot.zip"
ren Godot_v4.4-stable_mono_win64.exe Godot.exe