namespace Cornerstone.Reflection;

public enum SourceAccessibility
{
	None = 0,
	Private = 0b0001,
	Protected = 0b0010,
	Internal = 0b0100,
	ProtectedOrInternal = 0b0110,
	ProtectedAndInternal = 0b0011,
	Public = 0b1000
}