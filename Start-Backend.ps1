param([switch]$Mock,[switch]$Lan)
$ErrorActionPreference='Stop'
$env:PHOTOAR_MODE=if($Mock){'mock'}else{'real'}
$env:PHOTOAR_HOST=if($Lan){'0.0.0.0'}else{'127.0.0.1'}
$env:PHOTOAR_DATA=Join-Path $PSScriptRoot 'backend\data'
& 'E:\conda-envs\triposr311\python.exe' (Join-Path $PSScriptRoot 'backend\service.py')
