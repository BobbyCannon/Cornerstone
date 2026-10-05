#region References

using Cornerstone.Automation.Web.Elements;
using Cornerstone.Automation.Internal;
using System.Text.Json.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Data;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Automation.Web;

[Notifiable(["*"])]
public abstract partial class WebAutomation : WebElement
{
	#region Fields

	public static readonly string[] UsernameAutocompleteTokens = ["username", "email", "login"];
	public static readonly string[] UsernameNameTokens = ["user", "email", "login", "account"];

	#endregion

	#region Constructors

	protected WebAutomation() : base(null, null, null)
	{
		CredentialElements = new CredentialElements();
		JavascriptLibraries = [];
		TimeProvider = DateTimeProvider.RealTime;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets or sets a flag that allows elements to refresh when reading properties. Defaults to true.
	/// </summary>
	[Notify]
	public partial bool AutoRefresh { get; set; }

	public CredentialElements CredentialElements { get; }

	/// <summary>
	/// True while the element tree is being refreshed.
	/// </summary>
	[Notify]
	public partial bool IsRefreshing { get; set; }

	/// <summary>
	/// Gets a list of JavaScript libraries that were detected on the page.
	/// </summary>
	public IEnumerable<JavaScriptLibrary> JavascriptLibraries { get; set; }

	/// <summary>
	/// Gets or sets a flag to tell the browser to act slower. Defaults to false.
	/// </summary>
	[Notify]
	public partial bool SlowMotion { get; set; }

	/// <summary>
	/// Time source for waits and delays. Defaults to real time.
	/// </summary>
	public IDateTimeProvider TimeProvider { get; set; }

	#endregion

	#region Methods

	public static string CleanupScriptForJavascriptString(string html)
	{
		return ReplaceInReverse(html, new Dictionary<char, string> { { '\'', "\\'" }, { '\"', "\\\"" }, { '\n', "\\n" }, { '\r', "\\r" }, { '\0', "\\0" } });
	}

	public void DetectCredentialFields()
	{
		foreach (var form in WhereDescendants<Form>())
		{
			var password = form.FirstOrDefaultDescendants<Elements.Input>(IsPasswordInput);
			if (password == null)
			{
				continue;
			}

			var username = FindBestUsername(form);
			CredentialElements.Password = password;
			CredentialElements.UserName = username;
			if (username != null)
			{
				return;
			}
		}

		// Username-then-password pages, or inputs that are not inside a form.
		CredentialElements.Password ??= FirstOrDefaultDescendants<Elements.Input>(IsPasswordInput);
		CredentialElements.UserName ??= FindBestUsername(this);
	}

	/// <summary>
	/// Execute a javascript snippet.
	/// </summary>
	/// <param name="script"> The script to execute. </param>
	/// <returns> The results from the script. </returns>
	public abstract Task<string> ExecuteJavaScriptAsync(string script);

	/// <summary>
	/// Refresh the children for this element host.
	/// </summary>
	public Task RefreshAsync(CancellationToken? cancellationToken = null)
	{
		return RefreshAsync<Element>(_ => false, cancellationToken);
	}

	public virtual async Task RefreshAsync<T>(Func<T, bool> condition, CancellationToken? cancellationToken = null)
	{
		if (IsRefreshing)
		{
			return;
		}

		IsRefreshing = true;
		try
		{
			CredentialElements.Reset();
			if (!await InjectTestScriptAsync())
			{
				return;
			}

			await DetectJavascriptLibrariesAsync();
			await RefreshElementsAsync();
			DetectCredentialFields();
		}
		finally
		{
			IsRefreshing = false;
		}
	}

	protected abstract string GetUri();

	/// <summary>
	/// Injects the test script into the browser. Waits until the engine can run script, then retries inject.
	/// Returns false when the page never became scriptable.
	/// </summary>
	protected async Task<bool> InjectTestScriptAsync()
	{
		var script = ResourceService.GetTestScript();
		if (string.IsNullOrEmpty(script))
		{
			return false;
		}

		for (var attempt = 0; attempt < 20; attempt++)
		{
			var readyState = NormalizeJavaScriptString(await ExecuteJavaScriptAsync("document.readyState"));
			if ((readyState != "interactive") && (readyState != "complete"))
			{
				await Task.Delay(150);
				continue;
			}

			var test = await ExecuteJavaScriptAsync("typeof Cornerstone");
			if (!IsJavaScriptUndefined(test))
			{
				return true;
			}

			await ExecuteJavaScriptAsync(script);
			test = await ExecuteJavaScriptAsync("typeof Cornerstone");
			if (!IsJavaScriptUndefined(test))
			{
				return true;
			}

			await Task.Delay(150);
		}

		return false;
	}

	protected abstract void NavigateTo(string uri);

	private async Task DetectJavascriptLibrariesAsync()
	{
		var uri = GetUri();
		if (string.IsNullOrEmpty(uri) || uri.Equals("about:tabs"))
		{
			return;
		}

		var libraries = new List<JavaScriptLibrary>();
		if ((await ExecuteJavaScriptAsync("typeof jQuery !== 'undefined'")).Equals("true", StringComparison.OrdinalIgnoreCase))
		{
			libraries.Add(JavaScriptLibrary.JQuery);
		}

		if ((await ExecuteJavaScriptAsync("typeof Vue !== 'undefined'")).Equals("true", StringComparison.OrdinalIgnoreCase))
		{
			libraries.Add(JavaScriptLibrary.Vue);
		}

		if ((await ExecuteJavaScriptAsync("typeof angular !== 'undefined'")).Equals("true", StringComparison.OrdinalIgnoreCase))
		{
			libraries.Add(JavaScriptLibrary.Angular);
		}

		if ((await ExecuteJavaScriptAsync("typeof moment !== 'undefined'")).Equals("true", StringComparison.OrdinalIgnoreCase))
		{
			libraries.Add(JavaScriptLibrary.Moment);
		}

		if ((await ExecuteJavaScriptAsync("typeof $().emulateTransitionEnd == 'function'")).Equals("true", StringComparison.OrdinalIgnoreCase))
		{
			libraries.Add(JavaScriptLibrary.Bootstrap3);
		}

		if (!libraries.Contains(JavaScriptLibrary.Bootstrap3)
			&& (await ExecuteJavaScriptAsync("typeof($.fn.popover) !== 'undefined'")).Equals("true", StringComparison.OrdinalIgnoreCase))
		{
			libraries.Add(JavaScriptLibrary.Bootstrap2);
		}

		JavascriptLibraries = libraries;
	}

	private static bool IsJavaScriptUndefined(string value)
	{
		var normalized = NormalizeJavaScriptString(value);
		return string.IsNullOrEmpty(normalized)
			|| normalized.Equals("undefined", StringComparison.OrdinalIgnoreCase);
	}

	private static string NormalizeJavaScriptString(string value)
	{
		return (value ?? string.Empty).Trim().Trim('"');
	}

	private static bool ContainsAny(string value, string[] tokens)
	{
		if (string.IsNullOrEmpty(value))
		{
			return false;
		}

		for (var i = 0; i < tokens.Length; i++)
		{
			if (value.Contains(tokens[i], StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}

	private static Elements.Input FindBestUsername(WebElement root)
	{
		Elements.Input best = null;
		var bestScore = int.MaxValue;

		foreach (var input in root.WhereDescendants<Elements.Input>())
		{
			var score = ScoreUsernameCandidate(input);
			if ((score < 0) || (score >= bestScore))
			{
				continue;
			}

			bestScore = score;
			best = input;
			if (score == 0)
			{
				return best;
			}
		}

		return best;
	}

	private static bool IsPasswordInput(Elements.Input input)
	{
		return input.GetAttributeValue("type", false).Equals("password", StringComparison.OrdinalIgnoreCase);
	}

	private static int ScoreUsernameCandidate(Elements.Input input)
	{
		var type = input.GetAttributeValue("type", false);
		if (type.Equals("password", StringComparison.OrdinalIgnoreCase))
		{
			return -1;
		}

		if (ContainsAny(input.GetAttributeValue("autocomplete", false), UsernameAutocompleteTokens))
		{
			return 0;
		}

		if (type.Equals("email", StringComparison.OrdinalIgnoreCase))
		{
			return 1;
		}

		if (ContainsAny(input.GetAttributeValue("name", false), UsernameNameTokens)
			|| ContainsAny(input.GetAttributeValue("id", false), UsernameNameTokens)
			|| ContainsAny(input.GetAttributeValue("placeholder", false), UsernameAutocompleteTokens)
			|| ContainsAny(input.GetAttributeValue("label", false), UsernameAutocompleteTokens))
		{
			return 2;
		}

		if (type.Length == 0
			|| type.Equals("text", StringComparison.OrdinalIgnoreCase)
			|| type.Equals("tel", StringComparison.OrdinalIgnoreCase))
		{
			return 3;
		}

		return -1;
	}

	/// <summary>
	/// Replaces a character with a string processing input in reverse. Does not replace if the string replacement matches at starting character.
	/// </summary>
	/// <param name="input"> The string to parse. </param>
	/// <param name="replacements"> The collection of trigger characters and replacements. </param>
	/// <returns> The updated string. </returns>
	protected static string ReplaceInReverse(string input, Dictionary<char, string> replacements)
	{
		foreach (var r in replacements)
		{
			for (var i = input.Length - 1; i > 0; i--)
			{
				if (input[i] != r.Key)
				{
					continue;
				}

				if (((i - r.Value.Length) >= 0) && (input.Substring(i - r.Value.Length, r.Value.Length) != r.Value))
				{
					input = input.Remove(i, 1).Insert(i, r.Value);
				}
			}
		}

		return input;
	}

	internal async Task<ICollection<WebElement>> GetElementsAsync(WebElement parent = null)
	{
		var data = await ExecuteJavaScriptAsync(parent?.Id == null
			? $"Cornerstone.getElements(undefined,{parent?.GetFrameIdInsert() ?? "undefined"})"
			: $"Cornerstone.getElements('{parent.Id}',{parent.GetFrameIdInsert()})");

		var elements = data.AsJsonNode() as JsonArray;
		return elements?.Select(x => Create(x, this, parent)).ToList() ?? [];
	}

	/// <summary>
	/// Refreshes the element collection for the current page.
	/// </summary>
	private async Task RefreshElementsAsync()
	{
		//LogManager.Write("Refresh the elements.", LogLevel.Verbose);
		Clear();

		var elements = await GetElementsAsync();
		var groups = elements.GroupBy(x => x.Id).ToList();
		var elementLookup = groups
			.Select(x => x.First())
			.ToDictionary(x => x.Id, x => x);

		var parents = new Dictionary<string, WebElement>();

		foreach (var element in elements)
		{
			if (string.IsNullOrWhiteSpace(element.ParentId))
			{
				parents[element.Id] = element;
			}
			else
			{
				elementLookup[element.ParentId].Children.Add(element);
			}
		}

		Load(parents.Values);
	}

	#endregion
}