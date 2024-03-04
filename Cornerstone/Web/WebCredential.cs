#region References

using System;
using System.Net.Http.Headers;
using System.Security;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Net;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Web;

/// <summary>
/// Represents a credential for a web client.
/// </summary>
public class WebCredential : Credential
{
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
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	public WebCredential(IDispatcher dispatcher)
		: this(string.Empty, string.Empty, dispatcher)
	{
	}

	/// <summary>
	/// Creates an instance of the web credential.
	/// </summary>
	/// <param name="username"> The username of the credential. </param>
	/// <param name="password"> The password of the credential. </param>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	public WebCredential(string username, string password, IDispatcher dispatcher = null)
		: this(username, password?.ToSecureString(), dispatcher)
	{
	}

	/// <summary>
	/// Creates an instance of the web credential.
	/// </summary>
	/// <param name="username"> The username of the credential. </param>
	/// <param name="password"> The password of the credential. </param>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	public WebCredential(string username, SecureString password, IDispatcher dispatcher = null)
		: base(username, password, dispatcher)
	{
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets or sets a flag indicating to remember the user.
	/// </summary>
	public bool RememberMe { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Gets the credential from an authentication header value.
	/// </summary>
	public static WebCredential FromAuthenticationHeaderValue(AuthenticationHeaderValue headerValue)
	{
		if (!string.Equals(headerValue.Scheme, "Basic", StringComparison.OrdinalIgnoreCase))
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

	/// <summary>
	/// Update the WebCredential with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="options"> The options for controlling the updating of the value. </param>
	public virtual bool UpdateWith(WebCredential update, UpdateableOptions options)
	{
		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		// ****** You can use GenerateUpdateWith to update this ******

		if ((options == null) || options.IsEmpty())
		{
			RememberMe = update.RememberMe;
		}
		else
		{
			this.IfThen(_ => options.ShouldProcessProperty(nameof(RememberMe)), x => x.RememberMe = update.RememberMe);
		}

		return base.UpdateWith(update, options);
	}

	/// <inheritdoc />
	public override bool UpdateWith(object update, UpdateableOptions options)
	{
		return update switch
		{
			WebCredential credential => UpdateWith(credential, options),
			_ => base.UpdateWith(update, options)
		};
	}

	#endregion
}