#region References

using System;
using System.ComponentModel;
using System.Xml.Serialization;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Highlighting.Xshd;

//
// NOTE: This is just for autocomplete
// 

/// <remarks />
[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008")]
[XmlRoot(Namespace = "http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008", IsNullable = false)]
public class SyntaxDefinition
{
	#region Fields

	private SyntaxDefinitionColor[] colorField;

	private string extensionsField;

	private string nameField;

	private SyntaxDefinitionProperty propertyField;

	private SyntaxDefinitionRuleSet[] ruleSetField;

	#endregion

	#region Properties

	/// <remarks />
	[XmlElement("Color")]
	public SyntaxDefinitionColor[] Color
	{
		get => colorField;
		set => colorField = value;
	}

	/// <remarks />
	[XmlAttribute]
	public string extensions
	{
		get => extensionsField;
		set => extensionsField = value;
	}

	/// <remarks />
	[XmlAttribute]
	public string name
	{
		get => nameField;
		set => nameField = value;
	}

	/// <remarks />
	public SyntaxDefinitionProperty Property
	{
		get => propertyField;
		set => propertyField = value;
	}

	/// <remarks />
	[XmlElement("RuleSet")]
	public SyntaxDefinitionRuleSet[] RuleSet
	{
		get => ruleSetField;
		set => ruleSetField = value;
	}

	#endregion
}

/// <remarks />
[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008")]
public class SyntaxDefinitionColor
{
	#region Fields

	private string exampleTextField;

	private string foregroundField;

	private string nameField;

	#endregion

	#region Properties

	/// <remarks />
	[XmlAttribute]
	public string exampleText
	{
		get => exampleTextField;
		set => exampleTextField = value;
	}

	/// <remarks />
	[XmlAttribute]
	public string foreground
	{
		get => foregroundField;
		set => foregroundField = value;
	}

	/// <remarks />
	[XmlAttribute]
	public string name
	{
		get => nameField;
		set => nameField = value;
	}

	#endregion
}

/// <remarks />
[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008")]
public class SyntaxDefinitionProperty
{
	#region Fields

	private string nameField;

	private string valueField;

	#endregion

	#region Properties

	/// <remarks />
	[XmlAttribute]
	public string name
	{
		get => nameField;
		set => nameField = value;
	}

	/// <remarks />
	[XmlAttribute]
	public string value
	{
		get => valueField;
		set => valueField = value;
	}

	#endregion
}

/// <remarks />
[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008")]
public class SyntaxDefinitionRuleSet
{
	#region Fields

	private object[] itemsField;

	private string nameField;

	#endregion

	#region Properties

	/// <remarks />
	[XmlElement("Keywords", typeof(SyntaxDefinitionRuleSetKeywords))]
	[XmlElement("Rule", typeof(SyntaxDefinitionRuleSetRule))]
	[XmlElement("Span", typeof(SyntaxDefinitionRuleSetSpan))]
	public object[] Items
	{
		get => itemsField;
		set => itemsField = value;
	}

	/// <remarks />
	[XmlAttribute]
	public string name
	{
		get => nameField;
		set => nameField = value;
	}

	#endregion
}

/// <remarks />
[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008")]
public class SyntaxDefinitionRuleSetKeywords
{
	#region Fields

	private string colorField;

	private string foregroundField;

	private string[] wordField;

	#endregion

	#region Properties

	/// <remarks />
	[XmlAttribute]
	public string color
	{
		get => colorField;
		set => colorField = value;
	}

	/// <remarks />
	[XmlAttribute]
	public string foreground
	{
		get => foregroundField;
		set => foregroundField = value;
	}

	/// <remarks />
	[XmlElement("Word")]
	public string[] Word
	{
		get => wordField;
		set => wordField = value;
	}

	#endregion
}

/// <remarks />
[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008")]
public class SyntaxDefinitionRuleSetRule
{
	#region Fields

	private string colorField;

	private string valueField;

	#endregion

	#region Properties

	/// <remarks />
	[XmlAttribute]
	public string color
	{
		get => colorField;
		set => colorField = value;
	}

	/// <remarks />
	[XmlText]
	public string Value
	{
		get => valueField;
		set => valueField = value;
	}

	#endregion
}

/// <remarks />
[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008")]
public class SyntaxDefinitionRuleSetSpan
{
	#region Fields

	private SyntaxDefinitionRuleSetSpanBegin beginField;

	private string colorField;

	private string endField;

	private bool multilineField;

	private bool multilineFieldSpecified;

	private SyntaxDefinitionRuleSetSpanRuleSet ruleSetField;

	private string ruleSetField1;

	#endregion

	#region Properties

	/// <remarks />
	public SyntaxDefinitionRuleSetSpanBegin Begin
	{
		get => beginField;
		set => beginField = value;
	}

	/// <remarks />
	[XmlAttribute]
	public string color
	{
		get => colorField;
		set => colorField = value;
	}

	/// <remarks />
	public string End
	{
		get => endField;
		set => endField = value;
	}

	/// <remarks />
	[XmlAttribute]
	public bool multiline
	{
		get => multilineField;
		set => multilineField = value;
	}

	/// <remarks />
	[XmlIgnore]
	public bool multilineSpecified
	{
		get => multilineFieldSpecified;
		set => multilineFieldSpecified = value;
	}

	/// <remarks />
	[XmlAttribute]
	public string ruleSet
	{
		get => ruleSetField1;
		set => ruleSetField1 = value;
	}

	/// <remarks />
	public SyntaxDefinitionRuleSetSpanRuleSet RuleSet
	{
		get => ruleSetField;
		set => ruleSetField = value;
	}

	#endregion
}

/// <remarks />
[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008")]
public class SyntaxDefinitionRuleSetSpanBegin
{
	#region Fields

	private string colorField;

	private string valueField;

	#endregion

	#region Properties

	/// <remarks />
	[XmlAttribute]
	public string color
	{
		get => colorField;
		set => colorField = value;
	}

	/// <remarks />
	[XmlText]
	public string Value
	{
		get => valueField;
		set => valueField = value;
	}

	#endregion
}

/// <remarks />
[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008")]
public class SyntaxDefinitionRuleSetSpanRuleSet
{
	#region Fields

	private SyntaxDefinitionRuleSetSpanRuleSetImport[] importField;

	private string nameField;

	private SyntaxDefinitionRuleSetSpanRuleSetSpan[] spanField;

	#endregion

	#region Properties

	/// <remarks />
	[XmlElement("Import")]
	public SyntaxDefinitionRuleSetSpanRuleSetImport[] Import
	{
		get => importField;
		set => importField = value;
	}

	/// <remarks />
	[XmlAttribute]
	public string name
	{
		get => nameField;
		set => nameField = value;
	}

	/// <remarks />
	[XmlElement("Span")]
	public SyntaxDefinitionRuleSetSpanRuleSetSpan[] Span
	{
		get => spanField;
		set => spanField = value;
	}

	#endregion
}

/// <remarks />
[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008")]
public class SyntaxDefinitionRuleSetSpanRuleSetImport
{
	#region Fields

	private string ruleSetField;

	#endregion

	#region Properties

	/// <remarks />
	[XmlAttribute]
	public string ruleSet
	{
		get => ruleSetField;
		set => ruleSetField = value;
	}

	#endregion
}

/// <remarks />
[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008")]
public class SyntaxDefinitionRuleSetSpanRuleSetSpan
{
	#region Fields

	private string beginField;

	private string beginField1;

	private string colorField;

	private string endField;

	private SyntaxDefinitionRuleSetSpanRuleSetSpanRuleSet ruleSetField;

	private string ruleSetField1;

	#endregion

	#region Properties

	/// <remarks />
	[XmlAttribute]
	public string begin
	{
		get => beginField1;
		set => beginField1 = value;
	}

	/// <remarks />
	public string Begin
	{
		get => beginField;
		set => beginField = value;
	}

	/// <remarks />
	[XmlAttribute]
	public string color
	{
		get => colorField;
		set => colorField = value;
	}

	/// <remarks />
	[XmlAttribute]
	public string end
	{
		get => endField;
		set => endField = value;
	}

	/// <remarks />
	[XmlAttribute]
	public string ruleSet
	{
		get => ruleSetField1;
		set => ruleSetField1 = value;
	}

	/// <remarks />
	public SyntaxDefinitionRuleSetSpanRuleSetSpanRuleSet RuleSet
	{
		get => ruleSetField;
		set => ruleSetField = value;
	}

	#endregion
}

/// <remarks />
[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008")]
public class SyntaxDefinitionRuleSetSpanRuleSetSpanRuleSet
{
	#region Fields

	private SyntaxDefinitionRuleSetSpanRuleSetSpanRuleSetSpan spanField;

	#endregion

	#region Properties

	/// <remarks />
	public SyntaxDefinitionRuleSetSpanRuleSetSpanRuleSetSpan Span
	{
		get => spanField;
		set => spanField = value;
	}

	#endregion
}

/// <remarks />
[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008")]
public class SyntaxDefinitionRuleSetSpanRuleSetSpanRuleSetSpan
{
	#region Fields

	private string beginField;

	private string colorField;

	private string ruleSetField;

	#endregion

	#region Properties

	/// <remarks />
	public string Begin
	{
		get => beginField;
		set => beginField = value;
	}

	/// <remarks />
	[XmlAttribute]
	public string color
	{
		get => colorField;
		set => colorField = value;
	}

	/// <remarks />
	[XmlAttribute]
	public string ruleSet
	{
		get => ruleSetField;
		set => ruleSetField = value;
	}

	#endregion
}