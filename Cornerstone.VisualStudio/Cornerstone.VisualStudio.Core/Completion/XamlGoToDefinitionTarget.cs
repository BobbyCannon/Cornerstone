namespace Cornerstone.VisualStudio.Core.Completion;

public enum XamlGoToDefinitionKind
{
	None,
	Type,
	Member,
	ClassName,
	MethodName,
	StyleClass
}

public sealed class XamlGoToDefinitionTarget
{
	#region Constructors

	private XamlGoToDefinitionTarget(
		XamlGoToDefinitionKind kind,
		string typeFullName,
		string memberName,
		string className,
		string methodName,
		int documentOffset)
	{
		Kind = kind;
		TypeFullName = typeFullName;
		MemberName = memberName;
		ClassName = className;
		MethodName = methodName;
		DocumentOffset = documentOffset;
	}

	#endregion

	#region Properties

	public static XamlGoToDefinitionTarget None { get; } = new(
		XamlGoToDefinitionKind.None, "", "", "", "", -1);

	public string ClassName { get; }

	public int DocumentOffset { get; }

	public XamlGoToDefinitionKind Kind { get; }

	public string MemberName { get; }

	public string MethodName { get; }

	public string TypeFullName { get; }

	#endregion

	#region Methods

	public static XamlGoToDefinitionTarget ForClass(string className)
	{
		return new XamlGoToDefinitionTarget(XamlGoToDefinitionKind.ClassName, "", "", className ?? "", "", -1);
	}

	public static XamlGoToDefinitionTarget ForMember(string typeFullName, string memberName)
	{
		return new XamlGoToDefinitionTarget(XamlGoToDefinitionKind.Member, typeFullName ?? "", memberName ?? "", "", "", -1);
	}

	public static XamlGoToDefinitionTarget ForMethod(string methodName, string className)
	{
		return new XamlGoToDefinitionTarget(XamlGoToDefinitionKind.MethodName, "", "", className ?? "", methodName ?? "", -1);
	}

	public static XamlGoToDefinitionTarget ForStyleClass(string className, int documentOffset)
	{
		return new XamlGoToDefinitionTarget(XamlGoToDefinitionKind.StyleClass, "", "", className ?? "", "", documentOffset);
	}

	public static XamlGoToDefinitionTarget ForType(string typeFullName)
	{
		return new XamlGoToDefinitionTarget(XamlGoToDefinitionKind.Type, typeFullName ?? "", "", "", "", -1);
	}

	#endregion
}
