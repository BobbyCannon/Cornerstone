#region References

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Data.Converters;
using Cornerstone.Presentation.Data.Core;
using Cornerstone.Presentation.Data.Core.ExpressionNodes;
using Cornerstone.Presentation.Data.Core.Parsers;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Data.Core;

[InvariantCulture]
[TestClass]
public abstract partial class BindingExpressionTests
{
	#region Methods

	protected TargetClass CreateTarget<TIn, TOut>(
		Expression<Func<TIn, TOut>> expression,
		PresentationProperty targetProperty = null,
		IValueConverter converter = null,
		object converterParameter = null,
		object dataContext = null,
		bool enableDataValidation = false,
		Optional<object> fallbackValue = default,
		BindingMode mode = BindingMode.OneWay,
		RelativeSource relativeSource = null,
		Optional<TIn> source = default,
		object targetNullValue = null,
		string stringFormat = null)
		where TIn : class
	{
		var (target, _) = CreateTargetAndExpression(
			expression,
			targetProperty,
			converter,
			converterParameter,
			dataContext,
			enableDataValidation,
			fallbackValue,
			mode,
			relativeSource,
			source,
			targetNullValue,
			stringFormat);
		return target;
	}

	protected TargetClass CreateTargetWithSource<TIn, TOut>(
		TIn source,
		Expression<Func<TIn, TOut>> expression,
		PresentationProperty targetProperty = null,
		IValueConverter converter = null,
		object converterParameter = null,
		bool enableDataValidation = false,
		Optional<object> fallbackValue = default,
		BindingMode mode = BindingMode.OneWay,
		RelativeSource relativeSource = null,
		object targetNullValue = null,
		string stringFormat = null,
		UpdateSourceTrigger updateSourceTrigger = UpdateSourceTrigger.PropertyChanged)
		where TIn : class
	{
		var (target, _) = CreateTargetAndExpression(
			expression,
			targetProperty,
			converter,
			converterParameter,
			null,
			enableDataValidation,
			fallbackValue,
			mode,
			relativeSource,
			source,
			targetNullValue,
			stringFormat,
			updateSourceTrigger);
		return target;
	}

	private static IDisposable StartWithFocusSupport()
	{
		return UnitTestApplication.Start(TestServices.RealFocus);
	}

	private protected (TargetClass, BindingExpression) CreateTargetAndExpression<TIn, TOut>(
		Expression<Func<TIn, TOut>> expression,
		PresentationProperty targetProperty = null,
		IValueConverter converter = null,
		object converterParameter = null,
		object dataContext = null,
		bool enableDataValidation = false,
		Optional<object> fallbackValue = default,
		BindingMode mode = BindingMode.OneWay,
		RelativeSource relativeSource = null,
		Optional<TIn> source = default,
		object targetNullValue = null,
		string stringFormat = null,
		UpdateSourceTrigger updateSourceTrigger = UpdateSourceTrigger.PropertyChanged)
		where TIn : class
	{
		targetProperty ??= typeof(TOut) switch
		{
			var t when t == typeof(bool) => TargetClass.BoolProperty,
			var t when t == typeof(double) => TargetClass.DoubleProperty,
			var t when t == typeof(int) => TargetClass.IntProperty,
			var t when t == typeof(string) => TargetClass.StringProperty,
			_ => TargetClass.ObjectProperty
		};

		return CreateTargetCore(
			expression,
			targetProperty,
			converter,
			converterParameter,
			dataContext,
			enableDataValidation,
			fallbackValue,
			mode,
			relativeSource,
			source,
			targetNullValue,
			stringFormat,
			updateSourceTrigger);
	}

	private protected abstract (TargetClass, BindingExpression) CreateTargetCore<TIn, TOut>(
		Expression<Func<TIn, TOut>> expression,
		PresentationProperty targetProperty,
		IValueConverter converter,
		object converterParameter,
		object dataContext,
		bool enableDataValidation,
		Optional<object> fallbackValue,
		BindingMode mode,
		RelativeSource relativeSource,
		Optional<TIn> source,
		object targetNullValue,
		string stringFormat,
		UpdateSourceTrigger updateSourceTrigger)
		where TIn : class;

	#endregion

	#region Classes

	public partial class Compiled : BindingExpressionTests
	{
		#region Methods

