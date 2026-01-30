#!/bin/bash

# Exit on error
set -e

# ------------------- CONFIGURATION -------------------
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PEM_PATH="$SCRIPT_DIR/ubuntu.pem"
REMOTE_USER="ubuntu"
REMOTE_HOST="ec2-18-222-198-24.us-east-2.compute.amazonaws.com"
REMOTE_PATH="/var/www/bbbAPIGL/publish"
TEMP_PATH="/home/ubuntu/publish_tmp"

CONFIGURATION="Release"
OUTPUT_FOLDER="$SCRIPT_DIR/dist"

# ------------------- VALIDATIONS -------------------
if [ ! -f "$PEM_PATH" ]; then
    echo -e "\e[31mError: No se encontró la clave PEM en: $PEM_PATH\e[0m"
    exit 1
fi

# ------------------- BUILD & PUBLISH -------------------
echo -e "\e[36m=== Limpiando y preparando publicación local ===\e[0m"
rm -rf "$OUTPUT_FOLDER"
rm -rf "$SCRIPT_DIR/publish"

echo -e "\e[36m=== Compilando y publicando proyecto ===\e[0m"
dotnet publish "$SCRIPT_DIR/bbbAPIGL.csproj" -c "$CONFIGURATION" -o "$OUTPUT_FOLDER"

# ------------------- PREPARE REMOTE TEMP -------------------
echo -e "\n\e[36m=== Preparando directorio temporal remoto ===\e[0m"
ssh -o StrictHostKeyChecking=no -i "$PEM_PATH" "${REMOTE_USER}@${REMOTE_HOST}" "rm -rf $TEMP_PATH && mkdir -p $TEMP_PATH"

# ------------------- DEPLOY VIA SCP -------------------
echo -e "\n\e[36m=== Subiendo archivos al servidor ===\e[0m"
# Copiamos el CONTENIDO de dist al temporal
scp -o StrictHostKeyChecking=no -i "$PEM_PATH" -r "$OUTPUT_FOLDER"/* "${REMOTE_USER}@${REMOTE_HOST}:${TEMP_PATH}"

# ------------------- MOVE & CHOWN AS ROOT (SUDO) -------------------
echo -e "\n\e[36m=== Despliegue final, permisos y reinicio de servicios ===\e[0m"

REMOTE_COMMANDS="sudo mkdir -p ${REMOTE_PATH} && \
sudo rm -rf ${REMOTE_PATH}* && \
sudo mkdir -p ${REMOTE_PATH} && \
sudo cp -r ${TEMP_PATH}/* ${REMOTE_PATH}/ && \
sudo chown -R www-data:www-data ${REMOTE_PATH} && \
sudo rm -rf ${TEMP_PATH} && \
sudo systemctl daemon-reload && \
sudo systemctl restart kestrel-bbbapigl.service && \
sudo systemctl restart nginx"

ssh -o StrictHostKeyChecking=no -i "$PEM_PATH" "${REMOTE_USER}@${REMOTE_HOST}" "$REMOTE_COMMANDS"

echo -e "\n\e[32m¡DESPLIEGUE COMPLETO!\e[0m"
echo "- Archivos en: $REMOTE_PATH"
echo "- Servicio kestrel-bbbapigl reiniciado."
echo "- Nginx reiniciado."
exit 0
