using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;
using Cornerstone.Presentation.Utilities;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.Transform.Transformers;
using XamlX.TypeSystem;
using IXamlIlAstEmitableNode = XamlX.Emit.IXamlAstEmitableNode<XamlX.IL.IXamlILEmitter, XamlX.IL.XamlILNodeEmitResult>;
using XamlIlEmitContext = XamlX.Emit.XamlEmitContext<XamlX.IL.IXamlILEmitter, XamlX.IL.XamlILNodeEmitResult>;

namespace Cornerstone.Presentation.Markup.Xaml.XamlIl.CompilerExtensions
{
    class XamlIlPresentationPropertyHelper
    {
        public static bool EmitProvideValueTarget(XamlIlEmitContext context, IXamlILEmitter emitter,
            XamlAstClrProperty property)
        {
            if (Emit(context, emitter, property))
                return true;
            var foundClr = property.DeclaringType.Properties.FirstOrDefault(p => p.Name == property.Name);
            if (foundClr == null)
                return false;
            context
                .Configuration.GetExtra<XamlIlClrPropertyInfoEmitter>()
                .Emit(context, emitter, foundClr);
            return true;
        }
        
        public static bool Emit(XamlIlEmitContext context, IXamlILEmitter emitter, XamlAstClrProperty property)
        {
            if (property is IXamlIlPresentationProperty ap)
            {
                emitter.Ldsfld(ap.PresentationProperty);
                return true;
            }
            var type = property.DeclaringType;
            var name = property.Name + "Property";
            var found = type.Fields.FirstOrDefault(f => f.IsStatic && f.Name == name);
            if (found == null)
                return false;

            emitter.Ldsfld(found);
            return true;
        }
        
        public static bool Emit(XamlIlEmitContext context, IXamlILEmitter emitter, IXamlProperty property)
        {
            var name = property.Name + "Property";
            var found = property.DeclaringType.Fields.FirstOrDefault(f => f.IsStatic && f.Name == name);
            if (found == null)
                return false;

            emitter.Ldsfld(found);
            return true;
        }

        public static IXamlIlPresentationPropertyNode CreateNode(AstTransformationContext context,
            string propertyName, IXamlAstTypeReference selectorTypeReference, IXamlLineInfo lineInfo)
        {
            XamlAstNamePropertyReference forgedReference;

            var parsedPropertyName = PropertyParser.Parse(propertyName);
            if(parsedPropertyName.owner == null)
                forgedReference = new XamlAstNamePropertyReference(lineInfo, selectorTypeReference,
                    propertyName, selectorTypeReference);
            else if (string.IsNullOrWhiteSpace(parsedPropertyName.ns)
                && string.Equals(parsedPropertyName.owner, "Classes", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(parsedPropertyName.name)
                )
            {
                return new XamlIlPresentationClassProperty(context.GetPresentationTypes(), parsedPropertyName.name, lineInfo);
            }
            else
            {
                var xmlOwner = parsedPropertyName.ns;
                if (xmlOwner != null)
                    xmlOwner += ":";
                xmlOwner += parsedPropertyName.owner;
                
                var tref = TypeReferenceResolver.ResolveType(context, xmlOwner, false, lineInfo, true);

                var propertyFieldName = parsedPropertyName.name + "Property";
                var found = tref.Type.GetAllFields()
                    .FirstOrDefault(f => f.IsStatic && f.IsPublic && f.Name == propertyFieldName);
                if (found == null)
                    throw new XamlX.XamlTransformException(
                        $"Unable to find {propertyFieldName} field on type {tref.Type.GetFullName()}", lineInfo);
                return new XamlIlPresentationPropertyFieldNode(context.GetPresentationTypes(), lineInfo, found);
            }

            var clrProperty = (XamlAstClrProperty)new PropertyReferenceResolver().Transform(context, forgedReference);
            var cornerstonePropertyBaseType = context.GetPresentationTypes().PresentationProperty;

            // PropertyReferenceResolver.Transform failed resolving property, return empty stub from here:
            if (clrProperty.DeclaringType == XamlPseudoType.Unknown)
            {
                return new XamlIlPresentationPropertyNode(lineInfo, cornerstonePropertyBaseType, clrProperty, XamlPseudoType.Unknown);
            }

            return new XamlIlPresentationPropertyNode(lineInfo, cornerstonePropertyBaseType, clrProperty);
        }

        public static IXamlType GetPresentationPropertyType(IXamlField field,
            CornerstoneXamlIlWellKnownTypes types, IXamlLineInfo lineInfo)
        {
            var cornerstonePropertyType = field.FieldType;
            while (cornerstonePropertyType != null)
            {
                if (cornerstonePropertyType.GenericTypeDefinition?.Equals(types.PresentationPropertyT) == true)
                {
                    return cornerstonePropertyType.GenericArguments[0];
                }

                cornerstonePropertyType = cornerstonePropertyType.BaseType;
            }

            throw new XamlX.XamlTransformException(
                $"{field.Name}'s type {field.FieldType} doesn't inherit from  PresentationProperty<T>, make sure to use typed properties",
                lineInfo);

        }
    }

