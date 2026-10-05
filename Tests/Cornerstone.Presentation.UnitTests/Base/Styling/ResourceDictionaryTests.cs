#region References

using System;
using Cornerstone.Presentation.Controls.Resources;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Styling;

[TestClass]
public class ResourceDictionaryTests
{
	#region Methods

	[PresentationTestMethod]
	public void AddOwnerSetsMergedDictionaryOwner()
	{
		var host = new StubResourceHost();

		var target = new ResourceDictionary
		{
			MergedDictionaries =
			{
				new ResourceDictionary()
			}
		};

		((IResourceProvider) target).AddOwner(host);

		CornerstoneTest.Same(host, target.Owner);
		CornerstoneTest.Same(host, ((ResourceDictionary) target.MergedDictionaries[0]).Owner);
	}

	[PresentationTestMethod]
	public void CanAddNullValue()
	{
		var target = new StubResourceDictionary();
		target.Add("null", null);
	}

	[PresentationTestMethod]
	public void CannotAddNullKey()
	{
		var target = new StubResourceDictionary();
		Assert.Throws<ArgumentNullException>(() => target.Add(null!, "null"));
	}

	[PresentationTestMethod]
	public void NotifyHostedResourcesChangedShouldBeCalledOnAddOwner()
	{
		var host = new StubResourceHost();
		var target = new ResourceDictionary { { "foo", "bar" } };

		((IResourceProvider) target).AddOwner(host);

		host.Calls.VerifyCalled("NotifyHostedResourcesChanged");
	}

	[PresentationTestMethod]
	public void NotifyHostedResourcesChangedShouldBeCalledOnMergedDictionaryAdd()
	{
		var host = new StubResourceHost();
		var target = new ResourceDictionary(host);

		host.Calls.Clear();
		target.MergedDictionaries.Add(new ResourceDictionary
		{
			{ "foo", "bar" }
		});

		host.Calls.VerifyCalled("NotifyHostedResourcesChanged", 1);
	}

	[PresentationTestMethod]
	public void NotifyHostedResourcesChangedShouldBeCalledOnMergedDictionaryRemove()
	{
		var host = new StubResourceHost();
		var target = new ResourceDictionary(host)
		{
			MergedDictionaries =
			{
				new ResourceDictionary { { "foo", "bar" } }
			}
		};

		host.Calls.Clear();
		target.MergedDictionaries.RemoveAt(0);

		host.Calls.VerifyCalled("NotifyHostedResourcesChanged", 1);
	}

	[PresentationTestMethod]
	public void NotifyHostedResourcesChangedShouldBeCalledOnMergedDictionaryResourceAdd()
	{
		var host = new StubResourceHost();
		var target = new ResourceDictionary(host)
		{
			MergedDictionaries =
			{
				new ResourceDictionary()
			}
		};

		host.Calls.Clear();
		((IResourceDictionary) target.MergedDictionaries[0]).Add("foo", "bar");

		host.Calls.VerifyCalled("NotifyHostedResourcesChanged", 1);
	}

	[PresentationTestMethod]
	public void NotifyHostedResourcesChangedShouldBeCalledOnRemoveOwner()
	{
		var host = new StubResourceHost();
		var target = new ResourceDictionary { { "foo", "bar" } };

		((IResourceProvider) target).AddOwner(host);
		host.Calls.Clear();
		((IResourceProvider) target).RemoveOwner(host);

		host.Calls.VerifyCalled("NotifyHostedResourcesChanged");
	}

	[PresentationTestMethod]
	public void NotifyHostedResourcesChangedShouldBeCalledOnResourceAdd()
	{
		var host = new StubResourceHost();
		var target = new ResourceDictionary(host);

		host.Calls.Clear();
		target.Add("foo", "bar");

		host.Calls.VerifyCalled("NotifyHostedResourcesChanged");
	}

	[PresentationTestMethod]
	public void NotifyHostedResourcesChangedShouldNotBeCalledOnEmptyMergedDictionaryAdd()
	{
		var host = new StubResourceHost();
		var target = new ResourceDictionary(host);

		host.Calls.Clear();
		target.MergedDictionaries.Add(new ResourceDictionary());

		host.Calls.VerifyNotCalled("NotifyHostedResourcesChanged");
	}

	[PresentationTestMethod]
	public void RemoveOwnerClearsMergedDictionaryOwner()
	{
		var host = new StubResourceHost();

		var target = new ResourceDictionary(host)
		{
			MergedDictionaries =
			{
				new ResourceDictionary()
			}
		};

		((IResourceProvider) target).RemoveOwner(host);

		CornerstoneTest.IsNull(target.Owner);
		CornerstoneTest.IsNull(((ResourceDictionary) target.MergedDictionaries[0]).Owner);
	}

	[PresentationTestMethod]
	public void SetsAddedMergedDictionaryOwner()
	{
		var host = new StubResourceHost();

		var target = new ResourceDictionary(host);
		target.MergedDictionaries.Add(new ResourceDictionary());

		CornerstoneTest.Same(host, target.Owner);
		CornerstoneTest.Same(host, ((ResourceDictionary) target.MergedDictionaries[0]).Owner);
	}

	[PresentationTestMethod]
	public void TryGetResourceShouldFindResource()
	{
		var target = new ResourceDictionary
		{
			{ "foo", "bar" }
		};

		CornerstoneTest.IsTrue(target.TryGetResource("foo", null, out var result));
		CornerstoneTest.AreEqual("bar", result);
	}

	[PresentationTestMethod]
	public void TryGetResourceShouldFindResourceFromItselfBeforeMergedDictionary()
	{
		var target = new ResourceDictionary
		{
			{ "foo", "bar" }
		};

		target.MergedDictionaries.Add(new ResourceDictionary
		{
			{ "foo", "baz" }
		});

		CornerstoneTest.IsTrue(target.TryGetResource("foo", null, out var result));
		CornerstoneTest.AreEqual("bar", result);
	}

	[PresentationTestMethod]
	public void TryGetResourceShouldFindResourceFromLaterMergedDictionary()
	{
		var target = new ResourceDictionary
		{
			MergedDictionaries =
			{
				new ResourceDictionary
				{
					{ "foo", "bar" }
				},
				new ResourceDictionary
				{
					{ "foo", "baz" }
				}
			}
		};

		CornerstoneTest.IsTrue(target.TryGetResource("foo", null, out var result));
		CornerstoneTest.AreEqual("baz", result);
	}

	[PresentationTestMethod]
	public void TryGetResourceShouldFindResourceFromMergedDictionary()
	{
		var target = new ResourceDictionary
		{
			MergedDictionaries =
			{
				new ResourceDictionary
				{
					{ "foo", "bar" }
				}
			}
		};

		CornerstoneTest.IsTrue(target.TryGetResource("foo", null, out var result));
		CornerstoneTest.AreEqual("bar", result);
	}

	#endregion
}