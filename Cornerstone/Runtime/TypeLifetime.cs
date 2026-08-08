namespace Cornerstone.Runtime;

/// <summary>
/// Lifetime for types registered with <see cref="DependencyProvider" />.
/// </summary>
public enum TypeLifetime
{
	/// <summary>
	/// A new instance is created on every resolve.
	/// </summary>
	Transient = 0,

	/// <summary>
	/// Reserved for future scoped lifetime support.
	/// </summary>
	Scoped = 1,

	/// <summary>
	/// A single shared instance is created on first resolve.
	/// </summary>
	Singleton = 2
}
