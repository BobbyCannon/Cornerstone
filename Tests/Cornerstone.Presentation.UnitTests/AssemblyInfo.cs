#region References

using Cornerstone.Presentation.Headless;
using Cornerstone.Presentation.UnitTests.Headless;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

[assembly: DoNotParallelize]
[assembly: DiscoverInternals]
[assembly: PresentationTestApplication(typeof(TestApplication))]