# IntegratorEduTeacherNotify
Run scheduled sounds and messages, such as from a central network configuration directory.

This program can replace hardware school bell systems, but the computer must be logged in and the speakers must be on.

## Troubleshooting

It has a public license so it is free for anyone to download and use, but there are FCA-related settings already on the FCA network in a CSV file that contains bell times. There is a 1sec bell sound for beginning of class, 2 sec for end, 3 sec for end of school. I did the same for the master time system. The pattern is less monotonous since it is more communicative.

### Vacation Setting
There are vacations or at least a start and end date for the school year in the software bell configuration on the network. That is the most likely reason the bells are not ringing right now. The network contains a date ranges file that is in a strict YAML format, which you can edit with a text editor. I recommend Notepad++ for Windows. Geany is fine as long as you dont have it set to convert spaces to tabs.

If all software bells stopped, the vacation setting is likely all that has affected you. However, below are additional troubleshooting steps, from most to least likely. Below I also note the location of the configuration folder on the network.

### The program is installed but there is no sound.
The computer must be not logged in. Ensure that the speakers are on and turned up enough.

### Missing configuration
To see stats, including the last error, right-click the tray icon then click "About." The error with several instances of **"missing...column"** indicates that the network location was inaccessible when the program loaded (a missing bell sound error may indicate an outage after that at the time it tried to play a bell).

The configuration file pulls from the network, so if there was an outage try logging off then back on which is the easiest way to reload the tray icon.

You must keep the configuration intact at the network location \\fcafiles\main\Operations\BellSettings (make sure you keep it backed up).
If you must use another location, you must put the location (and nothing else) in the data_path file in C:\ProgramData\IntegratorEduTeacherNotify\. If you do not have a configuration you must create one (See the "defaults" directory and "Configuration" section below for examples and more information).

### The program is not installed
If you dont have a tray icon you must put the exe somewhere, usually C:\ProgramData\IntegratorEduTeacherNotify\ and ensure it runs on startup, but the best way is to put the unzipped release on a flash drive and run (You probably have to right click and run as administrator): "install-from-here.." batch file, which will install the program correctly and run the reg file that makes the program run on startup (for all users) pointing to the location to which it installs. The batch file cannot be in a network location (see the other batch file for that purpose).


## Changes
See CHANGELOG.md.


## Known Issues
(See also <https://github.com/poikilos/IntegratorEduTeacherNotify> then click "Issues.")

* Should only play sound once if missed instead of looping continuously (such as if screen was locked and another person signs in while other user is still signed in)


## Features
* Play a sound
* Show a message (optionally customize MessageBox heading)
* Events on a specific minute, only on certain days of the week
* Event Category, which explains what to do, can be assigned to any number of times (down to the minute, so up to once every minute).
* Calendar can contain different schedules for different date ranges in the year
* A schedule can be assigned to any number of date ranges


## Configuration
Program stores "data_path" locally, which can store a path such as a network path (the data folder).
The data folder contains a settings.yml, RecurringEventCategories, any media you want to run at a scheduled time in any folder you want, and calendar folders
	settings.yml file says which calendar to use (basename of subfolder of data folder)
	RecurringEventCategories contains yml files that events can use to determine what media to run. For example, the file can be called End.yml (can contain one or more of these lines):
		sound: sounds\bell2.wav
		message: Leave for early lunch in room x today
		message_heading: Example Inc. Notification
	The calendar folder contains "date ranges.yml" which says which schedule to use at what date ranges in the year. It contains a YAML array where each element contains the following values: schedule (basename of a subfolder of calendar folder), first (start date NOT including year), last (inclusive end date NOT including year).
		A schedule folder contains "Events, Daily Disposable.csv" which contains the following column headers: Time,Category,Description,Days
			Time: time of day (24-hr format)
			Category: name (without extension) of a yml file in RecurringEventCategories. The yml file determines what to do.
			Description: This is for the scheduler's reference, and is not shown on the computer where deployed.
			Days: semicolon-separated list of days (Can be first two letters, first three letters, or full name of day).
		