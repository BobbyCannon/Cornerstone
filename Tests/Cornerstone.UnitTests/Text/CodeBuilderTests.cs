#region References

using System;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Collections;
using Cornerstone.Reflection;
using Cornerstone.Sample.Models;
using Cornerstone.Text.CodeGenerators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Text;

[TestClass]
public class CodeBuilderTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void AccessibilityToString()
	{
		AreEqual("", CodeBuilder.AccessibilityToString(SourceAccessibility.None));
		AreEqual("internal", CodeBuilder.AccessibilityToString(SourceAccessibility.Internal));
		AreEqual("public", CodeBuilder.AccessibilityToString(SourceAccessibility.Public));
		AreEqual("private", CodeBuilder.AccessibilityToString(SourceAccessibility.Private));
		AreEqual("private protected", CodeBuilder.AccessibilityToString(SourceAccessibility.ProtectedAndInternal));
		AreEqual("protected", CodeBuilder.AccessibilityToString(SourceAccessibility.Protected));
		AreEqual("protected internal", CodeBuilder.AccessibilityToString(SourceAccessibility.ProtectedOrInternal));
	}

	[TestMethod]
	[SuppressMessage("ReSharper", "ConvertNullableToShortForm")]
	public void GetCodeTypeName()
	{
		var scenarios = new (Type Type, string Expected)[]
		{
			(typeof(PresentationList<int>), "PresentationList<int>"),
			(typeof(SpeedyList<int>), "SpeedyList<int>"),
			(typeof(Nullable<int>), "int?")
		};

		foreach (var scenario in scenarios)
		{
			var actual = CodeBuilder.GetCodeTypeName(scenario.Type);
			AreEqual(scenario.Expected, actual);
		}
	}

	[TestMethod]
	public void TryAppendLiteral()
	{
		var items = new object[]
		{
			null, true, false, "foo", 1.23456f, 6.54321d
		};

		var builder = new CodeBuilder();
		var first = true;

		foreach (var item in items)
		{
			if (!first)
			{
				builder.WriteLine();
			}

			IsTrue(builder.TryAppendLiteral(item));
			first = false;
		}

		var expected = """
						null
						true
						false
						"foo"
						1.23456001
						6.5432100000000002
						""";

		AreEqual(expected, builder.ToString());

		// Not a literal so this should be false
		IsFalse(builder.TryAppendLiteral(new Exception()));
	}

	[TestMethod]
	public void TryDetectIndent()
	{
		var scenarios = new (string Content, char IndentChar, uint IndentLength, uint Indent)[]
		{
			("", '\t', 1, 0),
			("  \t", ' ', 4, 0),
			("  ", ' ', 4, 0),
			("    ", ' ', 4, 1),
			("\t\t\t", '\t', 1, 3)
		};

		var builder = new CodeBuilder();

		foreach (var scenario in scenarios)
		{
			builder.TryDetectIndent(scenario.Content);
			AreEqual(scenario.IndentChar, builder.Settings.IndentChar);
			AreEqual(scenario.IndentLength, builder.Settings.IndentLength);
			AreEqual(scenario.Indent, builder.Indent);
		}
	}

	[TestMethod]
	public void WriteObjectDeclarationForType()
	{
		var builder = new CodeBuilder
		{
			Settings = { DesiredOutput = CodeBuilderOutput.Declaration }
		};
		var scenarios = new (object Value, string Expected)[]
		{
			(
				new Account(),
				"""
				public class Account
				{
					public DateTime CreatedOn { get; set; }
					public string EmailAddress { get; set; }
					public bool IsDeleted { get; set; }
					public DateTime LastLoginDate { get; set; }
					public DateTime ModifiedOn { get; set; }
					public string Name { get; set; }
					public string Picture { get; set; }
					public string Roles { get; set; }
					public AccountStatus Status { get; set; }
					public Guid SyncId { get; set; }
					public string TimeZoneId { get; set; }
				}
				"""
			)
		};

		foreach (var scenario in scenarios)
		{
			builder.Clear();
			builder.WriteObject(scenario.Value);
			AreEqual(scenario.Expected, builder.ToString());
		}
	}

	[TestMethod]
	public void WriteObjectInstanceForPrimitiveTypes()
	{
		var builder = new CodeBuilder
		{
			Settings = { DesiredOutput = CodeBuilderOutput.Instance }
		};
		var scenarios = new (object Value, string Expected)[]
		{
			(null, "null"),
			(true, "true"),
			((bool?) true, "true"),
			(false, "false"),
			((bool?) false, "false"),

			// ReSharper disable once RedundantCast
			((bool?) null, "null")
		};

		foreach (var scenario in scenarios)
		{
			builder.Clear();
			builder.WriteObject(scenario.Value);
			AreEqual(scenario.Expected, builder.ToString());
		}
	}

	[TestMethod]
	public void WriteObjectInstanceForType()
	{
		var builder = new CodeBuilder
		{
			Settings = { DesiredOutput = CodeBuilderOutput.Instance, IgnoreDefaults = false }
		};
		var scenarios = new (object Value, string Expected)[]
		{
			(
				new AccountEntity
				{
					EmailAddress = "john@domain.com",
					Name = "John"
				},
				"""
				new AccountEntity
				{
					CreatedOn = DateTime.MinValue,
					EmailAddress = "john@domain.com",
					Id = 0,
					IsDeleted = false,
					LastLoginDate = DateTime.MinValue,
					ModifiedOn = DateTime.MinValue,
					Name = "John",
					Picture = null,
					Roles = null,
					Status = AccountStatus.Unknown,
					SyncId = Guid.Empty,
					TimeZoneId = null
				}
				"""
			)
		};

		foreach (var scenario in scenarios)
		{
			builder.Clear();
			builder.WriteObject(scenario.Value);
			AreEqual(scenario.Expected, builder.ToString());
		}
	}

	[TestMethod]
	public void WriteObjectInstanceWithFunc()
	{
		var builder = new CodeBuilder { Settings = { DesiredOutput = CodeBuilderOutput.Instance, IgnoreDefaults = false } };
		var scenarios = new (FuncTest Value, string Expected)[]
		{
			(
				new FuncTest
				{
					AccountFactory = () => []
				},
				"""
				new FuncTest
				{
					AccountFactory = () => []
				}
				"""
			),
			(
				new FuncTest
				{
					AccountFactory = () =>
					[
						new Account
						{
							Name = "John"
						}
					]
				},
				"""
				new FuncTest
				{
					AccountFactory = () =>
					[
						new Account
						{
							CreatedOn = DateTime.MinValue,
							EmailAddress = null,
							IsDeleted = false,
							LastLoginDate = DateTime.MinValue,
							ModifiedOn = DateTime.MinValue,
							Name = "John",
							Picture = null,
							Roles = null,
							Status = AccountStatus.Unknown,
							SyncId = Guid.Empty,
							TimeZoneId = null
						}
					]
				}
				"""
			)
		};

		foreach (var scenario in scenarios)
		{
			builder.Clear();

			var accounts = scenario.Value.AccountFactory?.Invoke();
			IsNotNull(accounts);

			builder.WriteObject(scenario.Value);
			AreEqual(scenario.Expected, builder.ToString());
		}
	}

	#endregion

	#region Classes

	[SourceReflection]
	public class FuncTest
	{
		#region Properties

		public Func<Account[]> AccountFactory { get; init; }

		public Account[] Accounts => field ??= AccountFactory?.Invoke();

		#endregion
	}

	#endregion
}