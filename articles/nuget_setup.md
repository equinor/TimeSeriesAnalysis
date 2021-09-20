## Setting up NuGet

``TimeSeriesAnalysis`` uses nuget to retreive pre-compiled open-source packages on which it is based.

Nuget can be set up in several different ways. ``TimeSeriesAnalysis`` uses the ``PackageReference`` type of 
configuration, in which nuget configuration is stored in ``TimeSeriesAnalysis.csprocj``.

> [!Note]
> Nuget will not create a local ``packages`` subfolder upon a ``nuget restore``.Instead it creates a 
> global package folder on your computer in the folder``%userprofile%\.nuget\packages``. In some cases it is 
> insightful to examine the contents of this folder if you are having any nuget issues.


In ``Visual Studio``, examine the menu ``TOOLS>Nuget Pacakge Manager>Package Manager Settngs``.
You should have the following selected:
 - Allow NuGet to download missing packages (shoudl be ``Checked``)
 - Automatically check for missing packages during build in Visual Studio (shoudl be ``Checked``)
 - Default package management format should be ``PacakgeReference``

> [!Note]
> If you are having issues with nuget, you can try pressing ``Clear All NuGet Cache(s)`` in the above dialog box.

``Solution Explorer>Solultion TimeSeries analysis(right click)>Restore NuGet packages`` should run with zero errors 
(observe the ``Output>Pacakge Manger`` for posssible error) messages.


