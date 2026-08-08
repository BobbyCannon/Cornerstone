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
				parent.appendChild(element);
			}
			return element;
		},
		hideElement: (element) => {
			if (element) {
				element.classList.add('hide');
			}
			return element;
		},
		showElement: (element) => {
			if (element) {
				element.classList.remove('hide');
			}
			return element;
		},
		/**
		 * WebView overlay: MUST attach to document.body (not #out).
		 * Putting nodes inside Avalonia's #out resizes the host and can infinite-loop layout on WASM.
		 * Use position:fixed so bounds match PointToScreen / viewport CSS pixels.
		 */
		attachOverlay: (element) => {
			if (!element) {
				return element;
			}

			element.classList.add('cornerstone-webview-host');
			element.style.position = 'fixed';
			element.style.left = '0';
			element.style.top = '0';
			element.style.width = '0';
			element.style.height = '0';
			element.style.margin = '0';
			element.style.padding = '0';
			element.style.border = 'none';
			element.style.overflow = 'hidden';
			element.style.boxSizing = 'border-box';
			// Above Avalonia canvas; pause/hide removes hit-testing when needed.
			element.style.zIndex = '10';
			element.style.pointerEvents = 'auto';

			for (const child of element.children) {
				child.style.width = '100%';
				child.style.height = '100%';
				child.style.border = 'none';
				child.style.margin = '0';
				child.style.padding = '0';
				child.style.boxSizing = 'border-box';
			}

			// Never parent under #out — that is Avalonia's layout root.
			if (element.parentNode !== globalThis.document.body) {
				globalThis.document.body.appendChild(element);
			}
			return element;
		},
		setOverlayBounds: (element, x, y, width, height) => {
			if (!element) {
				return element;
			}
			const w = Math.max(0, width || 0);
			const h = Math.max(0, height || 0);
			element.style.left = (x || 0) + 'px';
			element.style.top = (y || 0) + 'px';
			element.style.width = w + 'px';
			element.style.height = h + 'px';
			return element;
		},
		detachOverlay: (element) => {
			if (element && element.parentNode) {
				element.parentNode.removeChild(element);
			}
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
