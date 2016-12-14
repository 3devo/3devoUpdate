3Devo Filament extruder firmware update tool
============================================
This tool can be used to update the firmware of a 3Devo Next 1.0
Filament Extruder using a Windows computer. For updating using a Linux
or OSX computer, see the instructions further down.

To use this tool, extract all files from the zipfile to a convenient
location. You should have the following files:
 - 3devo serial driver.inf
 - 3devoUpdate.exe
 - avrdude.exe
 - avrdude.conf
 - libusb0.dll
 - README.txt


Updating the firmware requires two steps:

Step 1: Driver install
----------------------
To install the driver:
 - If you use Windows 10, enable test mode, or disable driver signature
   enforcement as explained at:
   http://www.howtogeek.com/167723/how-to-disable-driver-signature-verification-on-64-bit-windows-8.1-so-that-you-can-install-unsigned-drivers/
 - right click the '3devo serial driver' file
 - click 'Install'

No confirmation is shown, but if the driver is succesfully
installed, a popup should appear in the lower right saying "Next 1.0
filament extruder (COM..) installed" when the filament extruder is next
plugged into USB.

Installing the driver only needs to happen once, so you can skip this
step when later updating to another firmware version.


Step 2: Firmware upload
-----------------------
Start the updating tool doubleclicking the '3devoUpdate.exe' (or just
'3devoUpdate') executable file.

To start the upload:
 - Select the (virtual) serial port corresponding to the filament
   extruder. This list is automatically filtered, so usually it will
   contain just one entry.
 - Select the firmware file to upload. This is a .hex file that should
   be separately supplied.
 - Click upload



After this, the software update is complete. The rest of this file contains
instructions for Linux and OSX only.









Using Linux or OSX
==================
For Linux and OSX, no graphical tool is available, but the firmware
upload can be done using a terminal. Note that these instructions were
not tested on OSX.


Serial port
-----------
Unlike on Windows, no serial drivers installation is needed. Linux and
OSX should create the virtual serial port used by the filament extruder
automatically.

To find out the name of the serial port, look for a device file named
like `/dev/ttyACM0` (Linux) or `/dev/cu.usbserial123` (OSX). Running the
`dmesg` command shortly after plugging in the USB cable can also help to
find the device name.


Getting avrdude
---------------
The `avrdude` program is used to actually upload the firmware and must
be installed. Most Linux distributions will offer an `avrdude` package
that will work (`avrdude` version 6.1 or higher is needed).

Alternatively, you can use the `avrdude` version supplied with the
Arduino software. To use it, extract a zipfile or tarball downloaded
from https://www.arduino.cc/en/Main/Software, find the `avrdude` program
inside it and adapt the below command to point to it. You will probably
also need to add the `-C` option to the below command to point to the
`avrdude.conf` file. To find these, start the Arduino IDE, enable
verbose upload in the preferences and click "upload" to see both the
path to `avrdude` as well as the `-C` option to use.



Running avrdude
---------------
The actual upload is done using the avrdude command. In a terminal, run
a command like:

    avrdude -p atmega2560 -b 115200 -carduino -P/dev/ttyACM0 -D -Uflash:w:FeFirmware-v1.0.0.hex:i -v

Where `/dev/ttyACM0` is replaced with the appropriate serial port,
`FeFirmware-v1.0.0.hex` with the path to the firmware file to upload. If
needed, update the `avrdude` part with the path to the avrude binary.

Software licenses
=================
The 3devoUpdate tool is based on the avrdudess software, and contains
compiled versions of some third-party software. This section contains
information regarding the copyright and licensing of this software.


3devoUpdate
-----------
3devoUpdate itself is based on the avrdudess software by Zak Kemble from
https://github.com/zkemble/AVRDUDESS. It is licensed under the GPLv3.
The source code and full license terms can be found at
https://github.com/3devo/3devoUpdate or can be requested from
service@3devo.eu.


avrdude
-------
A copy of avrdude.exe is distributed along with this tool. It is a
verbatim copy taken from the zipfile downloaded from the avrdude project
at http://download.savannah.gnu.org/releases/avrdude/avrdude-6.1-mingw32.zip
and is licensed under the GPLv2.

The source code and full license terms can be found at
http://download.savannah.gnu.org/releases/avrdude/


libusb
------
A copy of libusb0.dll is distributed along with this tool. It is a
verbatim copy taken from the zipfile downloaded from the libusb project
at http://downloads.sourceforge.net/libusb-win32/libusb-win32-bin-1.2.6.0.zip
and is licensed under the LGPLv3.

The source code and full license terms can be found at
http://downloads.sourceforge.net/libusb-win32/libusb-win32-src-1.2.6.0.zip

