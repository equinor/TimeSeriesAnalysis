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
    class Control
    {
        [Test]
        public void  CascadeControl()
        {
            Shared.DisablePlots();

            ProcessControl pc = new ProcessControl();
            var dataSet = pc.CascadeControl();

            Shared.EnablePlots();
        }

        [Test]
        public void FeedForwardControl_Part1()
        {
            Shared.DisablePlots();

            ProcessControl pc = new ProcessControl();
            var dataSet = pc.FeedForward_Part1();

            Shared.EnablePlots();
        }


        [Test]
        public void FeedForwardControl_Part2()
        {
            Shared.DisablePlots();

            ProcessControl pc = new ProcessControl();
            var dataSet = pc.FeedForward_Part2();

            Shared.EnablePlots();
        }

        [Test]
        public void GainScheduling()
        {
            Shared.DisablePlots();

            ProcessControl pc = new ProcessControl();
            var dataSet = pc.GainScheduling();

            Shared.EnablePlots();
        }
        [Test]
        public void MinSelect()
        {
            Shared.DisablePlots();

            ProcessControl pc = new ProcessControl();
            var dataSet = pc.MinSelect();
         //   dataSet.SetT0(new DateTime(2021,1,1));
        //    var isOk = dataSet.ToCSV(@"C:\Appl\source\TimeSeriesAnalysis\minSelect_large.csv");
      //      Assert.IsTrue(isOk);
        //    var dataSet2 = new TimeSeriesDataSet(@"C:\Appl\source\TimeSeriesAnalysis\minSelect.csv");
            Shared.EnablePlots();
        }
    }
}
