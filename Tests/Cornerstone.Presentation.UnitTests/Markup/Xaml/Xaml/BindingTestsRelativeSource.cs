#nullable enable

#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;

[TestClass]
public class BindingTestsRelativeSource : XamlTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void BindingToAncestorOfTypeWithShorthandWorks()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;assembly=Cornerstone.Presentation.UnitTests'>
    <Border Name='border1'>
      <Border Name='border2'>
        <Button Name='button' Content='{Binding $parent[Border].Name}'/>
      </Border>
    </Border>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var button = window.GetControl<Button>("button");

			window.ApplyTemplate();

			CornerstoneTest.AreEqual("border2", button.Content);
		}
	}

	[PresentationTestMethod]
	public void BindingToAncestorWithNamespaceWorks()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<local:TestWindow xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;assembly=Cornerstone.Presentation.UnitTests'
        Title='title'>
  <Button Name='button' Content='{Binding Title, RelativeSource={RelativeSource AncestorType=local:TestWindow}}'/>
</local:TestWindow>";
			var window = (TestWindow) CornerstoneRuntimeXamlLoader.Load(xaml);
			var button = window.GetControl<Button>("button");

			window.ApplyTemplate();
			window.Presenter!.ApplyTemplate();

			CornerstoneTest.AreEqual("title", button.Content);
		}
	}

	[PresentationTestMethod]
	public void BindingToDataContextWorks()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;assembly=Cornerstone.Presentation.UnitTests'>
    <Button Name='button' Content='{Binding Foo, RelativeSource={RelativeSource DataContext}}'/>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var button = window.GetControl<Button>("button");

			button.DataContext = new { Foo = "foo" };
			window.ApplyTemplate();

			CornerstoneTest.AreEqual("foo", button.Content);
		}
	}

	[PresentationTestMethod]
	public void BindingToFirstAncestorWithShorthandUsesLogicalTree()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;assembly=Cornerstone.Presentation.UnitTests'>
    <Border Name='border'>
      <ContentControl Name='contentControl'>
        <Button Name='button' Content='{Binding $parent.Name}'/>
      </ContentControl>
    </Border>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var contentControl = window.GetControl<ContentControl>("contentControl");
			var button = window.GetControl<Button>("button");

			window.ApplyTemplate();

			CornerstoneTest.AreEqual("contentControl", button.Content);
		}
	}

	[PresentationTestMethod]
	public void BindingToFirstAncestorWithShorthandWorks()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;assembly=Cornerstone.Presentation.UnitTests'>
    <Border Name='border1'>
      <Border Name='border2'>
        <Button Name='button' Content='{Binding $parent.Name}'/>
      </Border>
    </Border>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var button = window.GetControl<Button>("button");

			window.ApplyTemplate();

			CornerstoneTest.AreEqual("border2", button.Content);
		}
	}

	[PresentationTestMethod]
	public void BindingToFirstAncestorWorks()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;assembly=Cornerstone.Presentation.UnitTests'>
    <Border Name='border1'>
      <Border Name='border2'>
        <Button Name='button' Content='{Binding Name, RelativeSource={RelativeSource AncestorType=Border}}'/>
      </Border>
    </Border>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var button = window.GetControl<Button>("button");

			window.ApplyTemplate();
			window.Presenter!.ApplyTemplate();

			CornerstoneTest.AreEqual("border2", button.Content);
		}
	}

	[PresentationTestMethod]
	public void BindingToSecondAncestorWithShorthandAndTypeWorks()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;assembly=Cornerstone.Presentation.UnitTests'>
    <Border Name='border1'>
      <Border Name='border2'>
        <Button Name='button' Content='{Binding $parent[Border; 1].Name}'/>
      </Border>
    </Border>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var button = window.GetControl<Button>("button");

			window.ApplyTemplate();

			CornerstoneTest.AreEqual("border1", button.Content);
		}
	}

	[PresentationTestMethod]
	public void BindingToSecondAncestorWithShorthandUsesLogicalTree()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;assembly=Cornerstone.Presentation.UnitTests'>
    <ContentControl Name='contentControl1'>
      <ContentControl Name='contentControl2'>
        <Button Name='button' Content='{Binding $parent[1].Name}'/>
      </ContentControl>
    </ContentControl>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var contentControl1 = window.GetControl<ContentControl>("contentControl1");
			var contentControl2 = window.GetControl<ContentControl>("contentControl2");
			var button = window.GetControl<Button>("button");

			window.ApplyTemplate();

			CornerstoneTest.AreEqual("contentControl1", button.Content);
		}
	}

	[PresentationTestMethod]
	public void BindingToSecondAncestorWithShorthandWorks()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;assembly=Cornerstone.Presentation.UnitTests'>
    <Border Name='border1'>
      <Border Name='border2'>
        <Button Name='button' Content='{Binding $parent[1].Name}'/>
      </Border>
    </Border>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var button = window.GetControl<Button>("button");

			window.ApplyTemplate();

			CornerstoneTest.AreEqual("border1", button.Content);
		}
	}

	[PresentationTestMethod]
	public void BindingToSecondAncestorWorks()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;assembly=Cornerstone.Presentation.UnitTests'>
    <Border Name='border1'>
      <Border Name='border2'>
        <Button Name='button' Content='{Binding Name, RelativeSource={RelativeSource AncestorType=Border, AncestorLevel=2}}'/>
      </Border>
    </Border>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var button = window.GetControl<Button>("button");

			window.ApplyTemplate();
			window.Presenter!.ApplyTemplate();

			CornerstoneTest.AreEqual("border1", button.Content);
		}
	}

	[PresentationTestMethod]
	public void BindingToSelfWorks()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;assembly=Cornerstone.Presentation.UnitTests'>
    <Button Name='button' Content='{Binding Name, RelativeSource={RelativeSource Self}}'/>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var button = window.GetControl<Button>("button");

			window.ApplyTemplate();

			CornerstoneTest.AreEqual("button", button.Content);
		}
	}

	[PresentationTestMethod]
	public void ShorthandBindingWithMultipleNegationWorks()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;assembly=Cornerstone.Presentation.UnitTests'>
    <Border Name='border1'>
      <Border Name='border2'>
        <Button Name='button' Content='{Binding !!$self.IsDefault}'/>
      </Border>
    </Border>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var button = window.GetControl<Button>("button");

			window.ApplyTemplate();

			CornerstoneTest.AreEqual(false, button.Content);
		}
	}

	[PresentationTestMethod]
	public void ShorthandBindingWithNegationWorks()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;assembly=Cornerstone.Presentation.UnitTests'>
    <Border Name='border1'>
      <Border Name='border2'>
        <Button Name='button' Content='{Binding !$self.IsDefault}'/>
      </Border>
    </Border>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var button = window.GetControl<Button>("button");

			window.ApplyTemplate();

			CornerstoneTest.AreEqual(true, button.Content);
		}
	}

	#endregion
}

public class TestWindow : Window
{
}