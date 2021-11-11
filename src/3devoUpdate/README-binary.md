3devoUpdate - firmware update tool
============================================
This tool can be used to update the firmware of 3devo products using a Windows
computer. For updating using a Linux or OSX computer, see the instructions further down.

To use this tool, extract all files from the zipfile to a convenient
location. You should have the following files:
 - Drivers v1.0.1.0
   + DFU drivers
     > 3devo-dfu-driver.inf
     > 3devo-dfu-signature.cat
   + Serial drivers
     > 3devo-serial-driver.inf
     > 3devo-serial-signature.cat
 - 3devoUpdate.exe
 - avrdude.exe
 - avrdude.conf
 - dfu-util.exe
 - libusb0.dll
 - libusb-1.0.dll
 - README.txt


Updating the firmware requires two steps:

Step 1: Driver install (Windows 8 and below only)
---------------------------------------------------
Starting from Windows 10, this driver is automatically installed and
this step can be skipped.

To install the driver:
 - Navigate to the `Drivers vx.x.x.x.` folder, `DFU drivers` (for Airid Dryer) and/or
   `Serial drivers` folder (for Filament Extruder or Filament Maker)
 - Right click the '3devo-dfu-driver.inf' and/or `3devo-serial-driver.inf` file
   depending on your machine.
 - Right mouse click -> 'Install'.

No confirmation is shown, but if the driver is succesfully
installed, a popup should appear in the lower right saying 
"Filament Maker or Filament Extruder (COM...) installed" when the filament
Extruder/Maker is next plugged into USB. Airid dryers will show up as
"Airid Granulate Dryer - Serial (COM...)".

Installing the driver only needs to happen once, so you can skip this
step when later updating to another firmware version.


Step 2: Firmware upload
-----------------------
Start the updating tool doubleclicking the '3devoUpdate.exe' (or just
'3devoUpdate') executable file.

To start the upload:
 - Select the (virtual) serial port corresponding to the Filament
   Maker or Airid Dryer. This list is automatically filtered, so usually it
   will contain just one entry.
 - Select the firmware file to upload. This is a .DfuSe or .hex file that
   should be separately supplied.
 - Click "Upload"



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
The `avrdude` program is used to actually upload the firmware to
Filament extruder and Filament Makers and must be installed. Most
Linux distributions will offer an `avrdude` package that will work
(`avrdude` version 6.1 or higher is needed).

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
The actual hex file upload is done using the avrdude command. In a terminal, run
a command like:

    avrdude -p atmega2560 -b 115200 -carduino -P/dev/ttyACM0 -D -Uflash:w:FeFirmware-v1.0.0.hex:i -v

Where `/dev/ttyACM0` is replaced with the appropriate serial port,
`FeFirmware-v1.0.0.hex` with the path to the firmware file to upload. If
needed, update the `avrdude` part with the path to the avrude binary.


Getting dfu-util
----------------
The `dfu-util` program is used to upload the firmware to
Airid dryers and must be installed. Most Linux distributions will offer
an `dfu-util` package that will work(`dfu-util` version 0.9 or higher is needed).

Note that, in this case Arduino software also supplies `dfu-util`, but this
version is not usable since the supplied version is v0.1 which is not compatible
with the 3devoUpdate application.


Running dfu-util
----------------
The actual DfuSe file upload is done using the dfu-util command. In a terminal, run
a command like:

    dfu-util -a 0 -s :leave -S "123456789123" -D GdFirmware-v1.0.0.DfuSe

Where "123456789123" is replaced with the appropriate device serial number, 
`GdFirmware-v1.0.0.DfuSe` with the path to the firmware file to upload.
The serial number can be found with the '--list' option without other parameters
in dfu-util. 


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
service@3devo.com.


avrdude
-------
A copy of avrdude.exe is distributed along with this tool. It is a
verbatim copy taken from the zipfile downloaded from the avrdude project
at http://download.savannah.gnu.org/releases/avrdude/avrdude-6.1-mingw32.zip
and is licensed under the GPLv2.

The source code and full license terms can be found at
http://download.savannah.gnu.org/releases/avrdude/


dfu-util
--------
A copy of dfu-util.exe is distributed along with this tool. It is a
verbatim copy taken from the zipfile downloaded from the avrdude project
at https://sourceforge.net/projects/dfu-util/files/latest/download
and is licensed under the GPLv2.

The source code and full license terms (file "COPYING") can be found at
https://sourceforge.net/p/dfu-util/dfu-util/ci/master/tree/


libusb
------
A copy of libusb0.dll and libusb-v1.0.dll is distributed along with this tool.
It is a verbatim copy taken from the zipfile downloaded from the libusb project
at http://downloads.sourceforge.net/libusb-win32/libusb-win32-bin-1.2.6.0.zip
and is licensed under the LGPLv3.

The source code and full license terms can be found at
http://downloads.sourceforge.net/libusb-win32/libusb-win32-src-1.2.6.0.zip