		private protected override (TargetClass, BindingExpression) CreateTargetCore<TIn, TOut>(
			Expression<Func<TIn, TOut>> expression,
			PresentationProperty targetProperty,
			IValueConverter converter,
			object converterParameter,
			object dataContext,
			bool enableDataValidation,
			Optional<object> fallbackValue,
			BindingMode mode,
			RelativeSource relativeSource,
			Optional<TIn> source,
			object targetNullValue,
			string stringFormat,
			UpdateSourceTrigger updateSourceTrigger)
		{
			var target = new TargetClass { DataContext = dataContext };
			var nodes = new List<ExpressionNode>();
			var fallback = fallbackValue.HasValue ? fallbackValue.Value : PresentationProperty.UnsetValue;
			var path = BindingExpressionVisitor<TIn>.BuildPath(expression);

			if (relativeSource is not null && relativeSource.Mode is not RelativeSourceMode.Self)
			{
				throw new NotImplementedException();
			}

			path.BuildExpression(nodes, out _);

			if (!source.HasValue && relativeSource is null)
			{
				nodes.Insert(0, new DataContextNode());
			}

			var bindingExpression = new BindingExpression(
				source.HasValue ? source.Value : target,
				nodes,
				fallback,
				converter: converter,
				converterParameter: converterParameter,
				enableDataValidation: enableDataValidation,
				mode: mode,
				targetNullValue: targetNullValue,
				targetTypeConverter: TargetTypeConverter.GetReflectionConverter(),
				stringFormat: stringFormat,
				updateSourceTrigger: updateSourceTrigger);
			target.GetValueStore().AddBinding(targetProperty, bindingExpression);
			return (target, bindingExpression);
		}

		#endregion
	}

	public partial class Reflection : BindingExpressionTests
	{
		#region Methods

		private protected override (TargetClass, BindingExpression) CreateTargetCore<TIn, TOut>(
			Expression<Func<TIn, TOut>> expression,
			PresentationProperty targetProperty,
			IValueConverter converter,
			object converterParameter,
			object dataContext,
			bool enableDataValidation,
			Optional<object> fallbackValue,
			BindingMode mode,
			RelativeSource relativeSource,
			Optional<TIn> source,
			object targetNullValue,
			string stringFormat,
			UpdateSourceTrigger updateSourceTrigger)
		{
			var target = new TargetClass { DataContext = dataContext };
			var (path, resolver) = BindingPathFromExpressionBuilder.Build(expression);
			var fallback = fallbackValue.HasValue ? fallbackValue.Value : PresentationProperty.UnsetValue;
			List<ExpressionNode> nodes = null;

			if (relativeSource is not null && relativeSource.Mode is not RelativeSourceMode.Self)
			{
				throw new NotImplementedException();
			}

			if (!string.IsNullOrEmpty(path))
			{
				var reader = new CharacterReader(path.AsSpan());
				var (astNodes, sourceMode) = BindingExpressionGrammar.Parse(ref reader);
				nodes = ExpressionNodeFactory.CreateFromAst(astNodes, resolver, null, out _);
			}

			if (!source.HasValue && relativeSource is null)
			{
				nodes ??= new();
				nodes.Insert(0, new DataContextNode());
			}

			var bindingExpression = new BindingExpression(
				source.HasValue ? source.Value : target,
				nodes,
				fallback,
				converter: converter,
				converterParameter: converterParameter,
				enableDataValidation: enableDataValidation,
				mode: mode,
				targetNullValue: targetNullValue,
				targetTypeConverter: TargetTypeConverter.GetReflectionConverter(),
				stringFormat: stringFormat,
				updateSourceTrigger: updateSourceTrigger);

			target.GetValueStore().AddBinding(targetProperty, bindingExpression);
			return (target, bindingExpression);
		}

		#endregion
	}

	protected class AttachedProperties
	{
		#region Fields

		public static readonly AttachedProperty<string> AttachedStringProperty =
			PresentationProperty.RegisterAttached<AttachedProperties, PresentationObject, string>("AttachedString");

		#endregion
	}

	protected class PodViewModel
	{
		#region Properties

		public string StringValue { get; set; }

		#endregion
	}

	protected class PrefixConverter : IValueConverter
	{
		#region Constructors

		public PrefixConverter(string prefix = null)
		{
			Prefix = prefix;
		}

		#endregion

		#region Properties

		public string Prefix { get; set; }

		#endregion

		#region Methods

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (targetType != typeof(string))
			{
				return value;
			}

			var result = value?.ToString() ?? string.Empty;
			var prefix = parameter?.ToString() ?? Prefix;

			if (prefix is not null)
			{
				result = prefix + result;
			}
			return result;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if ((targetType != typeof(string)) || parameter?.ToString() is not string prefix)
			{
				return value;
			}

			var s = value?.ToString() ?? string.Empty;

			if (s.StartsWith(prefix))
			{
				return s.Substring(prefix.Length);
			}
			return value;
		}

