## Setting up plots

> [!Note]
> The built-in plotting is mainly intended to provide a stand-alone plotting capability to those
> working in Visual Studio. If you are running the code from Python or Matlab, you might find it easier to 
> get started with the built-in plotting in those languages (see Python "getting started" examples).


If you have set up plotting correctly, you should be able to run the "PlotUnitTests" test from the Test Explorer in Visual Studio and plots should appear in a new browser window.

For plotting to work you need four prerequisites
- Chrome must installed (by default in ``C:\Program Files (x86)\Google\Chrome\Application\chrome.exe``)
- you need to be running a local http-server, if it servers a folder ``[wwwroot]`` then beneath it there mustbe a ``[wwwroot]\plotly`` that contains the javscript files in the "www\plotly" subfolder in the TimeSeriesAnalysis repository
- the folder ``[wwwroot]\plotly\Data`` needs to match the ``plotDataPath`` in Plot (by default: ``C:\inetpub\wwwroot\plotly\Data``). CSV-data files will be written in here.
- the front-end JavaScript code needs to find the time-series data in csv-files in its ``localhost\plotly\data`` folder.
- (if your http-server is serving another folder than ``C:\inetpub\wwwroot`` up on ``localhost``, you need to use ``mklink` to link this folder with the http-server's ``[root]\plotly\data`` folder )

> [!CAUTION]
> Be aware that you if port ``80`` and ``443`` are open on your machine and network, the data in the ``plotly\data`` folder may
> be shared with the world. It is your responsibility to block these ports if you want to avoid sharing sensitive data. 

### Chrome

Currently, plotting requires Chrome,and the project expects Chrome to be installed in the folder ``C:\Program Files (x86)\Google\Chrome\Application\chrome.exe``.

### Running a local HTTP-server

In Windows, if you are not already running a http-server, the easiest way to install one may be to install 
``Internet Information Services(IIS)``.
This is done from ``Control Panel->Windows Features`` and selecting ``Internet information Services`` in the menu that appears (requires Administrator privileges).

If setting up a new server, it is advantageous to map the folder ``c:\inetpub\wwwroot``, but it can be any path as long as it 
contains a "plotly" subfolder and that subfolder has a subfolder ``data`` where data-files are added.

If running IIS, 
- start "Internet Information Services(IIS) Manager" from the start menu, then 
- go to "default website"
- press "basic settings",
- make a note of the "physical path" and change if needed. 

The "physical path" is where you need to link in the "plotly" folder. 

![IIS](./images/IIS_setup.png)


### Serving up the "plotly" folder

When running a local http-server for development, you will need to add the folder "plotly" to it. 
The preferable way to to this is to add a symbolic link to the ``TimeSeriesAnalysis`` folder rather than copying files, to allow for version control. 

Suppose that your ``TimeSeriesAnalysis`` source code is stored in ``C:\appl\source\TimeSeriesAnalysis``, and that you are running an http-sever that is hosting the folder ``C:\inetpub\wwwroot``(your ``localhost``). 

*In Windows*: Start a ``command prompt``(cmd.exe) session in ``Windows`` with **administrator privileges** and give the following command:

``mklink /D c:\inetpub\wwwroot\plotly C:\appl\source\TimeSeriesAnalysis\www\plotly``

> [!Note]
> Plotting using the``Plot`` class will cause browser windows to open on ``localhost\plotly\index.html``. This html-file expects to find javscript files
``CsvToTable.js``, ``plotlyInterface.js`` and ``vendor\plotly.min.js``. ``vendor\plotly.min.js`` is obtained from https://plotly.com/javascript/getting-started/
and you can swap the given file for other versions if needed.

### Changing the paths by editing TimeSeriesAnalysis.dll.config
 
If the paths described above for whatever reason conflict with the setup of your computer, you can change these paths 
by editing the file ``TimeSeriesAnalysis.dll.config``, which by default has the following content:

[!code-csharp[Example](../../App.Config)]

#### Disabling all plots

It is possible to entirely disable all plotting by setting the variable ``PlotsAreEnabled`` in the above mention filed to ``false``.
This could be useful as a safety-measure if the code was ever to run in a production environment. 
 
#### If necessary ``localhost\plotly\data`` needs to symbolically linked to  ``c:/inetpub/www/plotly/data`` folder

An alternate way to get around paths that do not suit your runtime environment is to use ``mklink``.

If your http-server is mapping "c:\inetpub", then you will not need to do this step. 
``TimeSeriesAnalysis`` will write data into ``C:\inetpub\plotly\Data`` and it will thus be 
found by the javascript browser-side code and run to display your plots.

*If* you are serving up another folder [PathXYZ] on your computer to localhost than "c:\inetpub" , and do not want or cannot
for whatever reason change ``TimeSeriesAnalysis.dll.config``,an alternate solution is to make a 
directory link by the following command on the command prompt(``cmd.exe``) so: 

``mklink /D C:\inetpub\wwwroot\plotly\Data C:\[PathXYZ]\plotly\Data``

