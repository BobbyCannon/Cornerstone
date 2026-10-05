#region References

using System.ComponentModel;
using System.Runtime.CompilerServices;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Data;

[TestClass]
public class BindingTestsSource : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void SourceShouldBeUsed()
	{
		var source = new Source { Foo = "foo" };
		var binding = new Binding { Source = source, Path = "Foo" };
		var target = new TextBlock();

		target.Bind(TextBlock.TextProperty, binding);

		CornerstoneTest.AreEqual("foo", target.Text);
	}

	#endregion

	#region Classes

	public class Source : INotifyPropertyChanged
	{
		#region Fields

		private string _foo;

		#endregion

		#region Properties

		public string Foo
		{
			get => _foo;
			set
			{
				_foo = value;
				RaisePropertyChanged();
			}
		}

		#endregion

		#region Methods

		private void RaisePropertyChanged([CallerMemberName] string prop = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
		}

		#endregion

		#region Events

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion
	}

	#endregion
}