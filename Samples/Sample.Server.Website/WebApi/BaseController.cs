#region References

using System;
using System.Linq;
using System.Linq.Expressions;
using Cornerstone.Convert;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Sample.Shared.Storage;
using Sample.Shared.Storage.Server;

#endregion

namespace Sample.Server.Website.WebApi;

public abstract class BaseController : ControllerBase
{
	#region Fields

	private AccountEntity _account;

	#endregion

	#region Constructors

	protected BaseController(IServerDatabase database)
	{
		Database = database;
	}

	#endregion

	#region Properties

	public IServerDatabase Database { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Gets the current authenticated user using the provided session.
	/// </summary>
	/// <returns> The entity the authenticated account. </returns>
	protected AccountEntity GetAuthenticatedAccount(Expression<Func<AccountEntity, AccountEntity>> cast, bool throwException)
	{
		if (_account != null)
		{
			return _account;
		}

		// Make sure we are authenticated.
		var user = HttpContext.User;
		if (user.Identity?.IsAuthenticated != true)
		{
			if (throwException)
			{
				throw new Exception(Constants.Unauthorized);
			}

			return null;
		}

		var userId = user.Identity.Name.ConvertTo<int>();
		_account = Database.Accounts.Select(cast).FirstOrDefault(u => u.Id == userId);

		if (_account == null)
		{
			// Log the user out because we cannot find the user account.
			HttpContext.SignOutAsync();

			if (throwException)
			{
				throw new UnauthorizedAccessException(Constants.Unauthorized);
			}
		}

		return _account;
	}

	#endregion
}