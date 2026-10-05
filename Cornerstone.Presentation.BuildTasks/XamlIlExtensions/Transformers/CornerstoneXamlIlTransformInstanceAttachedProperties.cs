using System;
using System.Collections.Generic;
using System.Linq;
using XamlX;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Cornerstone.Presentation.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class CornerstoneXamlIlTransformInstanceAttachedProperties : IXamlAstTransformer
    {

        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlAstNamePropertyReference prop 
                && prop.TargetType is XamlAstClrTypeReference targetRef 
                && prop.DeclaringType is XamlAstClrTypeReference declaringRef)
            {
                // Target and declared type aren't assignable but both inherit from PresentationObject
                var cornerstoneObject = context.GetPresentationTypes().PresentationObject;
                if (cornerstoneObject.IsAssignableFrom(targetRef.Type)
                    && cornerstoneObject.IsAssignableFrom(declaringRef.Type)
                    && !declaringRef.Type.IsAssignableFrom(targetRef.Type))
                {
                    // Instance property
                    var clrProp = declaringRef.Type.GetAllProperties().FirstOrDefault(p => p.Name == prop.Name);
                    if (clrProp != null
                        && (clrProp.Getter?.IsStatic == false || clrProp.Setter?.IsStatic == false))
                    {
                        var declaringType = clrProp.DeclaringType;
                        var cornerstonePropertyFieldName = prop.Name + "Property";
                        var cornerstonePropertyField = declaringType.Fields.FirstOrDefault(f => f.IsStatic && f.Name == cornerstonePropertyFieldName);
                        if (cornerstonePropertyField != null)
                        {
                            var cornerstonePropertyType = cornerstonePropertyField.FieldType;
                            while (cornerstonePropertyType != null
                                   && !(cornerstonePropertyType.Namespace == "Cornerstone.Presentation"
                                        && (cornerstonePropertyType.Name == "PresentationProperty"
                                            || cornerstonePropertyType.Name == "PresentationProperty`1"
                                        )))
                            {
                                // Attached properties are handled by vanilla XamlIl
                                if (cornerstonePropertyType.Name.StartsWith("AttachedProperty"))
                                    return node;
                                
                                cornerstonePropertyType = cornerstonePropertyType.BaseType;
                            }

                            if (cornerstonePropertyType == null)
                                return node;

                            if (cornerstonePropertyType.GenericArguments.Count > 1)
                                return node;

                            var propertyType = cornerstonePropertyType.GenericArguments.Count == 1 ?
                                cornerstonePropertyType.GenericArguments[0] :
                                context.Configuration.WellKnownTypes.Object;

                            return new CornerstoneAttachedInstanceProperty(prop, context.Configuration,
                                    declaringType, propertyType, cornerstonePropertyType, cornerstoneObject,
                                    cornerstonePropertyField);
                        }

                    }


                }
            }

            return node;
        }

        class CornerstoneAttachedInstanceProperty : XamlAstClrProperty, IXamlIlPresentationProperty
        {
            private readonly TransformerConfiguration _config;
            private readonly IXamlType _declaringType;
            private readonly IXamlType _cornerstonePropertyType;
            private readonly IXamlType _cornerstoneObject;
            private readonly IXamlField _field;

            public CornerstoneAttachedInstanceProperty(XamlAstNamePropertyReference prop,
                TransformerConfiguration config,
                IXamlType declaringType,
                IXamlType type,
                IXamlType cornerstonePropertyType,
                IXamlType cornerstoneObject,
                IXamlField field) : base(prop, prop.Name,
                declaringType, null)
            
            
            {
                _config = config;
                _declaringType = declaringType;
                _cornerstonePropertyType = cornerstonePropertyType;
                
                // XamlIl doesn't support generic methods yet
                if (_cornerstonePropertyType.GenericArguments.Count > 0)
                    _cornerstonePropertyType = _cornerstonePropertyType.BaseType!;
                
                _cornerstoneObject = cornerstoneObject;
                _field = field;
                PropertyType = type;
                Setters.Add(new SetterMethod(this));
                Getter = new GetterMethod(this);
            }

            public IXamlType PropertyType { get;  }

            public IXamlField PresentationProperty => _field;
            
            class SetterMethod : IXamlPropertySetter, IXamlEmitablePropertySetter<IXamlILEmitter>
            {
                private readonly CornerstoneAttachedInstanceProperty _parent;

                public SetterMethod(CornerstoneAttachedInstanceProperty parent)
                {
                    _parent = parent;
                    Parameters = new[] {_parent._cornerstoneObject, _parent.PropertyType};
                }

                public IXamlType TargetType => _parent.DeclaringType;
                public PropertySetterBinderParameters BinderParameters { get; } = new PropertySetterBinderParameters();
                public IReadOnlyList<IXamlType> Parameters { get; }
                public IReadOnlyList<IXamlCustomAttribute> CustomAttributes => _parent.CustomAttributes;
                public void Emit(IXamlILEmitter emitter)
                {
                    var so = _parent._config.WellKnownTypes.Object;
                    var method = _parent._cornerstoneObject
                        .FindMethod(m => m.IsPublic && !m.IsStatic && m.Name == "SetValue"
                                         &&
                                         m.Parameters.Count == 3
                                         && m.Parameters[0].Equals(_parent._cornerstonePropertyType)
                                         && m.Parameters[1].Equals(so)
                                         && m.Parameters[2].IsEnum
                        );
                    if (method == null)
                        throw new XamlTypeSystemException(
                            "Unable to find SetValue(PresentationProperty, object, BindingPriority) on PresentationObject");
                    using (var loc = emitter.LocalsPool.GetLocal(_parent.PropertyType))
                        emitter
                            .Stloc(loc.Local)
                            .Ldsfld(_parent._field)
                            .Ldloc(loc.Local);

                    if(_parent.PropertyType.IsValueType)
                        emitter.Box(_parent.PropertyType);
                    emitter        
                        .Ldc_I4(0)
                        .EmitCall(method);

                }
            }

            class GetterMethod :  IXamlCustomEmitMethod<IXamlILEmitter>
            {
                public GetterMethod(CornerstoneAttachedInstanceProperty parent) 
                {
                    Parent = parent;
                    DeclaringType = parent._declaringType;
                    Name = "PresentationObject:GetValue_" + Parent.Name;
                    Parameters = new[] {parent._cornerstoneObject};
                }
                public CornerstoneAttachedInstanceProperty Parent { get; }
                public bool IsPublic => true;
                public bool IsPrivate => false;
                public bool IsFamily => false;
                public bool IsStatic => true;
                public bool ContainsGenericParameters => false;
                public bool IsGenericMethod => false;
                public bool IsGenericMethodDefinition => false;
                public string Name { get; protected set; }
                public IXamlType DeclaringType { get; }
                public IXamlMethod MakeGenericMethod(IReadOnlyList<IXamlType> typeArguments) 
                    => throw new System.NotSupportedException();

                public bool Equals(IXamlMethod? other) =>
                    other is GetterMethod m && m.Name == Name && m.DeclaringType.Equals(DeclaringType);

                public IXamlType ReturnType => Parent.PropertyType;
                public IReadOnlyList<IXamlType> Parameters { get; }

                public IReadOnlyList<IXamlCustomAttribute> CustomAttributes => DeclaringType.CustomAttributes;
                public IXamlParameterInfo GetParameterInfo(int index) => new AnonymousParameterInfo(Parameters[index], index);
                public IReadOnlyList<IXamlType> GenericParameters => [];
                public IReadOnlyList<IXamlType> GenericArguments => [];

                public void EmitCall(IXamlILEmitter emitter)
                {
                    var method = Parent._cornerstoneObject
                        .FindMethod(m => m.IsPublic && !m.IsStatic && m.Name == "GetValue"
                                         &&
                                         m.Parameters.Count == 1
                                         && m.Parameters[0].Equals(Parent._cornerstonePropertyType));
                    if (method == null)
                        throw new XamlTypeSystemException(
                            "Unable to find T GetValue<T>(PresentationProperty<T>) on PresentationObject");
                    emitter
                        .Ldsfld(Parent._field)
                        .EmitCall(method);
                    if (Parent.PropertyType.IsValueType)
                        emitter.Unbox_Any(Parent.PropertyType);

                }
            }
        }
    }
}
