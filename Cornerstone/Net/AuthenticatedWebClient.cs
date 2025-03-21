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

	public AuthenticatedWebClient(
		PlatformCredentialVault platformCredentialVault,
		IRuntimeInformation runtimeInformation, Uri baseUri,
		TimeSpan timeout, Credential credential = null,
		IWebProxy proxy = null, IDispatcher dispatcher = null)
		: base(baseUri, timeout, credential, proxy, dispatcher)
	{
		PlatformCredentialVault = platformCredentialVault;
		RuntimeInformation = runtimeInformation;
		Headers.AddOrUpdateSyncClientDetails(runtimeInformation);

		LogInCommand = new RelayCommand(_ => LogIn());
		LogOutCommand = new RelayCommand(_ => LogOut());
	}

	#endregion

	#region Properties

	public bool IsAuthenticated { get; private set; }

	public ICommand LogInCommand { get; }

	public ICommand LogOutCommand { get; }

	public PlatformCredentialVault PlatformCredentialVault { get; }

	public IRuntimeInformation RuntimeInformation { get; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public override WebClient DeepClone(int? maxDepth = null, IncludeExcludeSettings settings = null)
	{
		var response = new AuthenticatedWebClient(PlatformCredentialVault, RuntimeInformation, BaseUri, Timeout, null, Proxy, GetDispatcher());
		return response;
	}

	/// <inheritdoc />
	public override void Initialize()
	{
		if (LoadCredential(out var credential))
		{
			Credential = credential;
			LogIn();
		}
		base.Initialize();
	}

	public void Uninitialize()
	{
		Dispose();
	}

	protected virtual bool LoadCredential(out Credential credential)
	{
		if (PlatformCredentialVault.LoadCredential())
		{
			credential = PlatformCredentialVault.Credential;
			return true;
		}

		credential = null;
		return false;
	}

	protected virtual void LogIn()
	{
		if (!ValidateCredential())
		{
			LogOut();
			return;
		}
		IsAuthenticated = true;
		OnLoggedIn();
	}

	protected virtual void LogOut()
	{
		Credential = null;
		IsAuthenticated = false;
		RemoveCredential();
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

	protected virtual bool RemoveCredential()
	{
		PlatformCredentialVault.RemoveCredential();
		return false;
	}

	protected virtual bool ValidateCredential()
	{
		return Credential != null;
	}

	#endregion

	#region Events

	public event EventHandler LoggedIn;
	public event EventHandler LoggedOut;

	#endregion
}