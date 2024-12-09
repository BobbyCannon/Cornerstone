#region References

using System.Collections.Generic;
using System.ComponentModel;
using Cornerstone.Sync;
using Cornerstone.Weaver.TestAssembly;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;

#endregion

namespace Cornerstone.UnitTests.Weaver;

[TestClass]
public class PropertyChangeTest : CornerstoneUnitTest
{
	#region Methods

	[Theory]
	public void DependsOn(AccountType type)
	{
		var changed = new List<string>();
		var changing = new List<string>();
		var account = GetAccount(type);

		if (account is INotifyPropertyChanging propertyChanging)
		{
			propertyChanging.PropertyChanging += (_, args) => changing.Add(args.PropertyName);
		}

		if (account is INotifyPropertyChanged propertyChanged)
		{
			propertyChanged.PropertyChanged += (_, args) => changed.Add(args.PropertyName);
		}

		account.FirstName = "Bob";
		account.LastName = "Doe";

		if (account is INotifyPropertyChanging)
		{
			AreEqual(
				new[]
				{
					nameof(AccountINotifyPropertyChanged.FullName),
					nameof(AccountINotifyPropertyChanged.FirstName),
					nameof(AccountINotifyPropertyChanged.FullName),
					nameof(AccountINotifyPropertyChanged.LastName)
				},
				changing
			);
		}

		if (account is INotifyPropertyChanged)
		{
			AreEqual(
				new[]
				{
					nameof(AccountINotifyPropertyChanged.FullName),
					nameof(AccountINotifyPropertyChanged.FirstName),
					nameof(AccountINotifyPropertyChanged.FullName),
					nameof(AccountINotifyPropertyChanged.LastName)
				},
				changed
			);
		}
	}

	[TestMethod]
	public void PropertyChange()
	{
		var option = new SyncSettings();
		var changed = new List<string>();
		var changing = new List<string>();

		option.PropertyChanging += (_, args) => changing.Add(args.PropertyName);
		option.PropertyChanged += (_, args) => changed.Add(args.PropertyName);
		option.IncludeIssueDetails = true;

		AreEqual(new[] { nameof(option.IncludeIssueDetails) }, changing);
		AreEqual(new[] { nameof(option.IncludeIssueDetails) }, changed);
	}

	private IAccount GetAccount(AccountType type)
	{
		return type switch
		{
			AccountType.AccountBindable => new AccountBindable(),
			AccountType.AccountINotifyPropertyChanged => new AccountINotifyPropertyChanged(),
			AccountType.AccountNotifiable => new AccountNotifiable(),
			_ => new AccountINotifyPropertyChanged()
		};
	}

	#endregion
}