		#endregion
	}

	protected class SourceControl : Control
	{
		#region Fields

		public static readonly StyledProperty<SourceControl> NextProperty =
			PresentationProperty.Register<SourceControl, SourceControl>("Next");

		public static readonly StyledProperty<string> StringValueProperty =
			PresentationProperty.Register<SourceControl, string>("StringValue");

		#endregion

		#region Properties

		public string ClrProperty { get; set; }

		public SourceControl Next
		{
			get => GetValue(NextProperty);
			set => SetValue(NextProperty, value);
		}

		public string StringValue
		{
			get => GetValue(StringValueProperty);
			set => SetValue(StringValueProperty, value);
		}

		#endregion
	}

	protected class TargetClass : Control
	{
		#region Fields

		public static readonly StyledProperty<bool> BoolProperty =
			PresentationProperty.Register<TargetClass, bool>("Bool");

		public static readonly StyledProperty<double> DoubleProperty =
			PresentationProperty.Register<TargetClass, double>("Double");

		public static readonly StyledProperty<int> IntProperty =
			PresentationProperty.Register<TargetClass, int>("Int");

		public static readonly StyledProperty<object> ObjectProperty =
			PresentationProperty.Register<TargetClass, object>("Object");

		public static readonly DirectProperty<TargetClass, string> ReadOnlyStringProperty =
			PresentationProperty.RegisterDirect<TargetClass, string>(
				nameof(ReadOnlyString),
				o => o.ReadOnlyString);

		public static readonly StyledProperty<string> StringProperty =
			PresentationProperty.Register<TargetClass, string>("String");

		private string _readOnlyString = "readonly";

		#endregion

		#region Constructors

		static TargetClass()
		{
			FocusableProperty.OverrideDefaultValue<TargetClass>(true);
		}

		#endregion

		#region Properties

		public Dictionary<PresentationProperty, BindingNotification> BindingNotifications { get; } = new();

		public bool Bool
		{
			get => GetValue(BoolProperty);
			set => SetValue(BoolProperty, value);
		}

		public double Double
		{
			get => GetValue(DoubleProperty);
			set => SetValue(DoubleProperty, value);
		}

		public int Int
		{
			get => GetValue(IntProperty);
			set => SetValue(IntProperty, value);
		}

		public object Object
		{
			get => GetValue(ObjectProperty);
			set => SetValue(ObjectProperty, value);
		}

		public string ReadOnlyString
		{
			get => _readOnlyString;
			private set => SetAndRaise(ReadOnlyStringProperty, ref _readOnlyString, value);
		}

		public string String
		{
			get => GetValue(StringProperty);
			set => SetValue(StringProperty, value);
		}

		#endregion

		#region Methods

		public void SetReadOnlyString(string value)
		{
			ReadOnlyString = value;
		}

		public override string ToString()
		{
			return nameof(TargetClass);
		}

		protected override void UpdateDataValidation(PresentationProperty property, BindingValueType state, Exception error)
		{
			base.UpdateDataValidation(property, state, error);

			var type = state switch
			{
				BindingValueType b when b.HasFlag(BindingValueType.BindingError) => BindingErrorType.Error,
				BindingValueType b when b.HasFlag(BindingValueType.DataValidationError) => BindingErrorType.DataValidationError,
				_ => BindingErrorType.None
			};

			if ((type == BindingErrorType.None) || error is null)
			{
				BindingNotifications.Remove(property);
			}
			else
			{
				BindingNotifications[property] = new BindingNotification(error, type);
			}
		}

		#endregion
	}

	protected class ViewModel : NotifyingBase
	{
		#region Fields

		private bool _boolValue;
		private double _doubleValue;
		private int _intValue;
		private ViewModel _next;
		private IObservable<ViewModel> _nextObservable;
		private Task<ViewModel> _nextTask;
		private object _objectValue;
		private string _stringValue;

		#endregion

		#region Properties

		public bool BoolValue
		{
			get => _boolValue;
			set
			{
				_boolValue = value;
				RaisePropertyChanged();
			}
		}

		public double DoubleValue
		{
			get => _doubleValue;
			set
			{
				_doubleValue = value;
				RaisePropertyChanged();
			}
		}

		public int IntValue
		{
			get => _intValue;
			set
			{
				_intValue = value;
				RaisePropertyChanged();
			}
		}

		public ViewModel Next
		{
			get => _next;
			set
			{
				_next = value;
				RaisePropertyChanged();
			}
		}

		public IObservable<ViewModel> NextObservable
		{
			get => _nextObservable;
			set
			{
				_nextObservable = value;
				RaisePropertyChanged();
			}
		}

		public Task<ViewModel> NextTask
		{
			get => _nextTask!;
			set
			{
				_nextTask = value;
				RaisePropertyChanged();
			}
		}

		public object ObjectValue
		{
			get => _objectValue;
			set
			{
				_objectValue = value;
				RaisePropertyChanged();
			}
		}

		public string StringValue
		{
			get => _stringValue;
			set
			{
				_stringValue = value;
				RaisePropertyChanged();
			}
		}

		#endregion

		#region Methods

		public void SetStringValueWithoutRaising(string value)
		{
			_stringValue = value;
		}

		#endregion
	}

	#endregion
}