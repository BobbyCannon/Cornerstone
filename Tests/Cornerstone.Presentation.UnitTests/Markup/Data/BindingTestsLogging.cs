#region References

using System;
using System.Collections.Generic;
using System.Globalization;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Naming;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Data.Converters;
using Cornerstone.Presentation.Data.Core;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Logging;
using Cornerstone.Presentation.Markup.Xaml.MarkupExtensions;
using Cornerstone.Presentation.Markup.Xaml.MarkupExtensions.CompiledBindings;
using Cornerstone.Presentation.Reactive;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Data;

[TestClass]
public class BindingTestsLogging : ScopedTestBase
{
	#region Methods

	private static IDisposable AssertLog(
		PresentationObject target,
		string expression,
		string message,
		string errorPoint = null,
		LogEventLevel level = LogEventLevel.Warning,
		PresentationProperty property = null)
	{
		var logs = new List<LogMessage>();
		var sink = TestLogSink.Start((l, a, s, m, p) =>
		{
			if (l >= level)
			{
				logs.Add(new(l, a, s, m, p));
			}
		});

		return Disposable.Create(() =>
		{
			sink.Dispose();
			CornerstoneTest.AreEqual(1, logs.Count);

			var l = logs[0];
			var messageTemplate = errorPoint is not null ? "An error occurred binding {Property} to {Expression} at {ExpressionErrorPoint}: {Message}" : "An error occurred binding {Property} to {Expression}: {Message}";

			CornerstoneTest.AreEqual(level, l.level);
			CornerstoneTest.AreEqual(LogArea.Binding, l.area);
			CornerstoneTest.AreEqual(target, l.source);
			CornerstoneTest.AreEqual(messageTemplate, l.messageTemplate);
			CornerstoneTest.AreEqual(property ?? Control.TagProperty, l.propertyValues[0]);
			CornerstoneTest.AreEqual(expression, l.propertyValues[1]);

			if (errorPoint is not null)
			{
				CornerstoneTest.AreEqual(errorPoint, l.propertyValues[2]);
				CornerstoneTest.AreEqual(message, l.propertyValues[3]);
			}
			else
			{
				CornerstoneTest.AreEqual(message, l.propertyValues[2]);
			}
		});
	}

	private static IDisposable AssertNoLog()
	{
		var count = 0;
		var sink = TestLogSink.Start((l, a, s, m, p) =>
		{
			if (l >= LogEventLevel.Warning)
			{
				++count;
			}
		});

		return Disposable.Create(() =>
		{
			sink.Dispose();
			CornerstoneTest.AreEqual(0, count);
		});
	}

	private static Type ResolveType(string ns, string typeName)
	{
		return typeName switch
		{
			"TextBlock" => typeof(TextBlock),
			"TestRoot" => typeof(TestRoot),
			_ => throw new InvalidOperationException($"Could not resolve type {typeName}.")
		};
	}

	#endregion

	#region Classes

	[TestClass]
	public class CompiledBinding : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void ShouldLogForInvalidDataContextType()
		{
			var target = new TestRoot { DataContext = 48 };
			var stringLengthProperty = new ClrPropertyInfo(
				"Length",
				x => ((string) x).Length,
				null,
				typeof(int));
			var bindingPath = new CompiledBindingPathBuilder()
				.Property(stringLengthProperty, PropertyInfoAccessorFactory.CreateInpcPropertyAccessor)
				.Build();
			var binding = new CompiledBindingExtension(bindingPath);

			using (AssertLog(
						target,
						bindingPath.ToString(),
						"Unable to cast object of type 'System.Int32' to type 'System.String'.",
						"Length"))
			{
				target.Bind(Control.TagProperty, binding);
			}
		}

