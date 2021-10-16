<img align="right" width="30%" src="https://github.com/patbec/Sinedo/raw/master/src/Sinedo/wwwroot/images/clouds.svg" alt="Clouds"/>

# Welcome

A small application to download files from sharehosters like Rapidgator. This application is made to run on your own Linux home server. The interface is designed to be used from a PC, tablet or mobile phone.
This software is a beta that is in the testing phase and may still contain bugs. 

<img src="https://github.com/patbec/Sinedo/raw/master/screenshots/screencapture-desktop-title.png" alt="Screenshot Banner"/>

<p align="right">
    <a href="https://github.com/patbec/Sinedo/blob/master/Screenshots.md">See more images</a>
</p>

## Installation

The software can be installed with the following command after downloading:

```
# Debian and Ubuntu:
sudo apt install ./sinedo.<version>.deb

# Red Hat Enterprise Linux:
sudo yum install -y ./sinedo.<version>.rpm
```

**Recommended:** To run the application with limited privileges, open the service file with the command `sudo nano /etc/systemd/system/sinedo.service` and enter the value `User=youruser` with your user name after a new line behind `[Service]`. Enter the command `systemctl daemon-reload` to complete the process. This will run the application as a normal user with limited privileges. Additionally, for safety, this application should not run with administrator privileges. ([Click here for an example](https://github.com/patbec/Sinedo/wiki/Service-File))

```
# Enable autostart
sudo systemctl enable sinedo

# Start the service
sudo systemctl start sinedo
```

The application is installed in the path `/usr/share/sinedo`, the settings can be easily specified later in the interface. These are located in the user folder under: `~/.config/sinedo/config.json`.


To check if the service is running correctly:

```
# View status and logs
systemctl status sinedo.service
```

Enter the hostname of the server with the **port 2222** in the browser to open the interface.
```
Example:
http://<hostname or ip>:2222
http://ubuntu-mini-server:2222
```
Then follow the setup to configure the application, after that you are ready to go! :)

In case of problems with paths, e.g. the download folder remains empty, the current path settings can be viewed at this url:
```
http://<hostname or ip>:2222/api/debug
```

## Features

The application supports **Click&Load**, which originally comes from the *JDownloader 2* application. In order to use this feature, the Click&Load requests must be redirected **to the server**.
The extension Redirect Click'n'Load is available for [Firefox](https://addons.mozilla.org/de/firefox/addon/redirect-click-n-load/), [Edge](https://chrome.google.com/webstore/detail/redirect-clicknload/hnjbnefgkiickkpfidpnlmcodicfgakk) and [Google Chrome](https://chrome.google.com/webstore/detail/redirect-clicknload/hnjbnefgkiickkpfidpnlmcodicfgakk).
After installing the extension, open the options page and enter the same data for **hostname and port** that is used in the browser.

Links can also be downloaded by saving them directly to the **Download directory**.
Currently text files (only with the extension .txt) are supported, these must contain links with line breaks from the Rapidgator service.
Support for DLC files is planned for later.

## Web-Api

To control downloads with other programs or add new ones in an automated way there is a REST api. Examples of how to use it can be found here: [Documentation REST-API](https://github.com/patbec/Sinedo/blob/master/REST-Api.md).

Code contributions or criticism are welcome.

## Build from Source

As development environment **Visual Studio Code** is used, the project can be opened via the file `Sinedo Workspace.code-workspace`.

To build the source code manually there is the option **dotnet Build**, under build tasks.
The file `tasks.json` contains the exact commands to perform the build via the terminal.

## Author

* **Patrick Becker** - [GitHub](https://github.com/patbec)

E-Mail: [git.bec@outlook.de](mailto:git.bec@outlook.de)

## Languages

The translation of the app is browser dependent, currently the app is available in German and English.

## License

This project is licensed under the GPL2 license - See the [LICENSE](https://github.com/patbec/Sinedo/blob/master/LICENSE) file for more information.

## tl;dr
```
# Install app
sudo apt install ./sinedo_<version>.deb

# Enable autostart
sudo systemctl enable sinedo

# Start the service
sudo systemctl start sinedo

# View status and logs
systemctl status sinedo.service

# Open page
http://<hostname or ip>:2222
```