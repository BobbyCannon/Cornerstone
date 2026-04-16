#region References

using System;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Generators;

public class MarkdownGenerator
{
	#region Fields

	private bool _initialHeaderEmitted;
	private int _sectionCounter;
	private readonly string[] _sectionTitles;
	private readonly string[] _topics;

	#endregion

	#region Constructors

	public MarkdownGenerator()
	{
		_sectionCounter = 1;
		_sectionTitles =
		[
			"Performance Analysis", "Live Streaming Demo", "Core Features", "Advanced Techniques",
			"Benchmark Results", "Implementation Details", "Future Improvements", "Real-World Usage",
			"Optimization Strategies", "Architecture Overview"
		];
		_topics =
		[
			"Avalonia", "Markdown rendering", "real-time streaming", "UI performance",
			"reactive updates", "cross-platform", "memory usage", "rendering pipeline"
		];
	}

	#endregion

	#region Methods

	/// <summary>
	/// Writes the next markdown chunk directly into the provided buffer.
	/// Reduced paragraph density for better visual variety.
	/// </summary>
	public void GetNext(IStringBuffer buffer, int avgChunkSize = 60, int minChunkSize = 15)
	{
		buffer.Clear();

		if (!_initialHeaderEmitted)
		{
			_initialHeaderEmitted = true;
			buffer.Append("# 🚀 Endless Markdown Stress Test\n\n");
			buffer.Append("## Live Continuous Benchmark\n\n");
			buffer.Append("This stream will continue generating rich markdown on each call.\n\n");
			return;
		}

		// Major section header (less frequent)
		if ((_sectionCounter % RandomGenerator.NextInteger(4, 9)) == 0)
		{
			buffer.Append("## ");
			buffer.Append(_sectionTitles[RandomGenerator.NextInteger(_sectionTitles.Length)]);
			buffer.Append(" #");
			buffer.Append(_sectionCounter.ToString());
			buffer.Append(Environment.NewLine);
			buffer.Append(Environment.NewLine);
		}

		// Sub-header with short description
		else if (RandomGenerator.NextInteger(100) < 35)
		{
			buffer.Append("### ");
			RandomGenerator.LoremIpsum(buffer, 3, 6);
			buffer.Append(Environment.NewLine);
			buffer.Append(Environment.NewLine);
		}

		var paragraphs = RandomGenerator.NextInteger(0, 3);
		for (var p = 0; p < paragraphs; p++)
		{
			if (p > 0)
			{
				buffer.Append(Environment.NewLine);
			}
			GenerateShortParagraph(buffer, 2, 5);
		}

		if (paragraphs > 0)
		{
			buffer.Append(Environment.NewLine);
		}

		if (RandomGenerator.NextInteger(100) < 55)
		{
			AppendRandomList(buffer);
		}

		if (RandomGenerator.NextInteger(100) < 45)
		{
			AppendRandomTable(buffer);
		}

		if (RandomGenerator.NextInteger(100) < 40)
		{
			AppendRandomCodeBlock(buffer);
		}

		if (RandomGenerator.NextInteger(100) < 35)
		{
			AppendRandomMath(buffer);
		}

		if (RandomGenerator.NextInteger(100) < 30)
		{
			AppendTaskList(buffer);
		}

		if (RandomGenerator.NextInteger(100) < 50)
		{
			AppendMixedInline(buffer);
		}

		// Occasional separator
		if (RandomGenerator.NextInteger(100) < 30)
		{
			buffer.Append("---");
			buffer.Append(Environment.NewLine);
		}

		_sectionCounter++;
	}

	public void Reset()
	{
		_initialHeaderEmitted = false;
	}

	private void AppendMixedInline(IStringBuffer sb)
	{
		sb.Append("**Bold**, *italic*, ***bold italic***, ~~strikethrough~~, `code`, ");
		sb.Append("[links](https://bobbycannon.com), and even `inline `**mixed** formatting.\n\n");
	}

