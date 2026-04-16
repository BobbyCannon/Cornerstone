#region References

using System;
using System.Collections.Generic;
using Cornerstone.Collections;
using Cornerstone.Parsers;
using Cornerstone.Presentation;
using Cornerstone.Profiling;
using Cornerstone.Reflection;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Avalonia.Text.Models;

[SourceReflection]
public class TokenManager : ViewManager<Token>, IQueue<Token>
{
	#region Fields

	private readonly IQueue<Token> _pool;
	private Tokenizer _tokenizer;
	private readonly TextEditorViewModel _viewModel;

	#endregion

	#region Constructors

	public TokenManager(TextEditorViewModel viewModel)
	{
		_viewModel = viewModel;
		_pool = new SpeedyQueue<Token>(65536);
	}

	#endregion

	#region Properties

	public bool HasTokenizer => _tokenizer != null;

	public int TokenRebuildIndex { get; private set; }

	#endregion

	#region Methods

	public void Add(int type, int startOffset, int endOffset)
	{
		Add(_tokenizer.CreateOrUpdateSection(type, startOffset, endOffset));
	}

	public override void Clear()
	{
		_pool.Enqueue(AsSpan());
		base.Clear();
	}

	public Token GetTokenForOffset(int offset)
	{
		if (Count == 0)
		{
			return null;
		}

		if (Count == 1)
		{
			return this[0];
		}

		// Fast path for very likely case: offset at or beyond end
		if (offset >= this[^1].EndOffset)
		{
			return this[^1];
		}

		var left = 0;
		var right = Count - 1;

		while (left <= right)
		{
			var mid = left + ((right - left) >> 1);
			var token = this[mid];

			if (token.Contains(offset))
			{
				return token;
			}

			if (offset < token.StartOffset)
			{
				right = mid - 1;
			}
			else
			{
				left = mid + 1;
			}
		}

		// If we get here, offset lies between lines or after last line
		// Because we already checked the after-last-line case, this means:
		// between line[right] and line[right+1]
		return this[right];
	}

	public int GetTokenIndexForOffset(int offset)
	{
		if (Count == 0)
		{
			return -1;
		}

		if (Count == 1)
		{
			return 0;
		}

		// Fast path for very likely case: offset at or beyond end
		if (offset >= this[^1].EndOffset)
		{
			return Count - 1;
		}

		var left = 0;
		var right = Count - 1;

		while (left <= right)
		{
			var mid = left + ((right - left) >> 1);
			var token = this[mid];

			if (token.Contains(offset))
			{
				return mid;
			}

			if (offset < token.StartOffset)
			{
				right = mid - 1;
			}
			else
			{
				left = mid + 1;
			}
		}

		// If we get here, offset lies between lines or after last line
		// Because we already checked the after-last-line case, this means:
		// between line[right] and line[right+1]
		return right;
	}

	public IEnumerable<Token> GetTokens(int startOffset, int endOffset)
	{
		var tokenIndex = GetTokenIndexForOffset(startOffset);
		if (tokenIndex < 0)
		{
			yield break;
		}

		var range = new TextRange(startOffset, endOffset);

		while (tokenIndex < Count)
		{
			var token = this[tokenIndex++];
			if (range.Overlaps(token))
			{
				yield return token;
			}
		}
	}

	public void Initialize(string extension)
	{
		Initialize(Tokenizer.GetByExtension(extension, _viewModel.Buffer, this));
	}

	public void Initialize(Tokenizer tokenizer)
	{
		Clear();
		_tokenizer = tokenizer;
		Rebuild(new TextDocumentChangedArgs());
		OnPropertyChanged(nameof(HasTokenizer));
	}

	public void Rebuild(TextDocumentChangedArgs args)
	{
		using var _ = ProfilerExtensions.Start(_viewModel.Profiler, "TokenManager.Rebuild");
		var tokenizer = _tokenizer;
		if (tokenizer is not { SupportsRebuilding: true })
		{
			// Tokenizer is null or does not support rebuilding
			return;
		}

		_tokenizer.StartProcessing();

		TokenRebuildIndex = 0;

		while (_tokenizer.NextSection() is { } token)
		{
			if (TokenRebuildIndex++ < Count)
			{
				// An existing token was updated so just continue
				continue;
			}

			Add(token);
		}

		while (TokenRebuildIndex < Count)
		{
			// Pool the remaining tokens.
			var tokenToPool = this[Count - 1];
			RemoveAt(Count - 1);
			_pool.Enqueue(tokenToPool);
		}

		TokenRebuildIndex = -1;

		NotifyOfPropertyChanged(nameof(Count));
	}

	void IQueue<Token>.Enqueue(Token value)
	{
		_pool.Enqueue(value);
	}

	void IQueue<Token>.Enqueue(ReadOnlySpan<Token> values)
	{
		_pool.Enqueue(values);
	}

	bool IQueue<Token>.TryDequeue(out Token value)
	{
		if ((TokenRebuildIndex >= 0) && (TokenRebuildIndex < Count))
		{
			value = this[TokenRebuildIndex];
			return true;
		}

		return _pool.TryDequeue(out value);
	}

	#endregion
}