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
          //  Shared.EnablePlots();
            ProcessControl pc = new ProcessControl();
            Shared.DisablePlots();
            var dataSet = pc.CascadeControl();
      //     Shared.DisablePlots();
        }

        [Test]
        public void FeedForwardControl_Part1()
        {
           // Shared.EnablePlots();
            ProcessControl pc = new ProcessControl();
            var dataSet = pc.FeedForward_Part1();
           // Shared.DisablePlots();

            Assert.IsTrue(dataSet.GetValue("Process1-Output_Y", 599) -60< 0.01);
        }


        [Test]
        public void FeedForwardControl_Part2()
        {
            //   Shared.EnablePlots();
            ProcessControl pc = new ProcessControl();
            var dataSet = pc.FeedForward_Part2();
            //  Shared.DisablePlots();
        }

        [Test]
        public void GainScheduling()
        {
            //  Shared.EnablePlots();
            ProcessControl pc = new ProcessControl();
            var dataSet = pc.GainScheduling();
            //    Shared.DisablePlots();
        }
        [Test]
        public void MinSelect()
        {
            ProcessControl pc = new ProcessControl();
       //     Shared.EnablePlots();
            var dataSet = pc.MinSelect();
         //   Shared.DisablePlots();
            //   dataSet.SetT0(new DateTime(2021,1,1));
            //    var isOk = dataSet.ToCSV(@"C:\Appl\source\TimeSeriesAnalysis\minSelect_large.csv");
            //      Assert.IsTrue(isOk);
            //    var dataSet2 = new TimeSeriesDataSet(@"C:\Appl\source\TimeSeriesAnalysis\minSelect.csv");

        }
    }
}
