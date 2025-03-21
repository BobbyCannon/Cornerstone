var CornerstoneBrowser = CornerstoneBrowser ||
{
	localStorage: {
		getValue: (key) => globalThis.localStorage.getItem(key),
		setValue: (key, value) => globalThis.localStorage.setItem(key, value)
	},
	document: {
		createElement: function (parent, element) {
			var element = globalThis.document.createElement(element);
			parent.appendChild(element);
			return element;
		},
		hideElement: function (element) {
			element.style.display = 'none';
		},
		showElement: function (element) {
			element.style.display = '';
		}
	},
	window: {
		getLocation: () => globalThis.window.location.href,
		setLocation: x => globalThis.window.history.replaceState(null, null, x),
	}
};