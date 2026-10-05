export class JsExports {
    public static resolvedExports?: any;
    public static exportsPromise: Promise<any>;

    public static get InputHelper(): any {
        return this.resolvedExports?.Cornerstone.Presentation.Browser.Interop.InputHelper;
    }

    public static get DomHelper(): any {
        return this.resolvedExports?.Cornerstone.Presentation.Browser.Interop.DomHelper;
    }

    public static get TimerHelper(): any {
        return this.resolvedExports?.Cornerstone.Presentation.Browser.Interop.TimerHelper;
    }

    public static get SingleThreadedDispatcherImpl(): any {
        return this.resolvedExports?.Cornerstone.Presentation.Browser.BrowserSingleThreadedDispatcherImpl;
    }

    public static get CanvasHelper(): any {
        return this.resolvedExports?.Cornerstone.Presentation.Browser.Interop.CanvasHelper;
    }
}
async function resolveExports (): Promise<any> {
    const runtimeApi = await globalThis.getDotnetRuntime(0);
    if (runtimeApi == null) { return; }
    JsExports.resolvedExports = await runtimeApi.getAssemblyExports("Cornerstone.Presentation.dll");
    return JsExports.resolvedExports;
}

JsExports.exportsPromise = resolveExports();
