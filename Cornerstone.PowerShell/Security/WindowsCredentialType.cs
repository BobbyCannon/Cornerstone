namespace Cornerstone.PowerShell.Security;

/// <summary>
/// The type of credential
/// </summary>
public enum WindowsCredentialType
{
	/// <summary>
	/// Unknown
	/// </summary>
	Unknown = 0,

	/// <summary>
	/// Generic
	/// </summary>
	Generic = 1,

	/// <summary>
	/// Domain Password
	/// </summary>
	DomainPassword = 2,

	/// <summary>
	/// Domain Certificate
	/// </summary>
	DomainCertificate = 3,

	/// <summary>
	/// Domain Visible Password
	/// </summary>
	DomainVisiblePassword = 4,

	/// <summary>
	/// Generic Certificate
	/// </summary>
	GenericCertificate = 5,

	/// <summary>
	/// Domain Extended
	/// </summary>
	DomainExtended = 6,

	/// <summary>
	/// Maximum
	/// </summary>
	Maximum = 7,

	/// <summary>
	/// Maximum Ex
	/// </summary>
	MaximumEx = Maximum + 1000
}