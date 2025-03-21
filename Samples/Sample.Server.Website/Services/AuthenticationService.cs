#region References

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Claims;
using Cornerstone;
using Cornerstone.Runtime;
using Cornerstone.Storage;
using Cornerstone.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Sample.Shared.Storage;
using Sample.Shared.Storage.Server;
using Sample.Shared.Storage.Sync;

#endregion

namespace Sample.Server.Website.Services;

[ExcludeFromCodeCoverage]
public class AuthenticationService : IAuthenticationService
{
	#region Fields

	private readonly IHttpContextAccessor _contextAccessor;
	private readonly AccountService _service;
	private readonly IDateTimeProvider _dateTimeProvider;
	private static readonly MemoryCache _userCache;

	#endregion

	#region Constructors

	public AuthenticationService(AccountService service,
		IDateTimeProvider dateTimeProvider,
		IHttpContextAccessor contextAccessor)
	{
		_service = service;
		_dateTimeProvider = dateTimeProvider;
		_contextAccessor = contextAccessor;
	}

	static AuthenticationService()
	{
		_userCache = new MemoryCache(new MemoryCacheOptions { ExpirationScanFrequency = TimeSpan.FromSeconds(5) });
	}

	#endregion

	#region Properties

	public bool IsAuthenticated => _contextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

	public ClaimsPrincipal User => _contextAccessor.HttpContext?.User;

	public int UserId => int.TryParse(User.Identity?.Name, out var id) ? id : 0;

	#endregion

	#region Methods

	public static void AddOrUpdateCacheEntry(int userId, DateTime modifiedOn)
	{
		// Try and clear the cache first
		_userCache.Remove(userId);

		// Add the user modified on to the cache
		_userCache.Set(userId, modifiedOn, new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(5) });
	}

	public static AuthenticationTicket CreateTicket(AccountEntity account, bool rememberMe, string authenticationType)
	{
		if (account == null)
		{
			throw new ArgumentException(nameof(account));
		}

		var roles = Account.SplitRoles(account.Roles);
		var claims = new List<Claim>
		{
			new(ClaimTypes.Name, account.Id.ToString())
		};

		claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

		var identity = new ClaimsIdentity(claims, authenticationType);
		var principal = new ClaimsPrincipal(identity);
		var ticket = new AuthenticationTicket(principal, authenticationType)
		{
			Properties = { IsPersistent = rememberMe }
		};
		ticket.Properties.Items.Add(nameof(AccountEntity.ModifiedOn), account.ModifiedOn.Ticks.ToString());

		return ticket;
	}

	public bool LogIn(WebCredential credentials)
	{
		if (string.IsNullOrWhiteSpace(credentials.Password))
		{
			throw new UnauthorizedAccessException(Babel.Tower[BabelKeys.AuthenticationFailed]);
		}

		var user = _service.AuthenticateAccount(credentials);
		if (user == null)
		{
			return false;
		}

		user.LastLoginDate = _dateTimeProvider.UtcNow;
		var ticket = CreateTicket(user, credentials.RememberMe, CookieAuthenticationDefaults.AuthenticationScheme);
		_contextAccessor.HttpContext?.SignInAsync(ticket.Principal, ticket.Properties);
		return true;
	}

	public void LogIn(AccountEntity user)
	{
		user.LastLoginDate = _dateTimeProvider.UtcNow;
		var ticket = CreateTicket(user, true, CookieAuthenticationDefaults.AuthenticationScheme);
		_contextAccessor.HttpContext?.SignInAsync(ticket.Principal, ticket.Properties);
	}

	public void LogOut()
	{
		_contextAccessor.HttpContext?.SignOutAsync();
	}

	public static void ValidatePrincipal(CookieValidatePrincipalContext context, IDatabaseProvider<IServerDatabase> databaseProvider)
	{
		if (!context.Properties.Items.TryGetValue(nameof(AccountEntity.ModifiedOn), out var modifiedOnValue))
		{
			context.RejectPrincipal();
			return;
		}

		if (!long.TryParse(modifiedOnValue, out var modifiedOnTicks))
		{
			context.RejectPrincipal();
			return;
		}

		var modifiedOn = new DateTime(modifiedOnTicks);
		var accountId = GetAccountId(context.Principal);
		DateTime? userModifiedOn = null;

		if (_userCache.TryGetValue<DateTime>(accountId, out var cachedValue))
		{
			userModifiedOn = cachedValue;
		}

		// Now unsure the user has not changed, if it has reject the principal or renew it?
		if (userModifiedOn == modifiedOn)
		{
			// Everything is fine so just bounce.
			return;
		}

		using var database = databaseProvider.GetDatabase();
		var userEntity = database.Accounts.FirstOrDefault(u => u.Id == accountId);

		if (userEntity == null)
		{
			// Failed to find the user so reject the cookie
			_userCache.Remove(accountId);
			context.RejectPrincipal();
			return;
		}

		// Update the cookie because something has changed. Ex. Roles, Name, etc.
		var ticket = CreateTicket(userEntity, true, CookieAuthenticationDefaults.AuthenticationScheme);
		context.HttpContext.SignInAsync(ticket.Principal, ticket.Properties);
		context.ReplacePrincipal(ticket.Principal);

		// Refresh the local cache
		AddOrUpdateCacheEntry(accountId, userEntity.ModifiedOn);
	}

	/// <summary>
	/// Get the user ID from the claims principal.
	/// </summary>
	/// <param name="principal"> The claims principal. </param>
	/// <returns> The ID of the user. </returns>
	public static int GetAccountId(ClaimsPrincipal principal)
	{
		if (principal.Identity?.IsAuthenticated != true)
		{
			return 0;
		}

		var claim = principal.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
		return int.TryParse(claim?.Value ?? "0", out var id) ? id : 0;
	}

	#endregion
}

public interface IAuthenticationService
{
	#region Properties

	bool IsAuthenticated { get; }
	ClaimsPrincipal User { get; }
	int UserId { get; }

	#endregion

	#region Methods

	bool LogIn(WebCredential credentials);
	void LogIn(AccountEntity account);
	void LogOut();

	#endregion
}