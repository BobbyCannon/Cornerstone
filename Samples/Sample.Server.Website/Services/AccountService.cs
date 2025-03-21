#region References

using System;
using System.Linq;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using Cornerstone.Net;
using Cornerstone.Runtime;
using Sample.Shared.Storage;
using Sample.Shared.Storage.Server;

#endregion

namespace Sample.Server.Website.Services;

public class AccountService
{
	#region Constructors

	public AccountService(IServerDatabase database, IDateTimeProvider dateTimeProvider, AccountEntity account = null)
	{
		Database = database;
		DateTimeProvider = dateTimeProvider;
		Account = account;
	}

	#endregion

	#region Properties

	public AccountEntity Account { get; }

	public IServerDatabase Database { get; }

	public IDateTimeProvider DateTimeProvider { get; }

	#endregion

	#region Methods

	public AccountEntity AuthenticateAccount(Credential model)
	{
		if (string.IsNullOrWhiteSpace(model.Password))
		{
			throw new AuthenticationException(Constants.LoginInvalidError);
		}

		var user = Database.Accounts
			.Where(x => !x.IsDeleted)
			.Select(x => new AccountEntity
			{
				Id = x.Id,
				EmailAddress = x.EmailAddress,
				Name = x.Name,
				PasswordHash = x.PasswordHash,
				Roles = x.Roles
			})
			.FirstOrDefault(x => x.EmailAddress == model.UserName);

		if (user == null)
		{
			throw new AuthenticationException(Constants.LoginInvalidError);
		}

		if (!user.PasswordHash.Equals(Hash(model.Password, user.Id.ToString()), StringComparison.OrdinalIgnoreCase))
		{
			throw new AuthenticationException(Constants.LoginInvalidError);
		}

		// Don't allow this update to bump the user modified on
		// See the SyncController comments on why, basically it causes sync issues
		var previousValue = Database.DatabaseSettings.MaintainModifiedOn;
		Database.DatabaseSettings.MaintainModifiedOn = false;
		user.LastLoginDate = DateTimeProvider.UtcNow;
		Database.SaveChanges();
		Database.DatabaseSettings.MaintainModifiedOn = previousValue;

		return user;
	}

	public static string Hash(string password, string salt)
	{
		using HashAlgorithm algorithm = SHA256.Create();
		var pBytes = Encoding.Unicode.GetBytes(password);
		var sBytes = Encoding.Unicode.GetBytes(salt);
		var tBytes = new byte[sBytes.Length + pBytes.Length];

		Buffer.BlockCopy(sBytes, 0, tBytes, 0, sBytes.Length);
		Buffer.BlockCopy(pBytes, 0, tBytes, sBytes.Length, pBytes.Length);

		var hash = algorithm.ComputeHash(tBytes);
		return Convert.ToBase64String(hash);
	}

	#endregion
}