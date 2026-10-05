#nullable enable
using Cornerstone.Generators.Presentation.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cornerstone.Generators.UnitTests.Presentation.NameGenerator;

[TestClass]
public class GlobPatternTests
{
    [TestMethod]
    [DataRow("*", "anything", true)]
    [DataRow("", "anything", false)]
    [DataRow("Views/*", "Views/SignUpView.xaml", true)]
    [DataRow("Views/*", "Extensions/SignUpView.xaml", false)]
    [DataRow("*SignUpView*", "Extensions/SignUpView.xaml", true)]
    [DataRow("*SignUpView.paml", "Extensions/SignUpView.xaml", false)]
    [DataRow("*.xaml", "Extensions/SignUpView.xaml", true)]
    public void ShouldMatchGlobExpressions(string pattern, string value, bool matches)
    {
        Assert.AreEqual(matches, new GlobPattern(pattern).Matches(value));
    }

    [TestMethod]
    [DataRow("Views/SignUpView.xaml", true, new[] { "*.xaml", "Extensions/*" })]
    [DataRow("Extensions/SignUpView.paml", true, new[] { "*.xaml", "Extensions/*" })]
    [DataRow("Extensions/SignUpView.paml", false, new[] { "*.xaml", "Views/*" })]
    [DataRow("anything", true, new[] { "*", "*" })]
    [DataRow("anything", false, new[] { "", "" })]
    public void ShouldMatchGlobPatternGroups(string value, bool matches, string[] patterns)
    {
        Assert.AreEqual(matches, new GlobPatternGroup(patterns).Matches(value));
    }
}
