using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Xunit;
using VertikalgradientBerechnung;

namespace VertikalgradientBerechnung.Tests
{
    public class AnomalyAnalyzerTests
    {
        private readonly string _testFilePath = "test.csv";

        [Fact]
        public void CSVReader_ShouldReadCSVCorrectly()
        {
            File.WriteAllLines(_testFilePath, new string[]
            {
                "X,BH1,BH2",
                "1,10,20",
                "2,15,25",
                "3,20,30"
            });

            var reader = new AnomalyAnalyzer.CSVReader(_testFilePath);

            Assert.Equal(new List<double> { 1, 2, 3 }, reader.XValues);
            Assert.Equal(new List<double> { 10, 15, 20 }, reader.BH1Values);
            Assert.Equal(new List<double> { 20, 25, 30 }, reader.BH2Values);
        }

        [Fact]
        public void GradientCalculator_ShouldCalculateGradientsCorrectly()
        {
            var bh1Values = new List<double> { 10, 15, 20 };
            var bh2Values = new List<double> { 20, 25, 30 };
            double h1 = 2.1;
            double h2 = 3.1;

            var calculator = new AnomalyAnalyzer.GradientCalculator(bh1Values, bh2Values, h1, h2);

            var expectedGradients = new List<double> { 10, 10, 10 }; // (20-10)/(3.1-2.1) = 10

            Assert.Equal(expectedGradients, calculator.Gradients);
        }

        [Fact]
        public void GradientCalculator_ShouldFindMaxGradientIndex()
        {
            var bh1Values = new List<double> { 10, 15, 20 };
            var bh2Values = new List<double> { 20, 35, 30 };
            double h1 = 2.1;
            double h2 = 3.1;

            var calculator = new AnomalyAnalyzer.GradientCalculator(bh1Values, bh2Values, h1, h2);

            int expectedIndex = 1; // Maximaler Gradient bei Index 1 (35-15)/(3.1-2.1) = 20

            Assert.Equal(expectedIndex, calculator.MaxGradientIndex);
        }

        [Fact]
        public void GradientCalculator_ShouldCalculateDepthCorrectly()
        {
            double h1 = 2.1;
            double h2 = 3.1;
            double bh1 = 10;
            double bh2 = 20;

            var calculator = new AnomalyAnalyzer.GradientCalculator(new List<double>(), new List<double>(), h1, h2);
            double expectedDepth = ((h2 - h1) * Math.Pow(bh1 / bh2, 0.333)) / (Math.Pow(bh1 / bh2, 0.333) - 1);

            Assert.Equal(expectedDepth, calculator.CalculateDepth(h1, h2, bh1, bh2), 3);
        }

       [Fact]
        public void PlotGenerator_ShouldCreatePlotFile()
        {
            // Arrange
            var xValues = new List<double> { 0, 10, 20 };
            var gradients = new List<double> { 0, 10, 5 };
            var plotGenerator = new AnomalyAnalyzer.PlotGenerator(xValues, gradients);
            var outputFilePath = "Vertikalgradient.svg";

           var originalConsoleOut = Console.Out;
            using (var sw = new StringWriter())
            {
                Console.SetOut(sw);

                try
                {
                    plotGenerator.CreatePlot();

                    Assert.True(File.Exists(outputFilePath));
                }
                finally
                {
                    Console.SetOut(originalConsoleOut);
                }
            }

            if (File.Exists(outputFilePath))
            {
                File.Delete(outputFilePath);
            }
        }

        [Fact]
        public void AnomalyAnalyzer_ShouldAnalyzeAnomalyCorrectly()
        {
            File.WriteAllLines(_testFilePath, new string[]
            {
                "X,BH1,BH2",
                "1,10,20",
                "2,15,25",
                "3,20,30"
            });

            double h1 = 2.1;
            double h2 = 3.1;

            var analyzer = new AnomalyAnalyzer();
            using (var sw = new StringWriter())
            {
                Console.SetOut(sw);
                analyzer.AnalyzeAnomaly(_testFilePath, h1, h2);
                var result = sw.ToString().Trim();

                Assert.Contains("Die berechnete Tiefe der Anomalie ist", result);
                Assert.True(File.Exists("Vertikalgradient.svg"));
            }
        }
    }
}

