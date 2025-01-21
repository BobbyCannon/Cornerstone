#region References

using System;
using System.Net;
using System.Net.Http;
using System.Windows.Input;
using Cornerstone.Data;
using Cornerstone.Presentation;
using Cornerstone.Runtime;
using Cornerstone.Security;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Net;

public class AuthenticatedWebClient : WebClient
{
	#region Constructors

	public AuthenticatedWebClient(CredentialVault credentialVault,
		IRuntimeInformation runtimeInformation, Uri baseUri,
		TimeSpan timeout, Credential credential = null,
		IWebProxy proxy = null, IDispatcher dispatcher = null)
		: base(baseUri, timeout, credential, proxy, dispatcher)
	{
		CredentialVault = credentialVault;
		RuntimeInformation = runtimeInformation;
		Headers.AddOrUpdateSyncClientDetails(runtimeInformation);

		LogInCommand = new RelayCommand(_ => LogIn());
		LogOutCommand = new RelayCommand(_ => LogOut());
	}

	#endregion

	#region Properties

	public CredentialVault CredentialVault { get; }

	public bool IsAuthenticated { get; private set; }

	public ICommand LogInCommand { get; }

	public ICommand LogOutCommand { get; }

	public IRuntimeInformation RuntimeInformation { get; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public override WebClient DeepClone(int? maxDepth = null, IncludeExcludeSettings settings = null)
	{
		return new AuthenticatedWebClient(CredentialVault, RuntimeInformation, BaseUri, Timeout, null, Proxy, GetDispatcher());
	}

	/// <inheritdoc />
	public override void Initialize()
	{
		if (CredentialVault.LoadCredential())
		{
			Credential = new TokenCredential(CredentialVault.Credential.Password);
			LogIn();
		}
		base.Initialize();
	}

	public void Uninitialize()
	{
		Dispose();
	}

	protected void LogIn()
	{
		IsAuthenticated = true;
		OnLoggedIn();
	}

	protected void LogOut()
	{
		CredentialVault.RemoveCredential();
		Credential = null;
		IsAuthenticated = false;
		OnLoggedOut();
	}

	protected virtual void OnLoggedIn()
	{
		LoggedIn?.Invoke(this, EventArgs.Empty);
	}

	protected virtual void OnLoggedOut()
	{
		LoggedOut?.Invoke(this, EventArgs.Empty);
	}

	/// <inheritdoc />
	protected override HttpResponseMessage ProcessResponse(HttpResponseMessage response)
	{
		if (response.StatusCode == HttpStatusCode.Unauthorized)
		{
			LogOut();
		}

		return base.ProcessResponse(response);
	}

	#endregion

	#region Events

	public event EventHandler LoggedIn;
	public event EventHandler LoggedOut;

	#endregion
}