# IntegratorEduTeacherNotify
Schedules events.

## Changes
* (2016-10-05) recurring event categories should be added to categories_to_string (the string version of the list, for debug output) with += instead of =

## Known Issues
* Should only play sound once if missed instead of looping continuously (such as if screen was locked and another person signs in while other user is still signed in)

## Features
* Play a sound
* Show a message (optionally customize MessageBox heading)
* Events on a specific minute, only on certain days of the week
* Event Category, which explains what to do, can be assigned to any number of times (down to the minute, so up to once every minute).
* Calendar can contain different schedules for different date ranges in the year
* A schedule can be assigned to any number of date ranges

## Settings format
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
		