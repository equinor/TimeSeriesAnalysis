TimeSeriesAnalysis : 

What is this?
=========================================
This is a .NET class library for making it easy to work with time series in .NET language such as C#. 
It handles
- loading data from CSV
- manipulating time-series data as arrays and vectors
- filtering
- doing linear regression
- plotting times-series  

The aim of this library is to make the process of working with time series as easy as possible, 
and the resulting workflow should be comparable to working in Matlab, Python or R. 

Originally this code has been written with control-engineering in mind, as control engineers typically 
prefer compiled languages such as c# for code that is to be integrated in the control system. 


Install instructions to make in-browser time-series plotting work
=========================================

The plotting in this package is based on the javacsript plotting library plot.ly., a copy of which is included.

The repo includes a folder "www\plotly" and for plotting to work you need to run an http-server such as Internet Information Services that
in your broser serves up a folder on your computer as "localhost". 

Copy the folder "plotly" into the folder that for instance IIS is serveing(where it can live along side other pages you may be serving up),
such as "c:\inetpub\plotly" if  "c:\inetpub" is the folder that IIS is serving.

Plotting works by launching a browser(chrome) and directing it to "http://localhost/plotly/index.html", but all the low-level details are handled by the Plot class for you.



Nuget package upload how-to
=========================================

This is  .NET framework 4.6.1 class library published to github packages, by means of the following tutorial:
https://github.community/t/publish-net-framework-nuget-package/3077/2
and here:
https://docs.microsoft.com/en-us/nuget/create-packages/creating-a-package-msbuild

Note that the steps here are somewhat different to most online tutorials which target .NET Core and use the "dotnet" CLI instead of "nuget" CLI.

For future reference, this is the steps followed:

- the description that will be shown in nuget when downloading is pulled from <description></description> in the .csproj file beneath <propertygroup>. Consider adding it.
Also add the the url to the repository and some other info such as shown below:
```
    <RepositoryUrl>https://github.com/equinor/TimeSeriesAnalysis</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
    <PackageOutputPath>bin\debug\</PackageOutputPath>
    <Version>1.0.0</Version>
    <Authors>Steinar Elgsæter</Authors>
	<description>todo</description> 
	```	
- reccomend adding in information to the AssemblyInfo.cs before starting.
- a personal acceess token xxxxxxx needs to be generated for your user in github,and needs access to "read:packages", 
"write:packages" and "repo".organization access (authorize with SSO ) and then press "authorize"
- create a nuget.config file that defines "github" as a nuget destination:
```	<?xml version="1.0" encoding="utf-8"?>
	<configuration>
		<packageSources>
			<clear />
			<add key="github" value="https://nuget.pkg.github.com/equinor/index.json" />
			<add key="nuget" value="https://api.nuget.org/v3/index.json" />
		</packageSources>
	 <packageSourceCredentials>
			<github>
				<add key="Username" value="steinelg" />
				<add key="ClearTextPassword" value=xxxxxxx" />
			</github>
		</packageSourceCredentials>
</configuration>```
- pacakges.nuget needs to be moved into project file *.csproj as <pacakgereference> items instead
- NuGet.Build.Tasks.Pack need to be added as a pacakage to project
- need to download nuget.exe and use it to push generated .nupkg file

- then to publish put the following two commands in a publish.bat file:
```	nuget setapikey xxxxxxxx -source "github"
	nuget push bin\Debug\*.nupkg -source "github" -SkipDuplicate
	pause```
- check that the script concludes with "Your package was pushe" and no error messages in yellow or red.

Nuget package download how-to
=========================================

To use this nuget pacakge, you need to incldue the nuget.config file above in the solution that intends to pull down the package. You can then select github as
the package source in Visual Studio "Manage Nuget packages for Solution". 
	