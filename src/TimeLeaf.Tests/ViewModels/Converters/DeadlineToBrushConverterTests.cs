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
    public void Convert_Overdue_ShouldReturnOverdue()
    {
        // Arrange
        var yesterday = DateTime.Today.AddDays(-1);

        // Act
        var result = _converter.Convert(yesterday, typeof(string), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.AreEqual("Overdue", result);
    }

    [TestMethod]
    public void Convert_Today_ShouldReturnToday()
    {
        // Arrange
        var today = DateTime.Today;

        // Act
        var result = _converter.Convert(today, typeof(string), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.AreEqual("Today", result);
    }

    [TestMethod]
    public void Convert_NearFuture_ShouldReturnNear()
    {
        // Arrange
        var threeDaysLater = DateTime.Today.AddDays(3);

        // Act
        var result = _converter.Convert(threeDaysLater, typeof(string), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.AreEqual("Near", result);
    }

    [TestMethod]
    public void Convert_FarFuture_ShouldReturnFuture()
    {
        // Arrange
        var nextWeek = DateTime.Today.AddDays(7);

        // Act
        var result = _converter.Convert(nextWeek, typeof(string), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.AreEqual("Future", result);
    }

    [TestMethod]
    public void Convert_InvalidValue_ShouldReturnNone()
    {
        // Act
        var result = _converter.Convert("NotADateTime", typeof(string), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.AreEqual("None", result);
    }
}
