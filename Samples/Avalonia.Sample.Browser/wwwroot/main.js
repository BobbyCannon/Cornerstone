import { dotnet } from './_framework/dotnet.js'

const is_browser = typeof window != "undefined";
if (!is_browser) throw new Error(`Expected to be running in a browser`);

const dotnetRuntime = await dotnet
	.withDiagnosticTracing(false)
	.withApplicationArgumentsFromQuery()
	.create();

const config = dotnetRuntime.getConfig();

dotnetRuntime.setModuleImports("main.js", {
    window: {
        getLocation: () => globalThis.window.location.href,
        setLocation: x => globalThis.window.history.replaceState(null, null, x),
    },
    localStorage: {
        getValue: (key) => globalThis.localStorage.getItem(key),
        setValue: (key, value) => globalThis.localStorage.setItem(key, value)
    }
});

await dotnetRuntime.runMain(config.mainAssemblyName, [globalThis.location.href]);