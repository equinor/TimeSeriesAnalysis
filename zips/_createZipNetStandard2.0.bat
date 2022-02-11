echo create a zipped numbered release of the the complete library including dependencies

set buildnr=%1

set name=TimeSeriesAnalysis.%buildnr%.zip

zip %name% .\readme.md 
zip -j %name% ..\bin\debug\netstandard2.0\*.dll
zip -j %name% ..\bin\debug\netstandard2.0\*.pdb
zip -j %name% ..\bin\debug\netstandard2.0\*.dll.config
zip -j %name% ..\bin\debug\netstandard2.0\TimeSeriesAnalysis.xml
zip -r %name% ..\www\plotly\*.js 
zip -r %name% ..\www\plotly\vendor\*.js 
zip -r %name% ..\www\plotly\*.html

