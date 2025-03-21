#region References

using System;
using System.Diagnostics;
using System.Reflection;
using Cornerstone.Data;
using Cornerstone.Runtime;
using Cornerstone.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sample.Server.Website.Models;

#endregion

namespace Sample.Server.Website.Controllers;

public class HomeController : Controller
{
	#region Fields

	private readonly IDateTimeProvider _dateTimeProvider;

	#endregion

	#region Constructors

	public HomeController(IDateTimeProvider dateTimeProvider)
	{
		_dateTimeProvider = dateTimeProvider;
	}

	#endregion

	#region Methods

	[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	public IActionResult Error()
	{
		return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
	}

	public IActionResult Index()
	{
		return View();
	}

	[AllowAnonymous]
	public IActionResult LogIn()
	{
		return View();
	}

	[AllowAnonymous]
	public IActionResult LogOut()
	{
		return RedirectToAction(nameof(LogIn));
	}

	[AllowAnonymous]
	public IActionResult PagedRequest(PagedRequest request = null)
	{
		request ??= new PagedRequest();
		request.Cleanup();
		var result = new PagedResults<object>(request, 156324,
			2,
			"foo",
			_dateTimeProvider.UtcNow,
			TimeSpan.FromMilliseconds(123456),
			Assembly.GetAssembly(typeof(Entity))?.GetName().Version ?? new Version(1, 2, 3, 4)
		);
		return View(result);
	}

	[AllowAnonymous]
	public IActionResult Privacy()
	{
		return View();
	}

	#endregion
}