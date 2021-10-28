import sys

import clr
import matplotlib.pyplot as plt

assembly_path = r"C:\Appl\TimeSeriesAnalysisBuild"

sys.path.append(assembly_path)

clr.AddReference("TimeSeriesAnalysis")
clr.AddReference("System.Collections")
clr.AddReference("System")


from TimeSeriesAnalysis.Dynamic import LowPass
from TimeSeriesAnalysis.Utility import TimeSeriesCreator

timeBase_s = 1
filterTc_s = 10.0

input = TimeSeriesCreator.Step(11, 60, 0, 1)

lp = LowPass(timeBase_s)
output = lp.Filter(input, filterTc_s)


plt.plot(list(input), label="input")
plt.plot(list(output), label="output")
plt.grid()
plt.legend(loc="right")
plt.title("Example 1: Hello World")

plt.show()
