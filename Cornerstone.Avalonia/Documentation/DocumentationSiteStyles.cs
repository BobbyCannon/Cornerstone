namespace Cornerstone.Avalonia.Documentation;

/// <summary>
/// Handwritten layout. Colors and sizes come only from generated theme.css variables.
/// </summary>
public static class DocumentationSiteStyles
{
	#region Constants

	public const string Css =
		"""
		@font-face {
			font-family: "Open Sans";
			src: url("fonts/OpenSans-Regular.ttf") format("truetype");
			font-weight: 400;
			font-style: normal;
			font-display: swap;
		}

		@font-face {
			font-family: "Open Sans";
			src: url("fonts/OpenSans-Italic.ttf") format("truetype");
			font-weight: 400;
			font-style: italic;
			font-display: swap;
		}

		@font-face {
			font-family: "Open Sans";
			src: url("fonts/OpenSans-Bold.ttf") format("truetype");
			font-weight: 700;
			font-style: normal;
			font-display: swap;
		}

		@font-face {
			font-family: "Open Sans";
			src: url("fonts/OpenSans-BoldItalic.ttf") format("truetype");
			font-weight: 700;
			font-style: italic;
			font-display: swap;
		}

		@font-face {
			font-family: "Open Sans";
			src: url("fonts/OpenSans-Light.ttf") format("truetype");
			font-weight: 300;
			font-style: normal;
			font-display: swap;
		}

		@font-face {
			font-family: "DejaVu Sans Mono";
			src: url("fonts/DejaVuSansMono.ttf") format("truetype");
			font-weight: 400;
			font-style: normal;
			font-display: swap;
		}

		@font-face {
			font-family: "DejaVu Sans Mono";
			src: url("fonts/DejaVuSansMono-Bold.ttf") format("truetype");
			font-weight: 700;
			font-style: normal;
			font-display: swap;
		}

		@font-face {
			font-family: "DejaVu Sans Mono";
			src: url("fonts/DejaVuSansMono-Oblique.ttf") format("truetype");
			font-weight: 400;
			font-style: italic;
			font-display: swap;
		}

		@font-face {
			font-family: "DejaVu Sans Mono";
			src: url("fonts/DejaVuSansMono-BoldOblique.ttf") format("truetype");
			font-weight: 700;
			font-style: italic;
			font-display: swap;
		}

		html, body {
			margin: 0;
			padding: 0;
			background: var(--Background02);
			color: var(--Foreground00);
			font-family: "Open Sans", sans-serif;
			font-size: var(--ControlFontSize);
			line-height: 1.25;
		}

		.site-header {
			display: flex;
			align-items: center;
			justify-content: space-between;
			gap: 16px;
			background: var(--Background01);
			border-bottom: var(--ControlBorderThickness) solid var(--BorderBrush);
			padding: 10px 24px;
		}

		.site-home {
			color: var(--Theme-Accent);
			text-decoration: none;
		}

		.site-home:hover {
			text-decoration: underline;
		}

		.site-toolbar {
			display: flex;
			align-items: center;
			gap: 8px;
		}

		.site-toolbar label {
			display: flex;
			align-items: center;
			gap: 6px;
			color: var(--Foreground00);
			font-size: var(--ControlFontSize);
		}

		.site-toolbar select,
		.site-toolbar button {
			box-sizing: border-box;
			display: inline-flex;
			align-items: center;
			height: 2rem;
			margin: 0;
			font: inherit;
			font-size: var(--ControlFontSize);
			line-height: 1;
			color: var(--Foreground00);
			background: var(--Background03);
			border: var(--ControlBorderThickness) solid var(--BorderBrush);
			border-radius: var(--ControlCornerRadius);
			padding: 0 10px;
		}

		.site-toolbar button {
			cursor: pointer;
			min-width: 4.5em;
			justify-content: center;
		}

		.breadcrumbs {
			box-sizing: border-box;
			max-width: 920px;
			margin: 12px auto 0;
			padding: 0 16px;
			font-size: var(--ControlFontSize);
			font-family: "Open Sans", sans-serif;
		}

		.breadcrumbs ol {
			display: flex;
			flex-wrap: wrap;
			align-items: center;
			gap: 0;
			list-style: none;
			margin: 0;
			padding: 0;
		}

		.breadcrumbs li {
			display: flex;
			align-items: center;
			color: var(--Foreground00);
		}

		.breadcrumbs li:not(:last-child)::after {
			content: "/";
			margin: 0 8px;
			color: var(--Foreground00);
			opacity: 0.55;
		}

		.breadcrumbs a {
			color: var(--Theme-Accent);
			text-decoration: none;
		}

		.breadcrumbs a:hover {
			text-decoration: underline;
		}

		.markdown-body {
			box-sizing: border-box;
			max-width: 920px;
			margin: 12px auto 16px;
			padding: 24px 28px 32px;
			background: var(--Background03);
			color: var(--Foreground00);
			font-family: "Open Sans", sans-serif;
			font-size: var(--ControlFontSize);
			line-height: 1.25;
			border: var(--ControlBorderThickness) solid var(--BorderBrush);
			border-radius: 8px;
			display: flex;
			flex-direction: column;
			gap: 10px;
		}

		.markdown-body > * {
			margin: 0;
		}

		.markdown-body h1,
		.markdown-body h2,
		.markdown-body h3,
		.markdown-body h4,
		.markdown-body h5,
		.markdown-body h6 {
			font-family: "Open Sans", sans-serif;
			font-weight: 400;
			line-height: 1.2;
		}

		.markdown-body h1 { font-size: calc(var(--ControlFontSize) * 2.6); }
		.markdown-body h2 { font-size: calc(var(--ControlFontSize) * 2.2); }
		.markdown-body h3 { font-size: calc(var(--ControlFontSize) * 2.0); }
		.markdown-body h4 { font-size: calc(var(--ControlFontSize) * 1.6); }
		.markdown-body h5 { font-size: calc(var(--ControlFontSize) * 1.4); }
		.markdown-body h6 { font-size: calc(var(--ControlFontSize) * 1.2); }

		.markdown-body p {
			font-family: inherit;
			font-weight: 400;
		}

		.markdown-body a {
			color: var(--Theme-Accent);
		}

		.markdown-body strong {
			font-weight: 700;
		}

		.markdown-body em {
			font-style: italic;
		}

		.markdown-body ul {
			list-style: none;
			padding: 0;
		}

		.markdown-body li {
			white-space: pre-wrap;
		}

		.markdown-body li::before {
			content: "• ";
		}

		.markdown-body table {
			border-collapse: separate;
			border-spacing: 0;
			width: 100%;
			border: var(--ControlBorderThickness) solid var(--BorderBrush);
			border-radius: 4px;
			overflow: hidden;
			background: var(--Background03);
		}

		.markdown-body th,
		.markdown-body td {
			border: 0;
			border-top: var(--ControlBorderThickness) solid var(--BorderBrush);
			border-left: var(--ControlBorderThickness) solid var(--BorderBrush);
			padding: 6px 8px;
			text-align: left;
			font-weight: 400;
		}

		.markdown-body tr:first-child th,
		.markdown-body tr:first-child td {
			border-top: 0;
		}

		.markdown-body th:first-child,
		.markdown-body td:first-child {
			border-left: 0;
		}

		.markdown-body th {
			background: var(--Background04);
			font-weight: 700;
		}

		.markdown-body blockquote {
			padding: 10px;
			border: var(--ControlBorderThickness) solid var(--BorderBrush);
			border-radius: 4px;
			background: var(--Background04);
		}

		.markdown-body .code-block {
			display: flex;
			flex-direction: column;
		}

		.markdown-body .code-block-header {
			box-sizing: border-box;
			height: 36px;
			padding: 0 10px;
			display: flex;
			align-items: center;
			background: var(--Background04);
			border: var(--ControlBorderThickness) solid var(--BorderBrush);
			border-bottom: 0;
			border-radius: 4px 4px 0 0;
			font-family: "Open Sans", sans-serif;
			font-size: var(--ControlFontSize);
		}

		.markdown-body pre {
			margin: 0;
			padding: 10px;
			overflow: auto;
			background: var(--Background04);
			border: var(--ControlBorderThickness) solid var(--BorderBrush);
			border-radius: 4px;
		}

		.markdown-body .code-block pre {
			border-radius: 0 0 4px 4px;
		}

		.markdown-body code {
			font-family: "DejaVu Sans Mono", monospace;
			font-size: var(--ControlFontSize);
		}

		.markdown-body pre code {
			font-size: inherit;
		}

		.markdown-body hr {
			border: 0;
			border-bottom: var(--ControlBorderThickness) solid var(--BorderBrush);
			height: 0;
		}

		::selection {
			background: var(--SelectionColor);
		}
		""";

	#endregion
}