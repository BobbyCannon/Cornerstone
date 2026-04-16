#region References

using System;
using System.Net.Http.Headers;
using System.Security;
using Cornerstone.Data;

#endregion

namespace Cornerstone.Web;

/// <summary>
/// Represents a credential for a web client.
/// </summary>
public partial class WebCredential : Credential
{
	#region Constants

	public const string AuthenticationScheme = "Basic";

	#endregion

	#region Constructors

	/// <summary>
	/// Creates an instance of the web credential.
	/// </summary>
	public WebCredential()
		: this(string.Empty, string.Empty)
	{
	}

	/// <summary>
	/// Creates an instance of the web credential.
	/// </summary>
	/// <param name="username"> The username of the credential. </param>
	/// <param name="password"> The password of the credential. </param>
	public WebCredential(string username, string password)
		: base(username, password, null)
	{
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets or sets a flag indicating to remember the user.
	/// </summary>
	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial bool RememberMe { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Gets the credential from an authentication header value.
	/// </summary>
	public static WebCredential FromAuthenticationHeaderValue(AuthenticationHeaderValue headerValue)
	{
		if (!string.Equals(headerValue.Scheme, AuthenticationScheme, StringComparison.OrdinalIgnoreCase))
		{
			throw new SecurityException("The authentication header is incorrect schema.");
		}

		var response = new WebCredential();
		response.Load(headerValue);
		return response;
	}

	/// <summary>
	/// Reset the web credential.
	/// </summary>
	public override void Reset()
	{
		RememberMe = false;
		base.Reset();
	}

	#endregion
}