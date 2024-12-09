#region References

using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanged;

public class EventArgsCache
{
	#region Fields

	private readonly TypeDefinition _cacheTypeDefinition;
	private readonly Dictionary<string, FieldDefinition> _properties = new();

	private readonly WeaverForPropertyChanged _weaverForPropertyChanged;

	#endregion

	#region Constructors

	public EventArgsCache(WeaverForPropertyChanged weaverForPropertyChanged)
	{
		_weaverForPropertyChanged = weaverForPropertyChanged;
		var attributes = TypeAttributes.AutoClass | TypeAttributes.AutoLayout | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit | TypeAttributes.Class | TypeAttributes.NotPublic;
		_cacheTypeDefinition = new(weaverForPropertyChanged.ModuleDefinition.Assembly.Name.Name, "<>PropertyChangedEventArgs", attributes, weaverForPropertyChanged.ModuleWeaver.TypeSystem.ObjectReference);
		weaverForPropertyChanged.MarkAsGeneratedCode(_cacheTypeDefinition.CustomAttributes);
	}

	#endregion

	#region Methods

	public FieldReference GetEventArgsField(string propertyName)
	{
		if (!_properties.TryGetValue(propertyName, out var field))
		{
			var attributes = FieldAttributes.Assembly | FieldAttributes.Static | FieldAttributes.InitOnly;
			field = new(propertyName, attributes, _weaverForPropertyChanged.PropertyChangedEventArgsReference);
			_properties.Add(propertyName, field);
		}

		return field;
	}

	public void InjectType()
	{
		if (_properties.Count == 0)
		{
			return;
		}

		var attributes = MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.Static;
		var cctor = new MethodDefinition(".cctor", attributes, _weaverForPropertyChanged.ModuleWeaver.TypeSystem.VoidReference);

		foreach (var pair in _properties.OrderBy(i => i.Key))
		{
			var propertyName = pair.Key;
			var eventArgsField = pair.Value;

			_cacheTypeDefinition.Fields.Add(eventArgsField);

			cctor.Body.Instructions.Append(
				Instruction.Create(OpCodes.Ldstr, propertyName),
				Instruction.Create(OpCodes.Newobj, _weaverForPropertyChanged.PropertyChangedEventConstructorReference),
				Instruction.Create(OpCodes.Stsfld, eventArgsField)
			);
		}

		cctor.Body.Instructions.Append(
			Instruction.Create(OpCodes.Ret)
		);

		_cacheTypeDefinition.Methods.Add(cctor);
		_weaverForPropertyChanged.ModuleDefinition.Types.Add(_cacheTypeDefinition);
	}

	#endregion
}