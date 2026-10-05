#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Data;

internal class DynamicReflectableType : IReflectableType, INotifyPropertyChanged, IEnumerable<KeyValuePair<string, object>>
{
	#region Fields

	private readonly Dictionary<string, object> _dic = new();

	#endregion

	#region Properties

	public object this[string key]
	{
		get => _dic[key];
		set
		{
			_dic[key] = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(key));
		}
	}

	#endregion

	#region Methods

	public void Add(string key, object value)
	{
		_dic.Add(key, value);

		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(key));
	}

	public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
	{
		return _dic.GetEnumerator();
	}

	public TypeInfo GetTypeInfo()
	{
		return new FakeTypeInfo(_dic);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return ((IEnumerable) _dic).GetEnumerator();
	}

	#endregion

	#region Events

	public event PropertyChangedEventHandler PropertyChanged;

	#endregion

	#region Classes

	private sealed class DynamicPropertyInfo : PropertyInfo
	{
		#region Fields

		private readonly Dictionary<string, object> _values;

		#endregion

		#region Constructors

		public DynamicPropertyInfo(string name, Dictionary<string, object> values)
		{
			Name = name;
			_values = values;
		}

		#endregion

		#region Properties

		public override PropertyAttributes Attributes => PropertyAttributes.None;

		public override bool CanRead => true;

		public override bool CanWrite => true;

		public override Type DeclaringType => typeof(DynamicReflectableType);

		public override string Name { get; }

		public override Type PropertyType => typeof(object);

		public override Type ReflectedType => typeof(DynamicReflectableType);

		#endregion

		#region Methods

		public override MethodInfo[] GetAccessors(bool nonPublic)
		{
			return Array.Empty<MethodInfo>();
		}

		public override object[] GetCustomAttributes(bool inherit)
		{
			return Array.Empty<object>();
		}

		public override object[] GetCustomAttributes(Type attributeType, bool inherit)
		{
			return Array.Empty<object>();
		}

		public override MethodInfo GetGetMethod(bool nonPublic)
		{
			return null;
		}

		public override ParameterInfo[] GetIndexParameters()
		{
			return Array.Empty<ParameterInfo>();
		}

		public override MethodInfo GetSetMethod(bool nonPublic)
		{
			return null;
		}

		public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
		{
			return _values.GetValueOrDefault(Name);
		}

		public override bool IsDefined(Type attributeType, bool inherit)
		{
			return false;
		}

		public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
		{
			_values[Name] = value;
		}

		#endregion
	}

	private class FakeTypeInfo : TypeInfo
	{
		#region Fields

		private readonly Dictionary<string, object> _dic;

		#endregion

		#region Constructors

		public FakeTypeInfo(Dictionary<string, object> values)
		{
			_dic = values;
		}

		#endregion

		#region Properties

		public override Assembly Assembly => throw new NotSupportedException();
		public override string AssemblyQualifiedName => null;
		public override Type BaseType => null;
		public override string FullName => null;
		public override Guid GUID => Guid.Empty;

		public override Module Module => throw new NotSupportedException();
		public override string Name => "";
		public override string Namespace => null;

		public override Type UnderlyingSystemType => throw new NotSupportedException();

		#endregion

		#region Methods

		public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
		{
			throw new NotSupportedException();
		}

		public override object[] GetCustomAttributes(bool inherit)
		{
			throw new NotSupportedException();
		}

		public override object[] GetCustomAttributes(Type attributeType, bool inherit)
		{
			throw new NotSupportedException();
		}

		public override Type GetElementType()
		{
			throw new NotSupportedException();
		}

		public override EventInfo GetEvent(string name, BindingFlags bindingAttr)
		{
			throw new NotSupportedException();
		}

		public override EventInfo[] GetEvents(BindingFlags bindingAttr)
		{
			throw new NotSupportedException();
		}

		public override FieldInfo GetField(string name, BindingFlags bindingAttr)
		{
			throw new NotSupportedException();
		}

		public override FieldInfo[] GetFields(BindingFlags bindingAttr)
		{
			throw new NotSupportedException();
		}

		public override Type GetInterface(string name, bool ignoreCase)
		{
			throw new NotSupportedException();
		}

		public override Type[] GetInterfaces()
		{
			throw new NotSupportedException();
		}

		public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
		{
			throw new NotSupportedException();
		}

		public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
		{
			throw new NotSupportedException();
		}

		public override Type GetNestedType(string name, BindingFlags bindingAttr)
		{
			throw new NotSupportedException();
		}

		public override Type[] GetNestedTypes(BindingFlags bindingAttr)
		{
			throw new NotSupportedException();
		}

		public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
		{
			throw new NotSupportedException();
		}

		public override object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args,
			ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters)
		{
			throw new NotSupportedException();
		}

		public override bool IsDefined(Type attributeType, bool inherit)
		{
			throw new NotSupportedException();
		}

		protected override TypeAttributes GetAttributeFlagsImpl()
		{
			throw new NotSupportedException();
		}

		protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention,
			Type[] types, ParameterModifier[] modifiers)
		{
			throw new NotSupportedException();
		}

		protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention,
			Type[] types, ParameterModifier[] modifiers)
		{
			throw new NotSupportedException();
		}

		protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types,
			ParameterModifier[] modifiers)
		{
			return new DynamicPropertyInfo(name, _dic);
		}

		protected override bool HasElementTypeImpl()
		{
			throw new NotSupportedException();
		}

		protected override bool IsArrayImpl()
		{
			throw new NotSupportedException();
		}

		protected override bool IsByRefImpl()
		{
			throw new NotSupportedException();
		}

		protected override bool IsCOMObjectImpl()
		{
			throw new NotSupportedException();
		}

		protected override bool IsPointerImpl()
		{
			throw new NotSupportedException();
		}

		protected override bool IsPrimitiveImpl()
		{
			throw new NotSupportedException();
		}

		#endregion
	}

	#endregion
}