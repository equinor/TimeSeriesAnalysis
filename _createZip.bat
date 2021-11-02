echo create a zipped numbered release of the the complete library including dependencies

set buildnr=%1

set name=zips\TimeSeriesAnalysis.%buildnr%.zip

zip %name% readme.md 
zip -j %name% bin\debug\*.dll
zip -j %name% bin\debug\*.pdb
zip -j %name% bin\debug\*.dll.config
zip -j %name% bin\debug\TimeSeriesAnalysis.xml
zip -r %name% www\plotly\*.js 
zip -r %name% www\plotly\vendor\*.js 
zip -r %name% www\plotly\*.html

