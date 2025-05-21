#!/bin/bash

# Set variables
APP_VERSION="0.0.19-dev"
APP_NAME="FileSyncApp"
APP_DIR="/opt/$APP_NAME"
SERVICE_FILE="$APP_NAME.service"
SW_DESCRIPTION="sw-description"
OUTPUT_DIR="FileSyncAppPackage"
PUBLISH_DIR="publish"

dotnet publish -o $PUBLISH_DIR -c Release -p:PublishSingleFile=true -p:PublishTrimmed=false -p:Version=$APP_VERSION -r linux-x64 --self-contained ./FileSyncApp.csproj
# Create directory structure

mkdir -p ${OUTPUT_DIR}
mount -t ramfs ramfs ${OUTPUT_DIR}

# Create systemd service file
cat <<EOL > ${OUTPUT_DIR}/${SERVICE_FILE}
[Unit]
Description=FileSyncApp - Datei-Synchronisation
After=network.target

[Service]
ExecStart=$APP_DIR/$APP_NAME
WorkingDirectory=$APP_DIR
Restart=on-failure
User=root
Environment=DOTNET_EnableDiagnostics=0

[Install]
WantedBy=multi-user.target
EOL

# Create sw-description filex
cat <<EOL > ${OUTPUT_DIR}/${SW_DESCRIPTION}
software =
{
    version = "$APP_VERSION";
    description = "FileSyncApp Deployment";
    bootloader_transaction_marker = false;
    bootloader_state_marker = false;
    hardware-compatibility: [ "#RE:.*" ];
    files: (
        {
            filename = "$APP_NAME";
            path = "$APP_DIR/$APP_NAME";
        },
        {
            filename = "$APP_NAME.service";
            path = "/etc/systemd/system/$SERVICE_FILE";
        }
    );
    scripts: (
        {
            filename = "update.sh";
            type = "shellscript";
        }
    );
    preinstall = " || true && mkdir -p /opt/$APP_NAME";
    postinstall = "systemctl daemon-reexec && systemctl daemon-reload && systemctl enable --now $APP_NAME.service";
};
EOL
cat <<\EOFUPDATE > ${OUTPUT_DIR}/update.sh
#!/bin/sh

if [ $# -lt 1 ]; then
    exit 0;
fi
if [ $1 = "preinst" ]; then
    systemctl stop FileSyncApp.service
    mkdir -p /opt/FileSyncApp
    echo "PREINST -> directory created"
fi

if [ $1 = "postinst" ]; then
  chmod +x /opt/FileSyncApp/FileSyncApp
  systemctl daemon-reexec
  systemctl daemon-reload
  systemctl enable --now FileSyncApp.service
fi

EOFUPDATE

# Copy the compiled binary to the package directory
cp ${PUBLISH_DIR}/$APP_NAME ${OUTPUT_DIR}

# Create CPIO archive
cd $OUTPUT_DIR
cpio -H crc -o < <(printf '%s\n' sw-description; find . ! -name sw-description -type f | sort) > ../$APP_NAME-$APP_VERSION.swu
cd ..
umount ${OUTPUT_DIR}
rm -r ${OUTPUT_DIR}

echo "SWUpdate package created: $APP_NAME-$APP_VERSION.swu"
