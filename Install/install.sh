#!/bin/bash
# This script will build and install the DynamicIPTables project

SELF_PATH=$(dirname "$(readlink -f "$0")")
name="dynamic-iptables"
user=dynamiciptables
sudoers="/etc/sudoers.d/${user}"
framework="net8.0"
runtime="linux-x64"
project_path="${SELF_PATH}/../DynamicIPTables"
project_etc="${project_path}/etc/${name}"
project="${project_path}/DynamicIPTables.csproj"
output="${SELF_PATH}/bin/Release/$framework/$runtime/publish"
logdir=/var/log/$name
install_dir=/opt/$name

echo ""
echo "************************************************"
echo "* Starting the installation of DynamicIPTables *"
echo "************************************************"
echo ""

echo "Checking if dotnet is installed"

dotnet_verison=$(dotnet --version 2>&1 > /dev/null)

if [ $? -ne 0 ]; then
    echo "Dotnet is not installed or not in the PATH"
    echo "Please install dotnet before running this script"
    exit 1
fi

echo "Dotnet is installed and version is ${dotnet_verison}"

runuser=$(whoami)

# Check if the script is run as root
if [ $runuser != "root" ]; then
    echo "Please run this script as root"
    exit 1
fi

echo "Removing the previous build from the directory $SELF_PATH/bin/Release/"

rm -r $SELF_PATH/bin/Release/

echo "Building the project to directory ${output}"

dotnet publish $project -f $framework -r $runtime \
               -c Release \
               -o $output \
               --self-contained true \
               /p:PublishSingleFile=true \
               /p:IncludeNativeLibrariesForSelfExtract=true

if [ $? -ne 0 ]; then
    echo "Failed to build the project"
    exit 1
fi

if id -u $user &>/dev/null; then

   echo "User $user already exists, going to delete it"
   userdel $user

    if [ $? -ne 0 ]; then
         echo "Failed to delete user $user"
         exit 1
    fi
fi

echo "Creating user $user with no login shell"

useradd -r -M -U -s /sbin/nologin $user

if [ $? -ne 0 ]; then
    echo "Failed to create user $user"
    exit 1
fi

if [ -f $sudoers ]; then
    echo "Sudoers file exists ${sudoers}, going to delete"
    rm $sudoers
fi

echo "Create sudoers file for user ${user}"

echo "${user} ALL=(root) NOPASSWD: /usr/sbin/iptables, /usr/sbin/ip6tables, /usr/sbin/ipset" > $sudoers

echo "Create instalation directory if not exist $install_dir and owner user $user"

if [ ! -d $install_dir ]; then
    
    mkdir -p $install_dir
    chown -R $user:$user $install_dir

    if [ $? -ne 0 ]; then
        echo "Failed to create $install_dir directory"
        exit 1
    fi

fi

echo "Going to copy the binary to ${install_dir}"
cp -r $output/* $install_dir

if [ $? -ne 0 ]; then
    echo "Failed to copy the binary to $install_dir"
    exit 1
fi

# Copy the service file to the systemd directory
cp $SELF_PATH/${name}.service /etc/systemd/system/

if [ $? -ne 0 ]; then
    echo "Failed to copy the service file to /etc/systemd/system/"
    exit 1
fi

echo "Reload the systemd daemon"
systemctl daemon-reload

if [ $? -ne 0 ]; then
    echo "Failed to reload the systemd daemon"
    exit 1
fi

# Create /etc/dynamic-iptables directory if not exist
if [ ! -d /etc/${name} ]; then

    mkdir /etc/${name}

    if [ $? -ne 0 ]; then
        echo "Failed to create /etc/${name} directory"
        exit 1
    fi

fi

# Create /etc/dynamic-iptables/config.conf
if [ ! -f /etc/${name}/${name}.conf ]; then
    cp ${project_etc}/${name}.conf /etc/${name}/
else
    cp ${project_etc}/${name}.conf /etc/${name}/${name}.new
fi

if [ $? -ne 0 ]; then
    echo "Failed to create /etc/${name}/${name}.conf"
    exit 1
fi

# Create if not exits /etc/dynamic-iptables/rules.d directory
if [ ! -d /etc/${name}/rules.d ]; then
    mkdir /etc/${name}/rules.d
fi

# Copy the rules to /etc/dynamic-iptables/rules.d if directory is empty
if [ ! "$(ls -A /etc/${name}/rules.d)" ]; then
    cp ${project_etc}/rules.d/letsencrypt-exmaple.conf /etc/${name}/rules.d/
fi

# Chamod of /etc/dynamic-iptables/ to only writable by root but view others
chmod 755 -R /etc/${name}

echo "Creating log directory $logdir"
if [ ! -d $logdir ]; then
    
        mkdir $logdir

    else
        echo "Log directory $logdir already exists"
        echo "Going to delete the content of the directory"
        rm -rf $logdir/*
fi

if [ $? -ne 0 ]; then
    echo "Failed to create log directory $logdir"
    exit 1
fi

echo "Changing owner of $logdir to $user"
chown -R $user:$user $logdir

echo "DynamicIPTables has been installed successfully"
exit 0