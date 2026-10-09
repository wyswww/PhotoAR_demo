param([string]$LicenseFile = 'C:\Users\19554\Desktop\License Key.txt')
$ErrorActionPreference='Stop'
$projectPath=Join-Path $PSScriptRoot '..\Unity'
if(!(Test-Path -LiteralPath $LicenseFile)){throw 'License file not found.'}
$env:PHOTOAR_EASYAR_KEY_FILE=(Resolve-Path -LiteralPath $LicenseFile).Path
& 'E:\Unity\6000.3.19f1\Editor\Unity.exe' -batchmode -nographics -quit -buildTarget Android -projectPath $projectPath -executeMethod EasyARBuild.Build -logFile (Join-Path $PSScriptRoot '..\reports\easyar-build.log')
# Close this project's Unity Editor before running. Alternatively use the PhotoAR/EasyAR menu in that editor.
