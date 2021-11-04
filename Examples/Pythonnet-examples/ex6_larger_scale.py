import sys

import clr
import matplotlib.pyplot as plt

assembly_path = r"C:\Appl\TimeSeriesAnalysisBuild"

sys.path.append(assembly_path)


clr.AddReference("System")
clr.AddReference("System.Collections")
clr.AddReference("TimeSeriesAnalysis")

from System.Collections.Generic import List
from TimeSeriesAnalysis.Dynamic import (
    DefaultProcessModel,
    DefaultProcessModelParameters,
    ISimulatableModel,
    PIDModel,
    PIDModelParameters,
    PlantSimulator,
    SignalType,
    TimeSeriesDataSet,
)
from TimeSeriesAnalysis.Utility import TimeSeriesCreator

timeBase_s = 1.0

N = 500

modelParameters = DefaultProcessModelParameters()
modelParameters.WasAbleToIdentify = True
modelParameters.TimeConstant_s = 10.0
modelParameters.ProcessGains = [1.0, 2.0]
modelParameters.TimeDelay = 0.0
modelParameters.Bias = 5.0


process = DefaultProcessModel(modelParameters, timeBase_s, ID="SubProcess1")

pidParameters = PIDModelParameters()

pidParameters.Kp = 0.5
pidParameters.Ti_s = 20.0


pid = PIDModel(pidParameters, timeBase_s, ID="PID1")

sim_models = List[ISimulatableModel]()
sim_models.Add(pid)
sim_models.Add(process)


plantSimulator = PlantSimulator(timeBase_s, sim_models)

plantSimulator.ConnectModels(process, pid)
plantSimulator.ConnectModels(pid, process, 0)

plantSimulator.AddSignal(process, SignalType.Distubance_D, TimeSeriesCreator.Step(N/4, N, 0, 1))
plantSimulator.AddSignal(pid, SignalType.Setpoint_Yset, TimeSeriesCreator.Step(0, N, 50, 50))
plantSimulator.AddSignal(process, SignalType.External_U, TimeSeriesCreator.Step(N/2, N, 0, 1), 1)


simData = TimeSeriesDataSet(int(timeBase_s))
isOK, simDataUpdated = plantSimulator.Simulate(simData)

fig, (ax1, ax2) = plt.subplots(2, 1, sharex=True)
fig.suptitle("Example 6: Larger-scale dynamic process simulation")
ax12 = ax1.twinx()

l1 = ax1.plot(list(simDataUpdated.GetValues(process.GetID(), SignalType.Output_Y_sim)), label="y_sim", color="C0")[0]
l121 = ax12.plot(list(simDataUpdated.GetValues(process.GetID(), SignalType.Distubance_D)), label="disturbance", color="C1")[0]
l122 = ax12.plot(list(simDataUpdated.GetValues(process.GetID(), SignalType.External_U, 1)), label="u_external", color="Black")[0]
l2 = ax2.plot(list(simDataUpdated.GetValues(pid.GetID(), SignalType.PID_U)), label="u_pid", color="C2")[0]

ax1.grid()
ax2.grid()
ax1.legend([l1, l121, l122, l2], ["y_sim", "disturbance", "u_external", "u_pid"])


plt.show()