	private void AppendRandomCodeBlock(IStringBuffer sb)
	{
		var lang = RandomGenerator.NextInteger(100) < 60 ? "csharp" : "json";
		sb.Append("```");
		sb.Append(lang);
		sb.Append(Environment.NewLine);

		if (lang == "csharp")
		{
			sb.Append("// Generated at section ");
			sb.Append(RandomGenerator.NextInteger(1000, 9999).ToString());
			sb.Append("\npublic void BenchmarkRender()\n{\n");
			sb.Append("    var view = new MarkdownView();\n");
			sb.Append("    // Keep streaming rich content...\n");
			sb.Append("}\n");
		}
		else
		{
			sb.Append("{\n");
			sb.Append("  \"section\": ");
			sb.Append(RandomGenerator.NextInteger(100).ToString());
			sb.Append(",");
			sb.Append(Environment.NewLine);
			sb.Append("  \"timestamp\": \"");
			sb.Append(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
			sb.Append("\",");
			sb.Append(Environment.NewLine);
			sb.Append("  \"topic\": \"");
			sb.Append(_topics[RandomGenerator.NextInteger(_topics.Length)]);
			sb.Append(Environment.NewLine);
			sb.Append("}");
			sb.Append(Environment.NewLine);
		}
		sb.Append("```");
		sb.Append(Environment.NewLine);
		sb.Append(Environment.NewLine);
	}

	private void AppendRandomList(IStringBuffer sb)
	{
		sb.Append("#### Random Capabilities");
		sb.Append(Environment.NewLine);
		sb.Append(Environment.NewLine);

		var ordered = RandomGenerator.NextInteger(100) < 35;
		var count = RandomGenerator.NextInteger(4, 9);

		for (var i = 1; i <= count; i++)
		{
			if (ordered)
			{
				sb.Append(i.ToString());
				sb.Append(". ");
			}
			else
			{
				sb.Append("- ");
			}
			RandomGenerator.LoremIpsum(sb, 6, 12);
			sb.Append(Environment.NewLine);
		}
		sb.Append(Environment.NewLine);
	}

	private void AppendRandomMath(IStringBuffer sb)
	{
		sb.Append("Inline math: $a^2 + b^2 = c^2$\n\n");
		sb.Append("Block math:\n$$\n");
		sb.Append("\\frac{n(n+1)}{2} = \\sum_{k=1}^{n} k\n");
		sb.Append("$$\n\n");
	}

	private void AppendRandomTable(IStringBuffer sb)
	{
		sb.Append("| Component | Status | Stress Level | Notes |\n");
		sb.Append("|-----------|--------|--------------|-------|\n");

		string[] items = ["Headings", "Tables", "MathJax", "Code Blocks", "Lists", "Inline Styles", "Streaming"];

		for (var i = 0; i < 5; i++)
		{
			var status = RandomGenerator.NextInteger(100) < 75 ? "✅ Passing" : "⚠️ Heavy";
			var stressLevel = RandomGenerator.NextInteger(2, 6);
			var stress = new string('♥', stressLevel);

			sb.Append("| ");
			sb.Append(items[RandomGenerator.NextInteger(items.Length)]);
			sb.Append(" | ");
			sb.Append(status);
			sb.Append(" | ");
			sb.Append(stress);
			sb.Append(" | ");
			sb.Append(_topics[RandomGenerator.NextInteger(_topics.Length)]);
			sb.Append(" |\n");
		}
		sb.Append(Environment.NewLine);
	}

	private void AppendTaskList(IStringBuffer sb)
	{
		sb.Append("- [x] Headings and basic formatting\n");
		sb.Append("- [x] Live streaming support\n");
		sb.Append("- [ ] Table rendering stress (iteration ");
		sb.Append(RandomGenerator.NextInteger(50, 500).ToString());
		sb.Append(")\n");
		sb.Append("- [x] Code block parsing\n");
		sb.Append("- [ ] Math rendering stability\n\n");
	}

	/// <summary>
	/// Much shorter paragraphs to reduce wall-of-text effect
	/// </summary>
	private void GenerateShortParagraph(IStringBuffer buffer, int minSentences, int maxSentences)
	{
		var count = RandomGenerator.NextInteger(minSentences, maxSentences + 1);

		for (var i = 0; i < count; i++)
		{
			if (i > 0)
			{
				buffer.Append(" ");
			}

			RandomGenerator.LoremIpsum(buffer, 8, 18); // shorter sentences too
		}
		buffer.Append(Environment.NewLine);
	}

	#endregion
}