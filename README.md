# Desktop Audio Loopback Recorder Ripper Copyright (C) 2017-2025 Marc Williams

## 32-bit Floating Point 16-bit CD Quality WAV & MP3 Encoding with L.A.M.E.
### Record what the sound mixer is playing to file or stream via http(s)

* Easy installation: copy the unzipped exe to 'C:\Program Files\' and create a desktop shortcut
* Registry settings: Computer\\HKEY_CURRENT_USER\\Software\\DesktopRecorder
* Compatible with wine on linux
* Run multiple instances
* Hot swap the file name during recording
* Remote control a running instance using itself from the command line:
    DesktopRecorder.exe -start
    DesktopRecorder.exe -start -file C:\files\recordings\recording.wav -mode 1
    DesktopRecorder.exe -start -mode 5
    DesktopRecorder.exe -stop
    DesktopRecorder.exe -quit|-exit
* Auto start record from the command line: DesktopRecorder.exe -start
* Re-size-able GUI
* Transparent button option
* Precision recording timer
* GET POST & PUT supports WEBDAV and TLS
* The file name can be automatically appended with the date
* It will default to add a count if the file name exists
* There is an 'overwrite' option to use the same file name
* And an 'append' option to add audio to the end of the existing file

![alt desktop recorder](https://github.com/marc365/Windows-DesktopRecorder/raw/master/DesktopRecorder.PNG)
![alt desktop recorder button](https://github.com/marc365/Windows-DesktopRecorder/raw/master/DesktopRecorderButton.PNG)
![alt desktop recorder small](https://github.com/marc365/Windows-DesktopRecorder/raw/master/DesktopRecorderSmall.PNG)
![alt desktop recorder stream](https://github.com/marc365/Windows-DesktopRecorder/raw/master/DesktopRecorderStream.PNG)

* Windows Vista CoreAudio AudioClient Copyright (C) 2007 Ray Molenkamp
* NAudio library Copyright (C) 2001-2013 Mark Heath
* LibMp3Lame.NativeMethods Copyright (C) 2013 Corey Murtagh
* L.A.M.E. MP3 Encoder 3.99.2.5 Copyright (C) 1999-2011 The L.A.M.E. Team
