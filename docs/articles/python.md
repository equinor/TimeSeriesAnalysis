# Loading this library from Python

Loading and using the TimeSeriesAnalysis .NET class library in Python is possible by utilizing the [Python.NET](https://github.com/pythonnet/pythonnet) package. ``pythonnet`` enables calling .NET code in Python, and allows Python code to interact with the .NET Common Language Runtime (CLR).

> [!Note]
> Make sure that you use a python version compatible with a recent release of Python&#46;NET.

### Python setup

In order to set up a working configuration in Windows, follow the steps below:

1. Install a compatible version of [Python](https://www.python.org/downloads/windows/).
2. In your project directory (e.g. "C:\Appl\myProject"), create a new virtual environment and specify the desired Python version (e.g. 3.12):

```console
# In project directory "C:\Appl\myProject"
> python3.12 -m venv venv
```

3. Activate the virtual environment:
```console
> venv\Scripts\activate
```

4. Install the Python&#46;NET package:
```console
> pip install pythonnet
```

5. Download a complete build of the TimeSeriesAnalysis assembly and accompanying dependecies from the list of [online releases](https://github.com/equinor/TimeSeriesAnalysis/releases), and unzip the assembly in a designated folder you have created in your project directory (e.g. "C:\Appl\myProject\TSABuild").

> [!Note]
> Alternatively, you can build and assemble the Dynamic-link libraries yourself, and copy the *.ddl-files into the folder in your project directory.

6. If the Dynamic-link libraries are downloaded from the Internet on a Windows computer, unblock the TimeSeriesAnalysis assembly:
    * Locate the file ``TimeSeriesAnalysis.dll`` in your project directory (e.g. "C:\Appl\myProject\TSABuild").
    * Right-click on the file and select Properties from the menu.
    * Click "Unblock" under the General tab.
    * Click Apply, and then OK.

7. To ensure the assembly can be implicitly imported, the directory containing the TimeSeriesAnalysis assembly must be added to your Python system path. In your desired Python file within your project, append the path of the folder containing the *.dll-files (e.g. "C:\Appl\myProject\TSABuild") to the PYTHONPATH of your system:

```Python
import sys

assembly_path = r"C:\Appl\myProject\TSABuild"
sys.path.append(assembly_path)
```

8. Load the TimeSeriesAnalysis assembly using Python&#46;NET:
```Python
import clr

clr.AddReference("TimeSeriesAnalysis")
```

The non-private classes, structs, interfaces and methods from the TimeSeriesAnalysis .NET class library can now be utilized in Python. Consult the API documentation for reference, or check out some of the Python-specific introductory code examples.

### Using the modules in Python

Classes and methods can be imported from the ``TimeSeriesAnalysis`` namespace and subnamespaces and used in Python.

Importing the vector class and implementing the vector addition
```Python
from TimeSeriesAnalysis import Vec

results = Vec().Add([1, 2], [3, 4])
```

will yield an array of the type ``System.Double[]``, which can be accessed using standard Python list indexing syntax:
```Python
>> results[0]
4.0

>> results[1]
6.0
```