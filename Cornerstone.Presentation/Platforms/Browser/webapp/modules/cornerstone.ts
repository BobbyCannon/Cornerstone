import { InputHelper } from "./cornerstone/input";
import { CornerstoneDOM } from "./cornerstone/dom";
import { Caniuse } from "./cornerstone/caniuse";
import { StreamHelper } from "./cornerstone/stream";
import { NativeControlHost } from "./cornerstone/nativeControlHost";
import { NavigationHelper } from "./cornerstone/navigationHelper";
import { GeneralHelpers } from "./cornerstone/generalHelpers";
import { TimerHelper } from "./cornerstone/timer";
import { SingleThreadedDispatcherHelper } from "./cornerstone/singleThreadedDispatcher";
import { CanvasSurface } from "./cornerstone/rendering/canvasSurface";
import { WebRenderTargetRegistry } from "./cornerstone/rendering/webRenderTargetRegistry";
import { WebRenderTarget } from "./cornerstone/rendering/webRenderTarget";
import { SoftwareRenderTarget } from "./cornerstone/rendering/softwareRenderTarget";
import { WebGlRenderTarget } from "./cornerstone/rendering/webGlRenderTarget";
import { ScreenHelper } from "./cornerstone/screens";

function getModuleUrl(): string {
    return import.meta.url;
}

function resolveModuleUrl(name: string): string {
    const meta = import.meta as ImportMeta & { resolve?(specifier: string): string };
    return meta.resolve ? meta.resolve(name) : new URL(name, import.meta.url).href;
}

async function registerServiceWorker(path: string, scope: string | undefined) {
    if ("serviceWorker" in navigator) {
        await globalThis.navigator.serviceWorker.register(path, scope ? { scope } : undefined);
    }
}

export {
    Caniuse,
    InputHelper,
    CornerstoneDOM,
    StreamHelper,
    NativeControlHost,
    NavigationHelper,
    GeneralHelpers,
    ScreenHelper,
    TimerHelper,
    SingleThreadedDispatcherHelper,
    WebRenderTarget,
    CanvasSurface,
    WebRenderTargetRegistry,
    SoftwareRenderTarget,
    WebGlRenderTarget,
    registerServiceWorker,
    getModuleUrl,
    resolveModuleUrl
};
