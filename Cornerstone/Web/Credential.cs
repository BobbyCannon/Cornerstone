#region References

using System;
using System.Net.Http.Headers;
using System.Text;
using Cornerstone.Data;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Web;

/// <summary>
/// Represents a credential for a client.
/// </summary>
public partial class Credential
	: Bindable, IDisposable, ICredential,
		IUpdateable<TokenCredential>,
		IUpdateable<WebCredential>,
		IUpdateable<Credential>,
		IUpdateable<ICredential>
{
	#region Constructors

	/// <summary>
	/// Creates an instance of the credential.
	/// </summary>
	public Credential()
		: this(string.Empty, null, null)
	{
	}

	/// <summary>
	/// Creates an instance of the credential.
	/// </summary>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	public Credential(IDispatcher dispatcher)
		: this(null, null, dispatcher)
	{
	}

	/// <summary>
	/// Creates an instance of the credential.
	/// </summary>
	/// <param name="username"> The username of the credential. </param>
	/// <param name="password"> The password of the credential. </param>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	public Credential(string username, string password, IDispatcher dispatcher)
		: base(dispatcher)
	{
		UserName = username ?? string.Empty;
		Password = password ?? string.Empty;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Represents the password for the credential.
	/// </summary>
	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial string Password { get; set; }

	/// <summary>
	/// Represents the UserName for the credential.
	/// </summary>
	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial string UserName { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Gets the credential as an authentication header value.
	/// </summary>
	public virtual AuthenticationHeaderValue GetAuthenticationHeaderValue()
	{
		return new AuthenticationHeaderValue("Basic", ToBase64String());
	}

	/// <summary>
	/// Determines if the credentials have been provided.
	/// </summary>
	/// <returns> Returns true if both UserName and Password both is not null or whitespace. </returns>
	public virtual bool HasCredentials()
	{
		return !string.IsNullOrWhiteSpace(UserName)
			&& Password is { Length: > 0 };
	}

	/// <summary>
	/// Load the credential from an authentication header value.
	/// </summary>
	public virtual void Load(AuthenticationHeaderValue value)
	{
		var credentialBytes = System.Convert.FromBase64String(value.Parameter ?? string.Empty);
		var credentials = Encoding.UTF8.GetString(credentialBytes).Split([':'], 2);
		UserName = credentials[0];
		Password = credentials[1];
	}

	/// <summary>
	/// Reset the credential.
	/// </summary>
	public virtual void Reset()
	{
		UserName = string.Empty;
		Password = string.Empty;

		NotifyOfPropertyChanged(nameof(Password));
	}

	public virtual string ToBase64String()
	{
		return System.Convert.ToBase64String(Encoding.UTF8.GetBytes($"{UserName}:{Password}"));
	}

	public static Credential ToCredential(AuthenticationHeaderValue headerValue)
	{
		return headerValue.Scheme switch
		{
			//SecurityDeviceCredential.AuthenticationScheme => SecurityDeviceCredential.FromAuthenticationHeaderValue(headerValue),
			TokenCredential.AuthenticationScheme => TokenCredential.FromAuthenticationHeaderValue(headerValue),
			WebCredential.AuthenticationScheme => WebCredential.FromAuthenticationHeaderValue(headerValue),
			_ => null
		};
	}

	/// <summary>
	/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	/// </summary>
	/// <param name="disposing"> True if disposing and false if otherwise. </param>
	protected virtual void Dispose(bool disposing)
	{
		if (!disposing)
		{
			return;
		}

		Reset();
	}

	#endregion
}

/// <summary>
/// Represents a credential.
/// </summary>
public interface ICredential
{
	#region Properties

	/// <summary>
	/// Represents the password for the credential.
	/// </summary>
	string Password { get; set; }

	/// <summary>
	/// Represents the username for the credential.
	/// </summary>
	string UserName { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Gets the credential as an authentication header value.
	/// </summary>
	AuthenticationHeaderValue GetAuthenticationHeaderValue();

	/// <summary>
	/// Determines if the credentials have been provided.
	/// </summary>
	/// <returns> Returns true if both UserName and Password both is not null or whitespace. </returns>
	bool HasCredentials();

	/// <summary>
	/// Load the credential from an authentication header value.
	/// </summary>
	void Load(AuthenticationHeaderValue value);

	/// <summary>
	/// Reset the credential.
	/// </summary>
	void Reset();

	#endregion
}