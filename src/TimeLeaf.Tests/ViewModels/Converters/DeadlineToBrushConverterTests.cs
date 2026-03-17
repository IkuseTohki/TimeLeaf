using System;
using System.Globalization;
using System.Windows.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TimeLeaf.ViewModels.Converters;

namespace TimeLeaf.Tests.ViewModels.Converters;

[TestClass]
public class DeadlineToBrushConverterTests
{
    private DeadlineToBrushConverter _converter = null!;

    [TestInitialize]
    public void Setup()
    {
        _converter = new DeadlineToBrushConverter();
    }

    [TestMethod]
    public void Convert_Overdue_ShouldReturnCrimson()
    {
        // Arrange
        var yesterday = DateTime.Today.AddDays(-1);

        // Act
        var result = _converter.Convert(yesterday, typeof(Brush), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.AreEqual(Brushes.Crimson, result);
    }

    [TestMethod]
    public void Convert_Today_ShouldReturnOrangeRed()
    {
        // Arrange
        var today = DateTime.Today;

        // Act
        var result = _converter.Convert(today, typeof(Brush), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.AreEqual(Brushes.OrangeRed, result);
    }

    [TestMethod]
    public void Convert_NearFuture_ShouldReturnOrange()
    {
        // Arrange
        var threeDaysLater = DateTime.Today.AddDays(3);

        // Act
        var result = _converter.Convert(threeDaysLater, typeof(Brush), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.AreEqual(Brushes.Orange, result);
    }

    [TestMethod]
    public void Convert_FarFuture_ShouldReturnBlack_InUnitTestEnvironment()
    {
        // Arrange
        var nextWeek = DateTime.Today.AddDays(7);

        // Act
        var result = _converter.Convert(nextWeek, typeof(Brush), null!, CultureInfo.InvariantCulture);

        // Assert
        // ユニットテスト環境では Application.Current が null のため、?? Brushes.Black が適用される
        Assert.AreEqual(Brushes.Black, result);
    }

    [TestMethod]
    public void Convert_InvalidValue_ShouldReturnBlack_InUnitTestEnvironment()
    {
        // Act
        var result = _converter.Convert("NotADateTime", typeof(Brush), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.AreEqual(Brushes.Black, result);
    }
}
