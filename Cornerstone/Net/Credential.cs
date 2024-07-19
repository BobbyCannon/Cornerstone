#region References

using System;
using System.Net.Http.Headers;
using System.Security;
using System.Text;
using Cornerstone.Attributes;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Web;

#endregion

namespace Cornerstone.Net;

/// <summary>
/// Represents a credential for a client.
/// </summary>
public class Credential : Notifiable, IDisposable, ICredential
{
	#region Constructors

	/// <summary>
	/// Creates an instance of the credential.
	/// </summary>
	public Credential()
		: this(string.Empty, (SecureString) null)
	{
	}

	/// <summary>
	/// Creates an instance of the credential.
	/// </summary>
	/// <param name="username"> The username of the credential. </param>
	/// <param name="password"> The password of the credential. </param>
	public Credential(string username, string password)
		: this(username, password?.ToSecureString())
	{
	}

	/// <summary>
	/// Creates an instance of the credential.
	/// </summary>
	/// <param name="username"> The username of the credential. </param>
	/// <param name="password"> The password of the credential. </param>
	public Credential(string username, SecureString password)
	{
		UserName = username ?? string.Empty;
		SecurePassword = password?.Copy();
	}

	#endregion

	#region Properties

	/// <summary>
	/// Represents the password for the credential.
	/// </summary>
	[ComputedProperty]
	public string Password
	{
		get => SecurePassword?.ToUnsecureString() ?? string.Empty;
		set
		{
			var p = SecurePassword;
			SecurePassword = value?.ToSecureString();
			p?.Dispose();
		}
	}

	/// <summary>
	/// Represents the secure password for the credential.
	/// </summary>
	[SerializationIgnore]
	public SecureString SecurePassword { get; set; }

	/// <summary>
	/// Represents the UserName for the credential.
	/// </summary>
	public string UserName { get; set; }

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
		return new AuthenticationHeaderValue("Basic",
			System.Convert.ToBase64String(Encoding.UTF8.GetBytes($"{UserName}:{Password}"))
		);
	}

	/// <summary>
	/// Determines if the credentials have been provided.
	/// </summary>
	/// <returns> Returns true if both UserName and Password both is not null or whitespace. </returns>
	public virtual bool HasCredentials()
	{
		return !string.IsNullOrWhiteSpace(UserName)
			&& SecurePassword is { Length: > 0 };
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
		SecurePassword?.Dispose();
		SecurePassword = null;

		// Not required as Password is just an unsecure version of SecurePassword
		//Password = string.Empty;

		NotifyOfPropertyChanged(nameof(Password));
	}

	/// <summary>
	/// Update the Credential with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="options"> The options for controlling the updating of the value. </param>
	public virtual bool UpdateWith(ICredential update, IncludeExcludeOptions options)
	{
		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		// ****** You can use GenerateUpdateWith to update this ******

		if ((options == null) || options.IsEmpty())
		{
			SecurePassword = update.SecurePassword?.Copy();
			UserName = update.UserName;
		}
		else
		{
			this.IfThen(_ => options.ShouldProcessProperty(nameof(SecurePassword)), x => x.SecurePassword = update.SecurePassword?.Copy());
			this.IfThen(_ => options.ShouldProcessProperty(nameof(UserName)), x => x.UserName = update.UserName);
		}

		return true;
	}

	/// <inheritdoc />
	public override bool UpdateWith(object update, IncludeExcludeOptions options)
	{
		return update switch
		{
			TokenCredential credential => UpdateWith(credential, options),
			WebCredential credential => UpdateWith(credential, options),
			Credential credential => UpdateWith(credential, options),
			ICredential credential => UpdateWith(credential, options),
			_ => base.UpdateWith(update, options)
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
	/// Represents the secure password for the credential.
	/// </summary>
	SecureString SecurePassword { get; set; }

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