		#endregion
	}

	[TestClass]
	public class Converter : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void ShouldLogErrorForUnconvertibleType()
		{
			var target = new Decorator { DataContext = new { Foo = new Version() } };
			var root = new TestRoot(target);
			var binding = new Binding("Foo");

			using (AssertLog(
						target,
						binding.Path,
						"Could not convert '0.0' (System.Version) to 'Cornerstone.Presentation.Thickness'.",
						property: Control.MarginProperty))
			{
				target.Bind(Control.MarginProperty, binding);
			}
		}

		[PresentationTestMethod]
		public void ShouldLogErrorForUnconvertibleTypeWithConverter()
		{
			var target = new Decorator { DataContext = new { Foo = new Version() } };
			var root = new TestRoot(target);
			var binding = new Binding("Foo")
			{
				Converter = new ThrowingConverter()
			};

			using (AssertLog(
						target,
						binding.Path,
						"Could not convert '0.0' (System.Version) to 'Cornerstone.Presentation.Thickness' " +
						"using 'Cornerstone.Presentation.UnitTests.Markup.Data.BindingTestsLogging+ThrowingConverter': " +
						"The method or operation is not implemented.",
						property: Control.MarginProperty))
			{
				target.Bind(Control.MarginProperty, binding);
			}
		}

		#endregion
	}

	[TestClass]
	public class DataContext : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void ShouldLogMissingMemberOnDataContext()
		{
			var target = new Decorator { DataContext = new TestClass("foo") };
			var root = new TestRoot(target);
			var binding = new Binding("Foo.Bar");

			using (AssertLog(
						target,
						binding.Path,
						"Could not find a matching property accessor for 'Bar' on 'System.String'.",
						"Bar"))
			{
				target.Bind(Control.TagProperty, binding);
			}
		}

		[PresentationTestMethod]
		public void ShouldLogNullInBindingChain()
		{
			var target = new Decorator { DataContext = new TestClass() };
			var root = new TestRoot(target);
			var binding = new Binding("Foo.Length");

			using (AssertLog(target, binding.Path, "Value is null.", "Foo"))
			{
				target.Bind(Control.TagProperty, binding);
			}
		}

		[PresentationTestMethod]
		public void ShouldNotLogMissingMemberOnNullDataContext()
		{
			var target = new Decorator();
			var root = new TestRoot(target);
			var binding = new Binding("Foo");

			using (AssertNoLog())
			{
				target.Bind(Control.TagProperty, binding);
			}
		}

		#endregion
	}

	[TestClass]
	public class Fallback : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		[DataRow(true)]
		[DataRow(false)]
		public void ShouldLogInvalidFallbackValue(bool rooted)
		{
			var target = new Decorator();
			var binding = new Binding("foo") { FallbackValue = "bar" };

			if (rooted)
			{
				new TestRoot(target);
			}

			// An invalid fallback value is invalid whether the control is rooted or not.
			using (AssertLog(
						target,
						binding.Path,
						"Could not convert FallbackValue 'bar' to 'System.Double'.",
						level: LogEventLevel.Error,
						property: Visual.OpacityProperty))
			{
				target.Bind(Visual.OpacityProperty, binding);
			}
		}

		[PresentationTestMethod]
		[DataRow(true)]
		[DataRow(false)]
		public void ShouldLogInvalidTargetNullValue(bool rooted)
		{
			var target = new Decorator { DataContext = new { Bar = (string) null } };
			var binding = new Binding("Bar") { TargetNullValue = "foo" };

			if (rooted)
			{
				new TestRoot(target);
			}

			// An invalid target null value is invalid whether the control is rooted or not.
			using (AssertLog(
						target,
						binding.Path,
						"Could not convert TargetNullValue 'foo' to 'System.Double'.",
						level: LogEventLevel.Error,
						property: Visual.OpacityProperty))
			{
				target.Bind(Visual.OpacityProperty, binding);
			}
		}

		#endregion
	}

	[TestClass]
	public class LogicalAncestor : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void ShouldLogAncestorNotFound()
		{
			var target = new Decorator();
			var root = new TestRoot(target);
			var binding = new Binding("$parent[TextBlock]") { TypeResolver = ResolveType };

			using (AssertLog(target, binding.Path, "Ancestor not found.", "$parent[TextBlock]"))
			{
				target.Bind(Control.TagProperty, binding);
			}
		}

		[PresentationTestMethod]
		public void ShouldNotLogAncestorNotFoundForUnrootedControl()
		{
			var target = new Decorator();
			var binding = new Binding("$parent[TextBlock]") { TypeResolver = ResolveType };

			using (AssertNoLog())
			{
				target.Bind(Control.TagProperty, binding);
			}
		}

		#endregion
	}

	[TestClass]
	public class NamedElement : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void ShouldLogNameScopeNotFound()
		{
			var target = new Decorator();
			var root = new TestRoot(target);
			var binding = new Binding("#source") { TypeResolver = ResolveType };

			using (AssertLog(target, binding.Path, "NameScope not found.", "#source"))
			{
				target.Bind(Control.TagProperty, binding);
			}
		}

		[PresentationTestMethod]
		public void ShouldNotLogElementPropertyNullForUnrootedControl()
		{
			var ns = new NameScope();
			var source = new Canvas { Name = "source" };
			var target = new Decorator();
			var binding = new Binding("#source.DataContext.Foo") { TypeResolver = ResolveType, NameScope = new(ns) };
			var container = new StackPanel
			{
				[NameScope.NameScopeProperty] = ns,
				Children = { source, target }
			};

			ns.Register(source.Name, source);

			using (AssertNoLog())
			{
				target.Bind(Control.TagProperty, binding);
			}

			// Sanity check to that the binding works when rooted: make sure that we're not just testing a broken
			// binding!
			using (AssertNoLog())
			{
				var root = new TestRoot(container);
				root.DataContext = new { Foo = "foo" };
				CornerstoneTest.AreEqual("foo", target.Tag);
			}
		}

		#endregion
	}

	[TestClass]
	public class NonControlDataContext : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void ShouldLogMissingMemberOnDataContext()
		{
			var target = new TestRoot();
			var binding = new Binding("Foo") { DefaultAnchor = new(target) };

			target.KeyBindings.Add(new KeyBinding
			{
				Gesture = new KeyGesture(Key.A),
				[!KeyBinding.CommandProperty] = binding
			});

			target.DataContext = new object();

			using (AssertLog(
						target,
						binding.Path,
						"Could not find a matching property accessor for 'Foo' on 'System.Object'.",
						"Foo"))
			{
				target.Bind(Control.TagProperty, binding);
			}
		}

		[PresentationTestMethod]
		public void ShouldNotLogMissingMemberOnNullDataContext()
		{
			var target = new TestRoot();
			var binding = new Binding("Foo") { DefaultAnchor = new(target) };

			target.KeyBindings.Add(new KeyBinding
			{
				Gesture = new KeyGesture(Key.A),
				[!KeyBinding.CommandProperty] = binding
			});

			using (AssertNoLog())
			{
				target.Bind(Control.TagProperty, binding);
			}
		}

		#endregion
	}

	[TestClass]
	public class Source : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void ShouldLogNullSource()
		{
			var target = new Decorator();
			var root = new TestRoot(target);
			var binding = new Binding("Foo") { Source = null };

			using (AssertLog(target, binding.Path, "Binding Source is null.", "(source)"))
			{
				target.Bind(Control.TagProperty, binding);
			}
		}

		[PresentationTestMethod]
		public void ShouldLogNullSourceForUnrootedControl()
		{
			var target = new Decorator();
			var binding = new Binding("Foo") { Source = null };

			using (AssertLog(target, binding.Path, "Binding Source is null.", "(source)"))
			{
				target.Bind(Control.TagProperty, binding);
			}
		}

		#endregion
	}

	[TestClass]
	public class VisualAncestor : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void ShouldLogAncestorNotFound()
		{
			var target = new Decorator();
			var root = new TestRoot(target);
			var binding = new Binding
			{
				RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor)
				{
					AncestorType = typeof(TextBlock)
				}
			};

			using (AssertLog(target, "$visualParent[TextBlock]", "Ancestor not found.", "$visualParent[TextBlock]"))
			{
				target.Bind(Control.TagProperty, binding);
			}
		}

		[PresentationTestMethod]
		public void ShouldLogAncestorPropertyNotFound()
		{
			var target = new Decorator();
			var root = new TestRoot(target);
			var binding = new Binding("Foo")
			{
				RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor)
				{
					AncestorType = typeof(TestRoot)
				}
			};

			using (AssertLog(
						target,
						"$visualParent[TestRoot].Foo",
						"Could not find a matching property accessor for 'Foo' on 'Cornerstone.Presentation.UnitTests.Helpers.TestRoot'.",
						"Foo"))
			{
				target.Bind(Control.TagProperty, binding);
			}
		}

		[PresentationTestMethod]
		public void ShouldNotLogAncestorNotFoundForUnrootedControl()
		{
			var target = new Decorator();
			var binding = new Binding
			{
				RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor)
				{
					AncestorType = typeof(Window)
				}
			};

			using (AssertNoLog())
			{
				target.Bind(Control.TagProperty, binding);
			}
		}

		#endregion
	}

	private class TestClass
	{
		#region Constructors

		public TestClass(string foo = null)
		{
			Foo = foo;
		}

		#endregion

		#region Properties

		public string Foo { get; set; }

		#endregion
	}

	private class ThrowingConverter : IValueConverter
	{
		#region Methods

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		#endregion
	}

	#endregion

	#region Records

	private record LogMessage(LogEventLevel level, string area, object source, string messageTemplate, params object[] propertyValues);

	#endregion
}