    interface IXamlIlPresentationPropertyNode : IXamlAstValueNode
    {
        IXamlType PresentationPropertyType { get; }
    }

    // Marker interface, used to identify whether the Cornerstone property represents Classes
    interface IXamlIlPresentationClassPropertyNode : IXamlIlPresentationPropertyNode
    {

    }

    class XamlIlPresentationPropertyNode : XamlAstNode, IXamlAstValueNode, IXamlIlAstEmitableNode, IXamlIlPresentationPropertyNode
    {
        public XamlIlPresentationPropertyNode(IXamlLineInfo lineInfo, IXamlType type, XamlAstClrProperty property, IXamlType propertyType) : base(lineInfo)
        {
            Type = new XamlAstClrTypeReference(this, type, false);
            Property = property;
            PresentationPropertyType = propertyType;
        }

        public XamlIlPresentationPropertyNode(IXamlLineInfo lineInfo, IXamlType type, XamlAstClrProperty property)
            : this(lineInfo, type, property, GetPropertyType(property))
        {
        }

        public XamlAstClrProperty Property { get; }

        public IXamlAstTypeReference Type { get; }
        public XamlILNodeEmitResult Emit(XamlIlEmitContext context, IXamlILEmitter codeGen)
        {
            if (!XamlIlPresentationPropertyHelper.Emit(context, codeGen, Property))
                throw new XamlX.XamlLoadException(Property.Name + " is not an PresentationProperty", this);
            return XamlILNodeEmitResult.Type(0, Type.GetClrType());
        }

        public IXamlType PresentationPropertyType { get; }

        private static IXamlType GetPropertyType(XamlAstClrProperty property) =>
            property.Getter?.ReturnType
            ?? property.Setters.FirstOrDefault()?.Parameters[0]
            ?? throw new InvalidOperationException(
                $"Unable to resolve \"{property.DeclaringType.Name}.{property.Name}\" property type. There is no setter or getter.");
    }

    class XamlIlPresentationPropertyFieldNode : XamlAstNode, IXamlAstValueNode, IXamlIlAstEmitableNode, IXamlIlPresentationPropertyNode
    {
        private readonly IXamlField _field;

        public XamlIlPresentationPropertyFieldNode(CornerstoneXamlIlWellKnownTypes types,
            IXamlLineInfo lineInfo, IXamlField field) : base(lineInfo)
        {
            _field = field;
            PresentationPropertyType = XamlIlPresentationPropertyHelper.GetPresentationPropertyType(field,
                types, lineInfo);
        }
        
        

        public IXamlAstTypeReference Type => new XamlAstClrTypeReference(this, _field.FieldType, false);
        public XamlILNodeEmitResult Emit(XamlIlEmitContext context, IXamlILEmitter codeGen)
        {
            codeGen.Ldsfld(_field);
            return XamlILNodeEmitResult.Type(0, _field.FieldType);
        }

        public IXamlType PresentationPropertyType { get; }
    }

    interface IXamlIlPresentationProperty
    {
        IXamlField PresentationProperty { get; }
    }
    
    class XamlIlPresentationProperty : XamlAstClrProperty, IXamlIlPresentationProperty
    {
        public IXamlField PresentationProperty { get; }
        public XamlIlPresentationProperty(XamlAstClrProperty original, IXamlField field,
            CornerstoneXamlIlWellKnownTypes types)
            :base(original, original.Name, original.DeclaringType, original.Getter, original.Setters, original.CustomAttributes)
        {
            var assignBinding = original.CustomAttributes.Any(ca => ca.Type.Equals(types.AssignBindingAttribute));

            PresentationProperty = field;
            if (!assignBinding)
                Setters.Insert(0, new BindingSetter(types, original.DeclaringType, field));

            // Styled and attached properties can be set with a BindingPriority when they're
            // assigned in a ControlTemplate.
            if (field.FieldType.GenericTypeDefinition == types.StyledPropertyT ||
                field.FieldType.GenericTypeDefinition == types.CornerstoneAttachedPropertyT)
            {
                var propertyType = field.FieldType.GenericArguments[0];
                Setters.Insert(0, new SetValueWithPrioritySetter(types, original.DeclaringType, field, propertyType));
                if (!assignBinding)
                    Setters.Insert(1, new BindingWithPrioritySetter(types, original.DeclaringType, field));
            }

            Setters.Insert(0, new UnsetValueSetter(types, original.DeclaringType, field));
            TypeConverters = original.TypeConverters;
        }

