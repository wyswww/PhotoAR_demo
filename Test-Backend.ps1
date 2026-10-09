param([switch]$Real)
$ErrorActionPreference='Stop'
Push-Location (Join-Path $PSScriptRoot 'backend')
try {
    if($Real){$env:PHOTOAR_MODE='real'; & 'E:\conda-envs\triposr311\python.exe' test_real.py}
    else {
        & 'E:\conda-envs\triposr311\python.exe' -m unittest test_service -v
        if($LASTEXITCODE -ne 0){throw 'Service tests failed'}
        & 'E:\conda-envs\triposr311\python.exe' test_http.py
    }
    if($LASTEXITCODE -ne 0){throw 'Backend tests failed'}
}finally {Pop-Location}
