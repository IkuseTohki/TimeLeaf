using Microsoft.VisualStudio.TestTools.UnitTesting;
using TimeLeaf.Models.Enums;
using TimeLeaf.ViewModels.Converters;

namespace TimeLeaf.Tests.ViewModels.Converters;

[TestClass]
public class TaskStatusToIconConverterTests
{
    private TaskStatusToIconConverter _converter = null!;

    [TestInitialize]
    public void Setup()
    {
        _converter = new TaskStatusToIconConverter();
    }

    [TestMethod]
    [DataRow(TaskStatus.NotStarted, "⚪")]
    [DataRow(TaskStatus.InProgress, "🔵")]
    [DataRow(TaskStatus.InReview, "🟡")]
    [DataRow(TaskStatus.Completed, "🟢")]
    public void Convert_ShouldReturnCorrectIcon(TaskStatus status, string expectedIcon)
    {
        // Act
        var result = _converter.Convert(status, typeof(string), null!, null!);

        // Assert
        Assert.AreEqual(expectedIcon, result);
    }

    [TestMethod]
    public void Convert_ShouldReturnQuestionMark_WhenValueIsInvalid()
    {
        // Act
        var result = _converter.Convert("InvalidValue", typeof(string), null!, null!);

        // Assert
        Assert.AreEqual("❓", result);
    }
}
