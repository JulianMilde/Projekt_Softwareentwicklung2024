using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using IsolinesProj;

namespace IsolinesProj.Tests
{
    public class IsolineTests
    {
        private readonly string _validFilePath = "valid_test_file.csv";
        private readonly string _emptyFilePath = "empty_test_file.csv";
        private readonly string _headerOnlyFilePath = "header_only_test_file.csv";
        private readonly string _singleLineFilePath = "single_line_test_file.csv";
        
        private readonly List<ScalarFieldPoint> _testPoints = new List<ScalarFieldPoint>
        {
            new ScalarFieldPoint(0, 0, 1),
            new ScalarFieldPoint(1, 0, 2),
            new ScalarFieldPoint(0, 1, 3),
            new ScalarFieldPoint(1, 1, 4)
        };

        [Fact]
        public async Task ReadCsvAsync_ValidFile_ReturnsPointsList()
        {
            await CreateCsvFile(_validFilePath, "X,Y,Value\n0,0,1\n1,0,2\n0,1,3\n1,1,4");
            var points = await CsvReader.ReadCsvAsync(_validFilePath);
            Assert.NotNull(points);
            Assert.Equal(4, points.Count);
        }

        [Fact]
        public async Task ReadCsvAsync_EmptyFile_ReturnsEmptyList()
        {
            await CreateCsvFile(_emptyFilePath, "");
            var points = await CsvReader.ReadCsvAsync(_emptyFilePath);
            Assert.NotNull(points);
            Assert.Empty(points);
        }

        [Fact]
        public async Task ReadCsvAsync_HeaderOnlyFile_ReturnsEmptyList()
        {
            await CreateCsvFile(_headerOnlyFilePath, "X,Y,Value");
            var points = await CsvReader.ReadCsvAsync(_headerOnlyFilePath);
            Assert.NotNull(points);
            Assert.Empty(points);
        }

        [Fact]
        public async Task ReadCsvAsync_SingleLineFile_ReturnsSinglePoint()
        {
            await CreateCsvFile(_singleLineFilePath, "X,Y,Value\n0,0,1");
            var points = await CsvReader.ReadCsvAsync(_singleLineFilePath);
            Assert.NotNull(points);
            Assert.Single(points);
        }

        [Fact]
        public void ScalarField_Interpolate_MinResolution_ReturnsValidArray()
        {
            int resolutionX = 1;
            int resolutionY = 1;
            var scalarField = new ScalarField(_testPoints, resolutionX, resolutionY);
            var interpolatedValues = scalarField.Interpolate();
            Assert.NotNull(interpolatedValues);
            Assert.Equal(1, interpolatedValues.GetLength(0));
            Assert.Equal(1, interpolatedValues.GetLength(1));
        }

        [Fact]
        public void ScalarField_Interpolate_SmallNumberOfPoints_ReturnsValidArray()
        {
            int resolutionX = 10;
            int resolutionY = 10;
            var scalarField = new ScalarField(_testPoints, resolutionX, resolutionY);
            var interpolatedValues = scalarField.Interpolate();
            Assert.NotNull(interpolatedValues);
            Assert.Equal(resolutionX, interpolatedValues.GetLength(0));
            Assert.Equal(resolutionY, interpolatedValues.GetLength(1));
        }

        [Fact]
        public void ScalarField_Constructor_NullPoints_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ScalarField(null!, 10, 10));
        }

        private async Task CreateCsvFile(string filePath, string content)
        {
            using (var writer = new StreamWriter(filePath))
            {
                await writer.WriteAsync(content);
            }
        }
    }
}
