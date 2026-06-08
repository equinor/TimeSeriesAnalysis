using NUnit.Framework;
using TimeSeriesAnalysis._Examples;


namespace TimeSeriesAnalysis.Test.PlantSimulations
{







    /// <summary>
    /// Tests that run the Processcontrol examples but with plotting disabled.
    /// <para>
    /// This way we both have examples that are free of an unit testing logic, but a
    /// also ensure that we have automatic verification that the example cases remain in working order,
    /// without having to duplicate the example code in separate unit tests.
    /// </para>
    /// </summary>
    [TestFixture]
    class AdvancedControlExamples
    {
        [Test]
        public void  CascadeControl()
        {
            ProcessControl pc = new ProcessControl();
            Shared.DisablePlots();
            var dataSet = pc.CascadeControl_explicitDisturbance();
        }

        [Test]
        public void FeedForwardControl_Part1()
        {
            ProcessControl pc = new ProcessControl();
            var dataSet = pc.FeedForward_Part1();
            Assert.IsTrue(dataSet.GetValue("Process1-Output_Y", 599) -60< 0.01);
        }


        [Test]
        public void FeedForwardControl_Part2()
        {
            ProcessControl pc = new ProcessControl();
            var dataSet = pc.FeedForward_Part2();
        }

        [Test]
        public void GainScheduling()
        {
            ProcessControl pc = new ProcessControl();
            var dataSet = pc.GainScheduling();
        }
        [Test]
        public void MinSelect()
        {
            ProcessControl pc = new ProcessControl();
            var dataSet = pc.MinSelect();

        }
    }
}
