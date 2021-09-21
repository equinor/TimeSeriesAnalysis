## Setting up NuGet

``TimeSeriesAnalysis`` uses NuGet to retrieve pre-compiled open-source packages on which it is based.

NuGet can be set up in several different ways. ``TimeSeriesAnalysis`` uses the ``PackageReference`` type of 
configuration, in which NuGet configuration is stored in ``TimeSeriesAnalysis.csprocj``.

> [!Note]
> NuGet will not create a local ``packages`` subfolder upon a ``NuGet restore``.Instead it creates a 
> global package folder on your computer in the folder``%userprofile%\.nuget\packages``. In some cases it is 
> insightful to examine the contents of this folder if you are having any NuGet issues.


In ``Visual Studio``, examine the menu ``TOOLS>NuGet Package Manager>Package Manager Settings``.
You should have the following selected:
 - Allow NuGet to download missing packages (should be ``Checked``)
 - Automatically check for missing packages during build in Visual Studio (should be ``Checked``)
 - Default package management format should be ``PacakgeReference``

> [!Note]
> If you are having issues with NuGet, you can try pressing ``Clear All NuGet Cache(s)`` in the above dialog box.

``Solution Explorer>Solution TimeSeries analysis(right click)>Restore NuGet packages`` should run with zero errors 
(observe the ``Output>Package Manger`` for possible error) messages.


