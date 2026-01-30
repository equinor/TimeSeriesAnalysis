
## How the built-in plotting works

> [!Note]
> The built-in plotting is mainly intended to provide a stand-alone plotting capability to those
> working directly in C# in Visual Studio or VS Code. If you are running the library from Python or Matlab, you might just as well  
> get started with the built-in plotting in those languages (see Python "getting started" examples).

The components of the built-in plotting are
- ``dotnet serve`` is used to spin up lightweight http server locally, and serve the folder "www" of the repo as ``localhost`` 
    - on Windows this server is started by ``www\serve.bat``
    - on Mac or Linux this server is started by ``www\serve.sh``
- In the ``Utility`` folder there are C# helper classes like ``Plot.cs`` and ``PlotGain.cs`` that can be called from c# to create plots. These plots
    1. write the given time-series to the ``www\plotly\data`` folder as named csv-files to be read by ``plotly.js``, and
    2. start a new chrome tab with at the address ``localhost\tsa\index.hml#[hash]`` where ``hash`` contains the specification of which variables to plot and how
- A "bridge" javascript code has been made in ``www\plotly\plotlyinterface.js`` that starts plotly plots according to the hash information in the url, and reads data from the ``www\plotly\data`` folder.
    - The library includes a copy of the open-source Javascript library ``plotly.js'' that does the actual plotting

The plotting expects Chrome to be installed in ``C:\Program Files (x86)\Google\Chrome\Application\chrome.exe`` or ``C:\Program Files \Google\Chrome\Application\chrome.exe`` on Windows: 

> [!Note]
> **Mac and Linux**
> On Mac or Linux, you may need to edit the ``chromePath`` in the file ``Utility\Plot.cs``, or edit App.Config as shown below.

## How to plot

1. make sure Chrome is installed, preferably in the default directory
2. start ``dotnet serve`` by running scripts ``serve.bat`` or ``serve.sh``
3. run any test that references ``Plot`` or ``PlotGain``

> [!Note]
>
>**How the test if it works** 
>The unit tests under "GettingStarted" have the plots enabled by default, but are excluded from the "run all unit tests" command. Going into the ``Test Explorer`` in 
> Visual Studio or "Testing" in VS Code and running any of these tests should cause a plot to appear. 

## Enabling and disabling plots: best practice 

Plotting is intended to be used when analyzing a certain test of feature, and should be part of test code. 
To avoid code breaking when implementing new features, it is common to "Run All" unit test, and in that case it is no longer practical to have 
houndreds of plots open up. 

For this reason, it is suggested to place Plot code inside an "if" statement and to set the if statement to "false".

When debugging:
```csharp
if (true)
{
    Plot.FromList(varsToPlot, plotConfig, pidDataSet.GetTimeBase(), caseId);
}
```

When checking in code:
```csharp
if (false)
{
    Plot.FromList(varsToPlot, plotConfig, pidDataSet.GetTimeBase(), caseId);
}
```

This pattern is preferred over commenting out or deleting plots. 
- If plots are deleted, then this makes it harder to debug broken unit tests later. 
- If plotting code is commented out, then the code may become out-of-date if plotting code is ever refactored. 




### Changing the paths by editing App.config
 
If the paths described above for whatever reason conflict with the setup of your computer, you can change these paths 
by editing the file ``App.config``, which by default has the following content:

[!code-csharp[Example](../../App.Config)]


