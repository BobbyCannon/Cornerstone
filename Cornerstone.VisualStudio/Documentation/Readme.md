# Cornerstone Visual Studio Documentation

Notes and plans for the **Cornerstone.VisualStudio** extension (Cornerstone designer / VSSDK).

| Document | Summary |
|----------|---------|
| [HowItWorks.md](HowItWorks.md) | Walk of the extension: package, designer, preview frames, editor host, and where each step can stall the Visual Studio shell |
| [AutoComplete.md](AutoComplete.md) | CXAML/AXAML IntelliSense: invoke, elements, attributes, caret after commit, Classes="", limits |
| [Editor.md](Editor.md) | How typing should work in the designer source view (Enter and indent first; add examples here) |
| [ExtensibilityPlatform.md](ExtensibilityPlatform.md) | VSSDK vs VisualStudio.Extensibility — why classic SDK remains |
| [NextRelease.md](NextRelease.md) | Next release notes / checklist |
| [Todo/Optimization.md](Todo/Optimization.md) | Performance optimization plan for the designer |
| [Todo/ClickToSourceNavigation.md](Todo/ClickToSourceNavigation.md) | Design: click preview → navigate to AXAML (deferred) |
| [Todo/BlankPreview.md](Todo/BlankPreview.md) | Blank preview: host accepts XAML, no frame painted yet |
| [Todo/ShellLockup.md](Todo/ShellLockup.md) | Open leads for the shell ignoring input until a key or a resize |

Framework-level docs live in **Cornerstone.Documentation** (separate reader EXE).
