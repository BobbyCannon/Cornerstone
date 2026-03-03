import { dotnet } from './_framework/dotnet.js'

const is_browser = typeof window != "undefined";
if (!is_browser) throw new Error(`Expected to be running in a browser`);

const dotnetRuntime = await dotnet
    .withDiagnosticTracing(false)
    .withApplicationArgumentsFromQuery()
    .withModuleConfig({
        onDownloadResourceProgress: (loaded, total) => {
            const progressElement = document.getElementById("progress");
            progressElement.value = (loaded / total) * 100;
        }
    })
    .create();

const config = dotnetRuntime.getConfig();

if (CornerstoneBrowser) {
	dotnetRuntime.setModuleImports("Cornerstone.Browser.js", CornerstoneBrowser);
}

await dotnetRuntime.runMain(config.mainAssemblyName, [globalThis.location.href]);