        abstract class PresentationPropertyCustomSetter : IXamlILOptimizedEmitablePropertySetter, IEquatable<PresentationPropertyCustomSetter>
        {
            protected readonly CornerstoneXamlIlWellKnownTypes Types;
            protected readonly IXamlField PresentationProperty;

            protected PresentationPropertyCustomSetter(
                CornerstoneXamlIlWellKnownTypes types,
                IXamlType declaringType,
                IXamlField cornerstoneProperty,
                bool allowNull,
                IReadOnlyList<IXamlType> parameters)
            {
                Types = types;
                PresentationProperty = cornerstoneProperty;
                TargetType = declaringType;
                Parameters = parameters;
                BinderParameters = new PropertySetterBinderParameters
                {
                    AllowXNull = allowNull,
                    AllowRuntimeNull = allowNull
                };
            }

            public IXamlType TargetType { get; }

            public PropertySetterBinderParameters BinderParameters { get; }

            public IReadOnlyList<IXamlType> Parameters { get; }
            public IReadOnlyList<IXamlCustomAttribute> CustomAttributes => Array.Empty<IXamlCustomAttribute>();

            public abstract void Emit(IXamlILEmitter emitter);

            public abstract void EmitWithArguments(
                XamlEmitContextWithLocals<IXamlILEmitter, XamlILNodeEmitResult> context,
                IXamlILEmitter emitter,
                IReadOnlyList<IXamlAstValueNode> arguments);

            public bool Equals(PresentationPropertyCustomSetter? other)
            {
                if (ReferenceEquals(null, other))
                    return false;
                if (ReferenceEquals(this, other))
                    return true;

                return GetType() == other.GetType() && PresentationProperty.Equals(other.PresentationProperty);
            }

            public override bool Equals(object? obj)
                => Equals(obj as PresentationPropertyCustomSetter);

            public override int GetHashCode() 
                => PresentationProperty.GetHashCode();
        }

        class BindingSetter : PresentationPropertyCustomSetter
        {
            public BindingSetter(
                CornerstoneXamlIlWellKnownTypes types,
                IXamlType declaringType,
                IXamlField cornerstoneProperty)
                : base(types, declaringType, cornerstoneProperty, false, [types.BindingBase])
            {
            }

            public override void Emit(IXamlILEmitter emitter)
            {
                using (var bloc = emitter.LocalsPool.GetLocal(Types.BindingBase))
                    emitter
                        .Stloc(bloc.Local)
                        .Ldsfld(PresentationProperty)
                        .Ldloc(bloc.Local)
                        .EmitCall(Types.PresentationObjectBindMethod, true);
            }

            public override void EmitWithArguments(
                XamlEmitContextWithLocals<IXamlILEmitter, XamlILNodeEmitResult> context,
                IXamlILEmitter emitter,
                IReadOnlyList<IXamlAstValueNode> arguments)
            {
                emitter.Ldsfld(PresentationProperty);
                context.Emit(arguments[0], emitter, Parameters[0]);
                emitter.EmitCall(Types.PresentationObjectBindMethod, true);
            }
        }

        class BindingWithPrioritySetter : PresentationPropertyCustomSetter
        {
            public BindingWithPrioritySetter(
                CornerstoneXamlIlWellKnownTypes types,
                IXamlType declaringType,
                IXamlField cornerstoneProperty)
                : base(types, declaringType, cornerstoneProperty, false, [types.BindingPriority, types.BindingBase])
            {
            }

            public override void Emit(IXamlILEmitter emitter)
            {
                using (var bloc = emitter.LocalsPool.GetLocal(Types.BindingBase))
                    emitter
                        .Stloc(bloc.Local)
                        .Pop() // ignore priority
                        .Ldsfld(PresentationProperty)
                        .Ldloc(bloc.Local)
                        .EmitCall(Types.PresentationObjectBindMethod, true);
            }

            public override void EmitWithArguments(
                XamlEmitContextWithLocals<IXamlILEmitter, XamlILNodeEmitResult> context,
                IXamlILEmitter emitter,
                IReadOnlyList<IXamlAstValueNode> arguments)
            {
                emitter.Ldsfld(PresentationProperty);
                context.Emit(arguments[1], emitter, Parameters[1]);
                emitter.EmitCall(Types.PresentationObjectBindMethod, true);
            }
        }

