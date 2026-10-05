#region References

using System;
using Cornerstone.Presentation.Automation;
using Cornerstone.Presentation.Automation.Peers;
using Cornerstone.Presentation.Automation.Provider;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Automation;

[TestClass]
public class ColorSpectrumAutomationPeerTests
{
	#region Classes

	[TestClass]
	public class AutomationPeerTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void ClassNameIsColorSpectrum()
		{
			var spectrum = new ColorSpectrum();
			var peer = (ColorSpectrumAutomationPeer) ControlAutomationPeer.CreatePeerForElement(spectrum);

			CornerstoneTest.AreEqual("ColorSpectrum", peer.GetClassName());
		}

		[PresentationTestMethod]
		public void ControlTypeIsCustom()
		{
			var spectrum = new ColorSpectrum();
			var peer = (ColorSpectrumAutomationPeer) ControlAutomationPeer.CreatePeerForElement(spectrum);

			CornerstoneTest.AreEqual(AutomationControlType.Custom, peer.GetAutomationControlType());
		}

		[PresentationTestMethod]
		public void CreatesColorSpectrumAutomationPeer()
		{
			var spectrum = new ColorSpectrum();
			var peer = ControlAutomationPeer.CreatePeerForElement(spectrum);

			CornerstoneTest.IsType<ColorSpectrumAutomationPeer>(peer);
		}

		[PresentationTestMethod]
		public void ImplementsIValueProvider()
		{
			var spectrum = new ColorSpectrum();
			var peer = (ColorSpectrumAutomationPeer) ControlAutomationPeer.CreatePeerForElement(spectrum);

			CornerstoneTest.AreEqual(Colors.White.ToString(), peer.Value);

			var valueProvider = CornerstoneTest.IsAssignableFrom<IValueProvider>(peer);
			valueProvider.SetValue("#00FF00");

			CornerstoneTest.AreEqual(Colors.Lime.ToString(), peer.Value);
			CornerstoneTest.AreEqual(Colors.Lime, spectrum.Color);
		}

		[PresentationTestMethod]
		public void SetValueUsesColorParseAndThrowsFormatExceptionOnInvalidInput()
		{
			var spectrum = new ColorSpectrum();
			var peer = (ColorSpectrumAutomationPeer) ControlAutomationPeer.CreatePeerForElement(spectrum);
			var valueProvider = CornerstoneTest.IsAssignableFrom<IValueProvider>(peer);

			Assert.Throws<FormatException>(() => valueProvider.SetValue("not-a-color"));
		}

		[PresentationTestMethod]
		public void ValuePropertyRaisesAutomationPropertyChangedEventOnColorChange()
		{
			var spectrum = new ColorSpectrum();
			var peer = (ColorSpectrumAutomationPeer) ControlAutomationPeer.CreatePeerForElement(spectrum);
			AutomationPropertyChangedEventArgs changed = null;

			peer.PropertyChanged += (_, e) =>
			{
				if (e.Property == ValuePatternIdentifiers.ValueProperty)
				{
					changed = e;
				}
			};

			spectrum.Color = Colors.Black;

			CornerstoneTest.IsNotNull(changed);
			CornerstoneTest.AreEqual(ValuePatternIdentifiers.ValueProperty, changed!.Property);
			CornerstoneTest.AreEqual(Colors.White.ToString(), changed.OldValue);
			CornerstoneTest.AreEqual(Colors.Black.ToString(), changed.NewValue);
		}

		#endregion
	}

	#endregion
}