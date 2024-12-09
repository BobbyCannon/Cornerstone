#region References

using System.Collections.Generic;
using Mono.Cecil;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyShared;

public class PropertyData
{
    #region Fields

    public List<string> AlreadyNotifies;
    public List<PropertyDefinition> AlsoNotifyFor;
    public FieldReference BackingFieldReference;
    public MethodReference EqualsMethod;
    public TypeNode ParentType;
    public PropertyDefinition PropertyDefinition;

    #endregion

    #region Constructors

    public PropertyData()
    {
        AlreadyNotifies = [];
        AlsoNotifyFor = [];
    }

    #endregion
}