        class SetValueWithPrioritySetter : PresentationPropertyCustomSetter
        {
            public SetValueWithPrioritySetter(
                CornerstoneXamlIlWellKnownTypes types,
                IXamlType declaringType,
                IXamlField cornerstoneProperty,
                IXamlType propertyType)
                : base(types, declaringType, cornerstoneProperty, propertyType.AcceptsNull(), [types.BindingPriority, propertyType])
            {
            }

            public override void Emit(IXamlILEmitter emitter)
            {
                /*
                  Current stack:
                   - object
                   - binding priority
                   - value
                */

                using (var valueLocal = emitter.LocalsPool.GetLocal(Parameters[1]))
                using (var priorityLocal = emitter.LocalsPool.GetLocal(Types.Int))
                    emitter
                        .Stloc(valueLocal.Local)
                        .Stloc(priorityLocal.Local)
                        .Ldsfld(PresentationProperty)
                        .Ldloc(valueLocal.Local)
                        .Ldloc(priorityLocal.Local);

                EmitSetStyledPropertyValue(emitter);
            }

            public override void EmitWithArguments(
                XamlEmitContextWithLocals<IXamlILEmitter, XamlILNodeEmitResult> context,
                IXamlILEmitter emitter,
                IReadOnlyList<IXamlAstValueNode> arguments)
            {
                emitter.Ldsfld(PresentationProperty);
                context.Emit(arguments[1], emitter, Parameters[1]);
                context.Emit(arguments[0], emitter, Parameters[0]);
                EmitSetStyledPropertyValue(emitter);
            }

            private void EmitSetStyledPropertyValue(IXamlILEmitter emitter)
            {
                var method = Types.PresentationObjectSetStyledPropertyValue.MakeGenericMethod(new[] { Parameters[1] });
                emitter.EmitCall(method, true);
            }
        }

        class UnsetValueSetter : PresentationPropertyCustomSetter
        {
            public UnsetValueSetter(
                CornerstoneXamlIlWellKnownTypes types,
                IXamlType declaringType,
                IXamlField cornerstoneProperty)
                : base(types, declaringType, cornerstoneProperty, false, [types.UnsetValueType])
            {
            }

            public override void Emit(IXamlILEmitter codegen)
            {
                codegen.Pop();
                EmitSetValue(codegen);
            }

            public override void EmitWithArguments(
                XamlEmitContextWithLocals<IXamlILEmitter, XamlILNodeEmitResult> context,
                IXamlILEmitter emitter,
                IReadOnlyList<IXamlAstValueNode> arguments)
            {
                EmitSetValue(emitter);
            }

            private void EmitSetValue(IXamlILEmitter emitter)
            {
                // Ignore the instance and load one from the static field to avoid extra local variable
                var unsetValue = Types.PresentationProperty.Fields.First(f => f.Name == "UnsetValue");

                emitter
                    .Ldsfld(PresentationProperty)
                    .Ldsfld(unsetValue)
                    .Ldc_I4(0)
                    .EmitCall(Types.PresentationObjectSetValueMethod, true);
            }
        }
    }

    sealed class XamlIlPresentationClassProperty : XamlAstClrProperty,
        IXamlIlPresentationClassPropertyNode,
        IXamlAstValueNode,
        IXamlAstLocalsEmitableNode<IXamlILEmitter, XamlILNodeEmitResult>
    {
        private readonly IXamlMethod _method;
        private readonly CornerstoneXamlIlWellKnownTypes _types;
        private readonly string _className;
        private readonly IXamlAstTypeReference _type;
        private readonly IXamlType _returnType;

        public XamlIlPresentationClassProperty(CornerstoneXamlIlWellKnownTypes types,
            string className,
            IXamlLineInfo lineInfo) : base(lineInfo, className, types.Classes, null)
        {
            Parameters = [types.XamlIlTypes.String];
            _method = types.GetClassProperty;
            PresentationPropertyType = types.XamlIlTypes.Boolean;
            _types = types;
            _returnType = _types.PresentationPropertyT.MakeGenericType(types.XamlIlTypes.Boolean);
            _type = new XamlAstClrTypeReference(this, _returnType, false);
            _className = className;
            Setters = [];
        }

        public IXamlType PresentationPropertyType { get; }
        public IReadOnlyList<IXamlType> Parameters { get; }
        public IXamlAstTypeReference Type => _type;

        public PropertySetterBinderParameters BinderParameters { get; } = new PropertySetterBinderParameters();

        public XamlILNodeEmitResult Emit(XamlEmitContextWithLocals<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter emitter)
        {
            using (var loc = emitter.LocalsPool.GetLocal(_types.XamlIlTypes.String))
            {
                emitter
                    .Ldstr(_className);
                emitter.EmitCall(_method, false);
            }
            return XamlILNodeEmitResult.Type(0, _returnType);
        }
    }
}
