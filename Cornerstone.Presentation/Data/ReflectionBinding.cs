using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Data.Converters;
using Cornerstone.Presentation.Data.Core;
using Cornerstone.Presentation.Data.Core.ExpressionNodes;
using Cornerstone.Presentation.Data.Core.Parsers;
using Cornerstone.Presentation.Metadata;
using Cornerstone.Presentation.Utilities;
using Cornerstone.Presentation.Controls.Naming;

namespace Cornerstone.Presentation.Data
{
    /// <summary>
    /// A binding that uses reflection to access members.
    /// </summary>
    [RequiresUnreferencedCode(TrimmingMessages.ReflectionBindingRequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(TrimmingMessages.ReflectionBindingRequiresDynamicCodeMessage)]
    public class ReflectionBinding : BindingBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReflectionBinding"/> class.
        /// </summary>
        public ReflectionBinding()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReflectionBinding"/> class.
        /// </summary>
        /// <param name="path">The binding path.</param>
        public ReflectionBinding(string path)
        {
            Path = path;
        }
        
        /// <summary>
        /// Gets or sets the amount of time, in milliseconds, to wait before updating the binding 
        /// source after the value on the target changes.
        /// </summary>
        /// <remarks>
        /// There is no delay when the source is updated via <see cref="UpdateSourceTrigger.LostFocus"/> 
        /// or <see cref="BindingExpressionBase.UpdateSource"/>. Nor is there a delay when 
        /// <see cref="BindingMode.OneWayToSource"/> is active and a new source object is provided.
        /// </remarks>
        public int Delay { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IValueConverter"/> to use.
        /// </summary>
        public IValueConverter? Converter { get; set; }

        /// <summary>
        /// Gets or sets the culture in which to evaluate the converter.
        /// </summary>
        /// <value>The default value is null.</value>
        /// <remarks>
        /// If this property is not set then <see cref="CultureInfo.CurrentCulture"/> will be used.
        /// </remarks>
        [TypeConverter(typeof(CultureInfoIetfLanguageTagConverter))]
        public CultureInfo? ConverterCulture { get; set; }

        /// <summary>
        /// Gets or sets a parameter to pass to <see cref="Converter"/>.
        /// </summary>
        public object? ConverterParameter { get; set; }

        /// <summary>
        /// Gets or sets the name of the element to use as the binding source.
        /// </summary>
        public string? ElementName { get; set; }

        /// <summary>
        /// Gets or sets the value to use when the binding is unable to produce a value.
        /// </summary>
        public object? FallbackValue { get; set; } = PresentationProperty.UnsetValue;

        /// <summary>
        /// Gets or sets the binding mode.
        /// </summary>
        public BindingMode Mode { get; set; }

        /// <summary>
        /// Gets or sets the binding path.
        /// </summary>
        [ConstructorArgument("path")]
        public string Path { get; set; } = "";

        /// <summary>
        /// Gets or sets the binding priority.
        /// </summary>
        public BindingPriority Priority { get; set; }

        /// <summary>
        /// Gets or sets the relative source for the binding.
        /// </summary>
        public RelativeSource? RelativeSource { get; set; }

        /// <summary>
        /// Gets or sets the source for the binding.
        /// </summary>
        public object? Source { get; set; } = PresentationProperty.UnsetValue;

        /// <summary>
        /// Gets or sets the string format.
        /// </summary>
        public string? StringFormat { get; set; }

        /// <summary>
        /// Gets or sets the value to use when the binding result is null.
        /// </summary>
        public object? TargetNullValue { get; set; } = PresentationProperty.UnsetValue;

        /// <summary>
        /// Gets or sets a value that determines the timing of binding source updates for
        /// <see cref="BindingMode.TwoWay"/> and <see cref="BindingMode.OneWayToSource"/> bindings.
        /// </summary>
        public UpdateSourceTrigger UpdateSourceTrigger { get; set; }

        /// <summary>
        /// Gets or sets a function used to resolve types from names in the binding path.
        /// </summary>
        public Func<string?, string, Type>? TypeResolver { get; set; }

        internal WeakReference? DefaultAnchor { get; set; }
        internal WeakReference<INameScope?>? NameScope { get; set; }

        internal override BindingExpressionBase CreateInstance(
            PresentationObject target,
            PresentationProperty? targetProperty,
            object? anchor)
        {
            List<ExpressionNode>? nodes = null;
            var isRooted = false;
            var enableDataValidation = targetProperty?.GetMetadata(target).EnableDataValidation ?? false;

            // Build the expression nodes from the binding path.
            if (!string.IsNullOrEmpty(Path))
            {
                var reader = new CharacterReader(Path.AsSpan());
                var (astPool, sourceMode) = BindingExpressionGrammar.ParseToPooledList(ref reader);
                nodes = ExpressionNodeFactory.CreateFromAst(
                    astPool,
                    TypeResolver,
                    GetNameScope(),
                    out isRooted);
            }

            // If the binding isn't rooted (i.e. doesn't have a Source or start with $parent, $self,
            // #elementName etc.) then we need to add a source node. The type of source node will
            // depend on the ElementName and RelativeSource properties of the binding and if
            // neither of those are set will default to a data context node.
            if (Source == PresentationProperty.UnsetValue && !isRooted && CreateSourceNode(targetProperty) is { } sourceNode)
            {
                nodes ??= new();
                nodes.Insert(0, sourceNode);
            }

            // If the first node is an ISourceNode then allow it to select the source; otherwise
            // use the binding source if specified, falling back to the target.
            var source = nodes?.Count > 0 && nodes[0] is SourceNode sn ?
                sn.SelectSource(Source, target, anchor ?? DefaultAnchor?.Target) :
                Source != PresentationProperty.UnsetValue ? Source : target;

            var (mode, trigger) = ResolveDefaultsFromMetadata(target, targetProperty);

            return new BindingExpression(
                source,
                nodes,
                FallbackValue,
                delay: TimeSpan.FromMilliseconds(Delay),
                converter: Converter,
                converterCulture: ConverterCulture,
                converterParameter: ConverterParameter,
                enableDataValidation: enableDataValidation,
                mode: mode,
                priority: Priority,
                stringFormat: StringFormat,
                targetProperty: targetProperty,
                targetNullValue: TargetNullValue,
                targetTypeConverter: TargetTypeConverter.GetReflectionConverter(),
                updateSourceTrigger: trigger);
        }

        private INameScope? GetNameScope()
        {
            INameScope? result = null;
            NameScope?.TryGetTarget(out result);
            return result;
        }

        private ExpressionNode? CreateSourceNode(PresentationProperty? targetProperty)
        {
            if (!string.IsNullOrEmpty(ElementName))
            {
                var nameScope = GetNameScope() ?? throw new InvalidOperationException(
                    "Cannot create ElementName binding when NameScope is null");
                return new NamedElementNode(nameScope, ElementName);
            }

            if (RelativeSource is not null)
                return ExpressionNodeFactory.CreateRelativeSource(RelativeSource);

            return ExpressionNodeFactory.CreateDataContext(targetProperty);
        }

        private (BindingMode, UpdateSourceTrigger) ResolveDefaultsFromMetadata(
            PresentationObject target,
            PresentationProperty? targetProperty)
        {
            var mode = Mode;
            var trigger = UpdateSourceTrigger == UpdateSourceTrigger.Default ?
                UpdateSourceTrigger.PropertyChanged : UpdateSourceTrigger;

            if (mode == BindingMode.Default)
            {
                if (targetProperty?.GetMetadata(target) is { } metadata)
                    mode = metadata.DefaultBindingMode;
                else
                    mode = BindingMode.OneWay;
            }

            return (mode, trigger);
        }
    }
}
