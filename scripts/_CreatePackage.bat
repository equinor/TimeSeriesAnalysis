cd ..
dotnet build TimeSeriesAnalysis.sln -c Release -p:NuGetBuild=True -p:GenerateDocumentationFile=True -p:DebugSymbols=true -p:DebugType=portable
dotnet pack TimeSeriesAnalysis.sln -c Release  --include-symbols -p:NuGetBuild=True -p:SymbolPackageFormat=snupkg --include-source
pause