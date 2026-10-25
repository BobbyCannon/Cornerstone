// Host helpers previously shipped as the classic Cornerstone.Browser.js script.
// They are exports of the cornerstone module, which ImportMain loads for every browser app.

const browserStorage = {
    getValue: (key: string): string | null => globalThis.localStorage.getItem(key),
    setValue: (key: string, value: string): void => globalThis.localStorage.setItem(key, value)
};

const browserDocument = {
    createElement: (parent: HTMLElement | null, tagName: string): HTMLElement => {
        if (typeof tagName !== "string") {
            throw new Error("tagName must be a string");
        }

        const element = globalThis.document.createElement(tagName);
        if (parent) {
            parent.appendChild(element);
        }

        return element;
    },
    hideElement: (element: HTMLElement | null): HTMLElement | null => {
        if (element) {
            element.classList.add("hide");
        }

        return element;
    },
    showElement: (element: HTMLElement | null): HTMLElement | null => {
        if (element) {
            element.classList.remove("hide");
        }

        return element;
    },
    // WebView overlay attaches to document.body, never under #out.
    // Nodes inside #out resize the host and can infinite-loop layout on WASM.
    // position:fixed keeps bounds in viewport CSS pixels for PointToScreen.
    attachOverlay: (element: HTMLElement | null): HTMLElement | null => {
        if (!element) {
            return element;
        }

        element.classList.add("cornerstone-webview-host");
        element.style.position = "fixed";
        element.style.left = "0";
        element.style.top = "0";
        element.style.width = "0";
        element.style.height = "0";
        element.style.margin = "0";
        element.style.padding = "0";
        element.style.border = "none";
        element.style.overflow = "hidden";
        element.style.boxSizing = "border-box";
        // Above the Cornerstone canvas. Pause and hide clear hit-testing when needed.
        element.style.zIndex = "10";
        element.style.pointerEvents = "auto";

        for (const child of element.children) {
            const hosted = child as HTMLElement;
            hosted.style.width = "100%";
            hosted.style.height = "100%";
            hosted.style.border = "none";
            hosted.style.margin = "0";
            hosted.style.padding = "0";
            hosted.style.boxSizing = "border-box";
        }

        if (element.parentNode !== globalThis.document.body) {
            globalThis.document.body.appendChild(element);
        }

        return element;
    },
    setOverlayBounds: (element: HTMLElement | null, x: number, y: number, width: number, height: number): HTMLElement | null => {
        if (!element) {
            return element;
        }

        const w = Math.max(0, width || 0);
        const h = Math.max(0, height || 0);
        element.style.left = (x || 0) + "px";
        element.style.top = (y || 0) + "px";
        element.style.width = w + "px";
        element.style.height = h + "px";
        return element;
    },
    detachOverlay: (element: HTMLElement | null): HTMLElement | null => {
        if (element && element.parentNode) {
            element.parentNode.removeChild(element);
        }

        return element;
    }
};

const browserWindow = {
    getLocation: (): string => globalThis.window.location.href,
    setLocation: (location: string): string => {
        globalThis.window.history.replaceState(null, "", location);
        return location;
    },
    checkPermission: async (permissionName: string): Promise<string> => {
        if (!globalThis.navigator.permissions?.query) {
            return "unsupported";
        }

        try {
            const result = await globalThis.navigator.permissions.query({ name: permissionName } as PermissionDescriptor);
            return result.state;
        } catch {
            return "unknown";
        }
    },
    requestMediaPermission: async (mediaType: string): Promise<string> => {
        const constraints: MediaStreamConstraints = {};
        if (mediaType === "audio") {
            constraints.audio = true;
        } else if (mediaType === "video") {
            constraints.video = true;
        } else {
            throw new Error("Invalid media type");
        }

        const stream = await globalThis.navigator.mediaDevices.getUserMedia(constraints);
        stream.getTracks().forEach(track => track.stop());
        return "granted";
    }
};

// W3C standard gamepad: 16 button values, then leftX, leftY, rightX, rightY.
// Read from Update. No animation-frame loop.
export class GamepadHelper {
    public static read(index: number): string {
        if (typeof globalThis.navigator.getGamepads !== "function") {
            return "";
        }

        const pads = globalThis.navigator.getGamepads();
        if (!pads) {
            return "";
        }

        const pad = pads[index];
        if (!pad || !pad.connected) {
            return "";
        }

        const values: number[] = [];
        for (let i = 0; i < 16; i++) {
            values.push(pad.buttons[i]?.value ?? 0);
        }

        for (let i = 0; i < 4; i++) {
            values.push(pad.axes[i] ?? 0);
        }

        return values.join(",");
    }
}

export { browserStorage as localStorage, browserDocument as document, browserWindow as window };
