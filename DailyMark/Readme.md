# DailyMark USPTO trademark application monitoring program
### What?
DailyMark is an open source (AGPL) cross-platform desktop program which will get the latest trademark application data from the United States Patent and Trademark Office using the official API (application programming interface), run search queries against that data, and then produce a human-readable HTML report about any matches found.

### Why?
If you or your clients have trademark registrations you may want to monitor the USPTO for new applications similar to your registrations. To do that you can hire a company but you may not know how they do it and will not be in control of the process. 
Daily Mark allows you to handle the entire process in an automated way.

### How?
The entire process is automated, all you need to do is:
1. In the application directory, edit the SearchQueries.txt file to add all of your search queries 
2. Run the application
3. Read the reports that will be placed in the Reports directory

#### Search Queries
All of your search queries go into a text file called SearchQueries.txt in the following format:

Query Name=Search Pattern

Query Name is any text that helps you remember what the query means.\
Search Pattern is a string of text in which you can use 2 kinds of wildcard characters: _ means any single character, % means 0 or more characters.\
Case does not matter for the Search Pattern.\
You can add comments to the file by using # at the start of the line

Examples:\
Imagine you own registrations for WIDGET, FUN IN THE SUN, and TRY IT AND YOU WILL NOT FORGET IT you can use the following search queries:\
\
#starts with FUN IN THE\
StartsWithFun=fun in the%\
#ends with WILL NOT FORGET IT\
EndsWithForgetIt=%WILL NOT FORGET IT\
#contains WIDGET\
widget=%widget%

### Running DailyMark
In the Release directory find the directory for your operating system, double click on DialyMark.exe or DialyMark.\
The same directory will also contain SearchQueries.txt where you should place the search queries as discussed above.\



### Questions?

Questions?

Email me at: ivankuz@hotmail.com

### License

DailyMark trademark application monitoring software
Copyright (C) 2019  Ivan Kouznetsov

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as
published by the Free Software Foundation, either version 3 of the
License, or (at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

https://www.gnu.org/licenses/agpl-3.0.en.html


