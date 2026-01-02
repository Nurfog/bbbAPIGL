<#
.SYNOPSIS
    Compila y publica la aplicación ASP.NET Core y la envía al servidor remoto vía SSH.

.DESCRIPTION
    1. Ejecuta `dotnet publish -c Release -o ./publish`.
    2. Usa `scp` (parte del cliente OpenSSH) para copiar recursivamente el contenido
       de la carpeta `publish` al directorio remoto `/var/www/bbbAPIGL/publish`.
    3. Opcionalmente muestra logs detallados y devuelve un código de salida.

.REQUIREMENTS
    - OpenSSH client must be installed (comes with Windows 10+ or WSL).
    - `ubuntu.pem` must be present in the same folder as this script
      (or edit `$PemPath` to point to its location).
    - El usuario remoto debe tener permisos de escritura en `/var/www/bbbAPIGL/publish`.

.NOTES
    No se copia la clave al servidor; solo se usa para autenticarse.
#>

# ------------------- CONFIGURATION -------------------
$PemPath   = Join-Path -Path $PSScriptRoot -ChildPath "ubuntu.pem"
$RemoteUser = "ubuntu"
$RemoteHost = "ec2-18-222-198-24.us-east-2.compute.amazonaws.com"
$RemotePath = "/var/www/bbbAPIGL/publish"
$TempPath   = "/home/ubuntu/publish_tmp"

$Configuration = "Release"
# Usamos 'dist' en lugar de 'publish' para evitar confusiones de nombres y recursividad
$OutputFolder  = Join-Path -Path $PSScriptRoot -ChildPath "dist"

# ------------------- VALIDATIONS -------------------
if (-not (Test-Path $PemPath)) {
    Write-Error "No se encontró la clave PEM en: $PemPath"
    exit 1
}

# ------------------- BUILD & PUBLISH -------------------
Write-Host "=== Limpiando y preparando publicación local ===" -ForegroundColor Cyan
if (Test-Path $OutputFolder) { Remove-Item -Recurse -Force $OutputFolder }
if (Test-Path (Join-Path $PSScriptRoot "publish")) { Remove-Item -Recurse -Force (Join-Path $PSScriptRoot "publish") }

Write-Host "=== Compilando y publicando proyecto === " -ForegroundColor Cyan
dotnet publish bbbAPIGL.csproj -c $Configuration -o $OutputFolder
if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish falló."
    exit $LASTEXITCODE
}

# ------------------- PREPARE REMOTE TEMP -------------------
Write-Host "`n=== Preparando directorio temporal remoto ===" -ForegroundColor Cyan
$cleanCmd = "rm -rf $TempPath && mkdir -p $TempPath"
$sshClean = "ssh -o StrictHostKeyChecking=no -i `"$PemPath`" ${RemoteUser}@${RemoteHost} `"$cleanCmd`""
Invoke-Expression $sshClean

# ------------------- DEPLOY VIA SCP -------------------
Write-Host "`n=== Subiendo archivos al servidor ===" -ForegroundColor Cyan
# Copiamos el CONTENIDO de dist al temporal
$scpCmd = "scp -o StrictHostKeyChecking=no -i `"$PemPath`" -r `"$OutputFolder/*`" ${RemoteUser}@${RemoteHost}:${TempPath}"

Invoke-Expression $scpCmd
if ($LASTEXITCODE -ne 0) {
    Write-Error "scp falló."
    exit $LASTEXITCODE
}

# ------------------- MOVE & CHOWN AS ROOT (SUDO) -------------------
Write-Host "`n=== Despliegue final, permisos y reinicio de servicios === " -ForegroundColor Cyan

# Comandos remotos para:
# 1. Preparar carpeta destino limpia
# 2. Mover archivos y cambiar dueño
# 3. Recargar systemd y reiniciar el servicio de la API
# 4. Reiniciar Nginx para asegurar que el proxy inverso responda bien
$remoteCommands = @(
    "sudo mkdir -p ${RemotePath}",
    "sudo rm -rf ${RemotePath}*",
    "sudo mkdir -p ${RemotePath}",
    "sudo cp -r ${TempPath}/* ${RemotePath}/",
    "sudo chown -R www-data:www-data ${RemotePath}",
    "sudo rm -rf ${TempPath}",
    "sudo systemctl daemon-reload",
    "sudo systemctl restart kestrel-bbbapigl.service",
    "sudo systemctl restart nginx"
) -join " && "

$sshExec = "ssh -o StrictHostKeyChecking=no -i `"$PemPath`" ${RemoteUser}@${RemoteHost} `"$remoteCommands`""

Invoke-Expression $sshExec
if ($LASTEXITCODE -ne 0) {
    Write-Error "Error en los comandos remotos finales (permisos o reinicio de servicios)."
    exit $LASTEXITCODE
}

Write-Host "`n¡DESPLIEGUE COMPLETO!" -ForegroundColor Green
Write-Host "- Archivos en: $RemotePath"
Write-Host "- Servicio kestrel-bbbapigl reiniciado."
Write-Host "- Nginx reiniciado."
exit 0
