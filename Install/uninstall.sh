#!/bin/bash
# This script will unistall the DynamicIPTables project

runuser=$(whoami)

if [ $runuser != "root" ]; then
    echo "Please run this script as root"
    exit 1
fi


# Get self path
name="dynamic-iptables"
user=dynamiciptables
sudoers="/etc/sudoers.d/${user}"
logdir=/var/log/$name
install_dir=/opt/$name

echo "Stopping the service if running"

systemctl stop ${name}.service 2&>1 /dev/null

echo "Disabling the service if enabled"

systemctl disable ${name}.service 2&>1 /dev/null

echo "Removing the directory if exist $install_dir"

if [ -d $install_dir ]; then
    rm -r $install_dir
fi

echo "Removing the directory if exist $logdir"

if [ -d $logdir ]; then
    rm -r $logdir
fi

echo "Removing the directory id exist /etc/$name"

if [ -d /etc/$name ]; then
    rm -r /etc/$name
fi

echo "Removing file if exist /etc/systemd/system/${name}.service"

if [ -f /etc/systemd/system/${name}.service ]; then
    rm /etc/systemd/system/${name}.service
fi

echo "Reload the systemd daemon"

systemctl daemon-reload

if [ -f $sudoers ]; then
    echo "Delete sudoers file ${sudoers}"
    rm $sudoers
fi

echo "Remove user and group if exist dynamiciptables"

if id -u $user &>/dev/null; then
    userdel $user
fi

echo "Uninstall dynamic-iptable complete"

exit 0