namespace Cornerstone.Avalonia.Documentation;

/// <summary>
/// Client script for static documentation: persist theme, density, and accent on the html element.
/// </summary>
public static class DocumentationSiteScripts
{
	#region Constants

	public const string JavaScript =
		"""
		(function () {
			var themeKey = "docs-theme";
			var densityKey = "docs-density";
			var colorKey = "docs-theme-color";
			var root = document.documentElement;
			var themeButton = document.getElementById("theme-toggle");
			var densitySelect = document.getElementById("density");
			var colorSelect = document.getElementById("theme-color");

			function storageGet(key) {
				try {
					return localStorage.getItem(key);
				} catch (e) {
					return null;
				}
			}

			function storageSet(key, value) {
				try {
					localStorage.setItem(key, value);
				} catch (e) {
				}
			}

			function preferredTheme() {
				return window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches
					? "dark"
					: "light";
			}

			function currentTheme() {
				return root.getAttribute("data-theme") || preferredTheme();
			}

			function knownColor(name) {
				if (!colorSelect || !name) {
					return name === "Blue";
				}
				for (var i = 0; i < colorSelect.options.length; i++) {
					if (colorSelect.options[i].value === name) {
						return true;
					}
				}
				return false;
			}

			function apply() {
				var theme = storageGet(themeKey);
				var density = storageGet(densityKey) || "normal";
				var color = storageGet(colorKey) || "Blue";
				if (theme === "light" || theme === "dark") {
					root.setAttribute("data-theme", theme);
				} else {
					root.removeAttribute("data-theme");
				}
				if (density !== "compact" && density !== "large" && density !== "normal") {
					density = "normal";
				}
				if (!knownColor(color)) {
					color = "Blue";
				}
				root.setAttribute("data-density", density);
				root.setAttribute("data-theme-color", color);
				if (densitySelect) {
					densitySelect.value = density;
				}
				if (colorSelect) {
					colorSelect.value = color;
				}
				if (themeButton) {
					var next = currentTheme() === "dark" ? "Light" : "Dark";
					themeButton.textContent = next;
					themeButton.setAttribute("title", "Switch to " + next.toLowerCase() + " theme");
				}
			}

			if (themeButton) {
				themeButton.addEventListener("click", function () {
					storageSet(themeKey, currentTheme() === "dark" ? "light" : "dark");
					apply();
				});
			}

			if (densitySelect) {
				densitySelect.addEventListener("change", function () {
					storageSet(densityKey, densitySelect.value);
					apply();
				});
			}

			if (colorSelect) {
				colorSelect.addEventListener("change", function () {
					storageSet(colorKey, colorSelect.value);
					apply();
				});
			}

			apply();
		})();
		""";

	#endregion
}