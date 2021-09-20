## Setting up plots


If you have set up plotting correctly, you should be able to run the "PlotUnitTests" test from the Test Explorer in Visual Studio and plots should appear in a new browser window.

### Chrome

Currently, plotting requires Chrome,and the project expects Chrome to be installed in the folder ``C:\Program Files (x86)\Google\Chrome\Application\chrome.exe``.

### Running a local HTTP-server

In Windows, if you are not already running a http-server, the easiest way to install one may be to install 
``Internet Information Services(IIS)``.
This is done from ``Control Panel->Windows Features`` and selecting ``Internet information Services`` in the menu that appears (requires Administrator priveleges).

If setting up a new server, it is advantageous to map the folder ``c:\inetpub``.


### Serving up the "plotly" folder

When running a local http-server for development, you will need to add the folder "plotly" to it. 
The preferable way to to this is to add a symbolic link to the ``TimeSeriesAnalysis`` folder rather than copying files, to allow for version control. 

Suppose that your ``TimeSeriesAnalysis`` source code is stored in ``C:\appl\source\TimeSeriesAnalysis``, and that you are running an http-sever that is hosting 
the folder ``C:\inetpub`` to your ``localhost``. 

*In Windows*: Start a ``command prompt``(cmd.exe) session in ``Windows`` with **administrator privileges** and give the following command:

``mklink /D c:\inetpub\wwwroot\plotly C:\appl\source\TimeSeriesAnalysis\www\plotly``

### Linking up the "plotly/data" folder

If your http-server is mapping "c:\inetpub", then you will not need to do this step. 
``TimeSeriesAnalysis`` will write data into ``C:\inetpub\plotly\Data`` and it will thus be 
found by the javascript browser-side code and run to display your plots.

*If* you are serving up another folder [PathXYZ] on your computer to localhost, and do not want to change this,
you will have to make a symoblic link like so: 

``mklink /D C:\inetpub\wwwroot\plotly\Data C:\[PathXYZ]\plotly\Data``
