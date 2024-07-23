#region References

using System;
using System.Security;
using System.Threading.Tasks;
using System.Windows.Input;
using Cornerstone.Collections;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Security.Vault;

/// <summary>
/// The vault to store credential securely.
/// </summary>
public abstract class SecureVault<T> : SecureVault, IViewModel
	where T : class, ISecureVaultCredential, ITrackPropertyChanges, new()
{
	#region Fields

	private readonly SpeedyList<T> _credentials;

	#endregion

	#region Constructors

	/// <summary>
	/// Creates an instance of the vault.
	/// </summary>
	protected SecureVault(IWindowsHelloService windowsHelloService, IDispatcher dispatcher)
	{
		_credentials = new SpeedyList<T>(dispatcher, new OrderBy<T>(x => x.Name)) { FilterCheck = FilterCheck };

		Credentials = new ReadOnlySpeedyList<T>(_credentials);
		WindowsHelloService = windowsHelloService;
		HasWindowsHelloServiceBeenSetup = false;

		// Commands
		ClearFilterCommand = new RelayCommand(_ => FilterText = string.Empty);
	}

	#endregion

	#region Properties

	public ICommand ClearFilterCommand { get; }

	public ReadOnlySpeedyList<T> Credentials { get; }

	public virtual bool Exists => true;

	public string FilterText { get; set; }

	public virtual bool HasWindowsHelloServiceBeenSetup { get; }

	public bool IsInitialized { get; private set; }

	public bool IsOpen => MasterPassword != null;

	public T ItemBeingEdited { get; private set; }

	public IWindowsHelloService WindowsHelloService { get; }

	protected SecureString MasterPassword { get; private set; }

	#endregion

	#region Methods

	public void AddNewCredential()
	{
		ItemBeingEdited = new T { Id = Guid.NewGuid() };
	}

	public void CancelEditCredential()
	{
		ItemBeingEdited = default;
	}

	public virtual void ChangeMasterPassword(SecureString oldMasterPassword, SecureString newMasterPassword, SecureString validateMasterPassword)
	{
		MasterPassword.Dispose();
		MasterPassword = newMasterPassword;
		OnPropertyChanged(nameof(IsOpen));
	}

	public virtual void CloseVault()
	{
		MasterPassword?.Dispose();
		MasterPassword = null;
		OnPropertyChanged(nameof(IsOpen));
	}

	public virtual void CreateVault(SecureString masterPassword)
	{
		MasterPassword = masterPassword?.Copy();
	}

	public abstract SecureString Decrypt(string value, SecureString password);

	/// <inheritdoc />
	public override void DeleteCredential(Guid id)
	{
		if (!IsOpen)
		{
			return;
		}

		_credentials.Remove(x => x.Id == id);

		SaveVault();
	}

	public abstract string Encrypt(SecureString value, SecureString password);

	public virtual void ExportVault()
	{
	}
	
	public virtual void ImportVault()
	{
	}

	/// <summary>
	/// Get the password unencrypted but in a secure string object.
	/// </summary>
	/// <param name="credential"> The credential to read the password of. </param>
	/// <returns> The password unencrypted string. </returns>
	public abstract SecureString GetPassword(T credential);

	/// <inheritdoc />
	public override bool HasChanges(IncludeExcludeOptions options)
	{
		return base.HasChanges(options)
			|| _credentials.HasChanges();
	}

	public virtual void Initialize()
	{
		Credentials.Initialize();
		IsInitialized = true;
	}

	public virtual void InitializeWindowsHello()
	{
	}

	public virtual bool OpenVault(SecureString masterPassword)
	{
		if (IsOpen)
		{
			return false;
		}

		MasterPassword = masterPassword?.Copy();
		OnPropertyChanged(nameof(IsOpen));
		return true;
	}

	public Task OpenVaultAsync(SecureString masterPassword)
	{
		return Task.Run(() => OpenVault(masterPassword));
	}

	public virtual void OpenVaultUsingWindowsHello()
	{
	}

	/// <summary>
	/// Gets the stored credential from the vault.
	/// </summary>
	/// <param name="id"> The id of the credential. </param>
	/// <param name="credential"> The credential if it was read. </param>
	/// <returns> Return true if the credential was read otherwise false. </returns>
	public bool ReadCredential(Guid id, out T credential)
	{
		return ReadCredential(x => x.Id == id, out credential);
	}

	/// <summary>
	/// Gets the stored credential from the vault.
	/// </summary>
	/// <param name="name"> The display name of the credential. </param>
	/// <param name="credential"> The credential if it was read. </param>
	/// <returns> Return true if the credential was read otherwise false. </returns>
	public bool ReadCredential(string name, out T credential)
	{
		return ReadCredential(x => x.Name == name, out credential);
	}

	/// <summary>
	/// Gets the stored credential from the vault.
	/// </summary>
	/// <param name="name"> The name of the credential. </param>
	/// <returns> Return true if the credential was read otherwise false. </returns>
	public Task<(bool result, T credential)> ReadCredentialAsync(string name)
	{
		return Task.Run(
			() => ReadCredential(name, out var credential)
				? (true, credential)
				: (false, credential)
		);
	}

	public void SaveAndCloseVault()
	{
		SaveVault();
		CloseVault();
	}

	public void SaveCredential()
	{
		WriteCredential(ItemBeingEdited);
		ItemBeingEdited = default;
		SaveVault();
	}

	public virtual void SaveVault()
	{
	}

	public void StartEditing(T credential)
	{
		ItemBeingEdited = credential.ShallowClone(EditOptions);
	}

	public virtual void Uninitialize()
	{
		Credentials.Uninitialize();
		IsInitialized = false;
	}

	/// <summary>
	/// Writes a credential to the vault.
	/// </summary>
	/// <returns> Return true if the credential was written otherwise false. </returns>
	public bool WriteCredential(T credential)
	{
		if (!IsOpen)
		{
			return false;
		}

		try
		{
			if (ReadCredential(credential.Id, out var credentialToUpdate))
			{
				credentialToUpdate.UpdateWith(credential, EditOptions);
				credentialToUpdate.ModifiedOn = TimeService.CurrentTime.UtcNow;
				UpdatePassword(credentialToUpdate, credential);
				return true;
			}

			credentialToUpdate = credential.ShallowClone(EditOptions);
			credentialToUpdate.CreatedOn = TimeService.CurrentTime.UtcNow;
			credentialToUpdate.ModifiedOn = TimeService.CurrentTime.UtcNow;
			UpdatePassword(credentialToUpdate, credential);
			_credentials.Add(credentialToUpdate);
			return true;
		}
		finally
		{
			SaveVault();
		}
	}

	/// <summary>
	/// Writes a credential to the vault.
	/// </summary>
	/// <returns> Return true if the credential was written otherwise false. </returns>
	public virtual Task<bool> WriteCredentialAsync(T credential)
	{
		return Task.Run(() => WriteCredential(credential));
	}

	protected void ClearCredentials()
	{
		_credentials.Clear();
	}

	protected void LoadCredentials(T[] credential)
	{
		_credentials.Load(credential);
	}

	/// <inheritdoc />
	protected override void OnPropertyChanged(string propertyName = null)
	{
		switch (propertyName)
		{
			case nameof(FilterText):
			{
				//Credentials.RefreshFilter();
				break;
			}
		}

		base.OnPropertyChanged(propertyName);
	}

	/// <summary>
	/// Gets the stored credential from the vault.
	/// </summary>
	/// <param name="predicate"> The predicate to read the credential. </param>
	/// <param name="credential"> The credential if it was read. </param>
	/// <returns> Return true if the credential was read otherwise false. </returns>
	protected bool ReadCredential(Func<T, bool> predicate, out T credential)
	{
		if (!IsOpen)
		{
			credential = default;
			return false;
		}

		credential = _credentials.FirstOrDefault(predicate);
		return credential != null;
	}

	/// <summary>
	/// Update the password.
	/// </summary>
	/// <param name="credential"> The credential to update. </param>
	/// <param name="update"> The credential update. </param>
	protected abstract void UpdatePassword(T credential, T update);

	/// <summary>
	/// Update the password.
	/// </summary>
	/// <param name="credential"> The credential to update. </param>
	/// <param name="password"> The new password. </param>
	protected abstract void UpdatePassword(T credential, SecureString password);

	/// <summary>
	/// The check to filter credentials view.
	/// </summary>
	private bool FilterCheck(T arg)
	{
		return string.IsNullOrWhiteSpace(FilterText)
			|| (arg.Name?.IndexOf(FilterText, StringComparison.OrdinalIgnoreCase) >= 0)
			|| (arg.UserName?.IndexOf(FilterText, StringComparison.OrdinalIgnoreCase) >= 0)
			|| (arg.Uri?.IndexOf(FilterText, StringComparison.OrdinalIgnoreCase) >= 0)
			|| (arg.Notes?.IndexOf(FilterText, StringComparison.OrdinalIgnoreCase) >= 0)
			|| (arg.Group?.IndexOf(FilterText, StringComparison.OrdinalIgnoreCase) >= 0);
	}

	#endregion
}

public abstract class SecureVault : Notifiable
{
	#region Constructors

	static SecureVault()
	{
		EditOptions = new IncludeExcludeOptions(
			[
				nameof(ISecureVaultCredential.Group),
				nameof(ISecureVaultCredential.Id),
				nameof(ISecureVaultCredential.Name),
				nameof(ISecureVaultCredential.Notes),
				nameof(ISecureVaultCredential.Uri),
				nameof(ISecureVaultCredential.UserName)
			],
			null
		);
	}

	#endregion

	#region Properties

	protected static IncludeExcludeOptions EditOptions { get; }

	#endregion

	#region Methods

	public abstract void DeleteCredential(Guid id);

	#endregion
}