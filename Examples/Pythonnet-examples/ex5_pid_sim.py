import sys

import clr
import matplotlib.pyplot as plt

assembly_path = r"C:\Appl\TimeSeriesAnalysisBuild"

sys.path.append(assembly_path)


clr.AddReference("System")
clr.AddReference("System.Collections")
clr.AddReference("TimeSeriesAnalysis")

from TimeSeriesAnalysis.Dynamic import (
    DefaultProcessModel,
    DefaultProcessModelParameters,
    PIDModel,
    PIDModelParameters,
    SubProcessDataSet,
    SubProcessSimulator,
)
from TimeSeriesAnalysis.Utility import TimeSeriesCreator

timeBase_s = 1.0

N = 500

modelParameters = DefaultProcessModelParameters()

modelParameters.WasAbleToIdentify = True
modelParameters.TimeConstant_s = 10.0
modelParameters.ProcessGains = [1.0]
modelParameters.TimeDelay = 0.0
modelParameters.Bias = 5.0


processModel = DefaultProcessModel(modelParameters, timeBase_s)

pidParameters = PIDModelParameters()

pidParameters.Kp = 0.5
pidParameters.Ti_s = 20.0


pid = PIDModel(pidParameters, timeBase_s)

dataSet = SubProcessDataSet(timeBase_s, N)

dataSet.D = TimeSeriesCreator.Step(N / 4, N, 0, 1)
dataSet.Y_setpoint = TimeSeriesCreator.Step(0, N, 50, 50)

simulator = SubProcessSimulator(processModel)
isOK, dataSet = simulator.CoSimulateProcessAndPID(pid, dataSet)

fig, (ax1, ax2) = plt.subplots(2, 1, sharex=True)
fig.suptitle("Example 5: PID-controller")
ax12 = ax1.twinx()

l1 = ax1.plot(list(dataSet.Y_sim), label="y_sim", color="C0")[0]
l12 = ax12.plot(list(dataSet.D), label="disturbance", color="Black")[0]
l2 = ax2.plot(list(dataSet.U_sim), label="u_pid", color="C1")[0]
ax1.grid()
ax2.grid()
ax1.legend([l1, l12, l2], ["y_sim", "disturbance", "u_pid"])


plt.show()
