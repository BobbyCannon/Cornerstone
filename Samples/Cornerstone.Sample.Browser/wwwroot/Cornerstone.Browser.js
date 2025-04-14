var CornerstoneBrowser = CornerstoneBrowser ||
{
	localStorage: {
		getValue: (key) => globalThis.localStorage.getItem(key),
		setValue: (key, value) => globalThis.localStorage.setItem(key, value)
	},
	document: {
		createElement: (parent, tagName) => {
			if (typeof tagName !== 'string') {
				throw new Error('tagName must be a string');
			}
			const element = globalThis.document.createElement(tagName);
			if (parent) {
				parent.appendChild(element)
			}
			return element;
		},
		hideElement: (element) => {
			element.classList.add('hide');
			return element;
		},
		showElement: (element) => {
			element.classList.remove('hide');
			return element;
		}
	},
	window: {
		getLocation: () => globalThis.window.location.href,
		setLocation: x => globalThis.window.history.replaceState(null, null, x),
		checkPermission: async (permissionName) => {
			if (!navigator.permissions || !navigator.permissions.query) {
				return "unsupported";
			}
			try {
				const result = await navigator.permissions.query({ name: permissionName });
				return result.state;
			} catch (err) {
				return "unknown";
			}
		},
		requestMediaPermission: async (mediaType) => {
			const constraints = {};
			if (mediaType === "audio") constraints.audio = true;
			else if (mediaType === "video") constraints.video = true;
			else return Promise.reject(new Error("Invalid media type"));
			try {
				const stream = await navigator.mediaDevices.getUserMedia(constraints);
				stream.getTracks().forEach(track => track.stop());
				return "granted";
			} catch (err) {
				throw err;
			}
		}
	}
};