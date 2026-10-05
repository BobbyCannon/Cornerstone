#region References

using System;
using System.Collections.Generic;
using Cornerstone.Collections;
using Cornerstone.Text.Parsing;
using Cornerstone.Presentation;
using Cornerstone.Profiling;
using Cornerstone.Reflection;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Presentation.Controls.Text.Models;

[SourceReflection]
public class TokenManager : SpeedyListViewManager<Token>, IQueue<Token>
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

	public Tokenizer Tokenizer => _tokenizer;

	public int TokenRebuildIndex { get; private set; }

	#endregion

	#region Methods

	public void Add(int type, int startOffset, int endOffset)
	{
		Add(_tokenizer.CreateOrUpdateSection(type, startOffset, endOffset));
	}

	/// <summary>
	/// Shift token ranges at or after <paramref name="fromOffset" /> by <paramref name="delta" />.
	/// Used when host output is inserted before a live prompt (no tokenizer rebuild).
	/// </summary>
	public void ShiftOffsets(int fromOffset, int delta)
	{
		if (delta == 0)
		{
			return;
		}

		for (var i = 0; i < Count; i++)
		{
			var token = this[i];
			if (token.StartOffset >= fromOffset)
			{
				token.StartOffset += delta;
				token.EndOffset += delta;
			}
			else if (token.EndOffset > fromOffset)
			{
				token.EndOffset += delta;
			}
		}
	}

	public override void Clear()
	{
		_pool.Enqueue(List.ToArray());
		base.Clear();
	}

	public Token GetTokenForOffset(int offset)
	{
		var index = GetTokenIndexForOffset(offset);
		if (index < 0)
		{
			return null;
		}

		var token = this[index];
		return token.Contains(offset) ? token : null;
	}

	/// <summary>
	/// Index of the first token whose end is past <paramref name="offset" />
	/// (contains offset, or the next token after a gap). -1 if none.
	/// Does not invent style by returning the previous token for unstyled offsets.
	/// </summary>
	public int GetTokenIndexForOffset(int offset)
	{
		if (Count == 0)
		{
			return -1;
		}

		var left = 0;
		var right = Count - 1;
		var result = -1;

		while (left <= right)
		{
			var mid = left + ((right - left) >> 1);
			if (this[mid].EndOffset > offset)
			{
				result = mid;
				right = mid - 1;
			}
			else
			{
				left = mid + 1;
			}
		}

		return result;
	}

	public IEnumerable<Token> GetTokens(int startOffset, int endOffset)
	{
		foreach (var token in GetOverlappingTokens(startOffset, endOffset))
		{
			yield return token;
		}
	}

	/// <summary>
	/// Tokens that overlap [startOffset, endOffset) without allocating an enumerator.
	/// </summary>
	public OverlappingTokens GetOverlappingTokens(int startOffset, int endOffset)
	{
		return new OverlappingTokens(this, startOffset, endOffset);
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
		NotifyComputedPropertyChanged(nameof(HasTokenizer));
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

		try
		{
			while (_tokenizer.NextSection() is { } token)
			{
				if (TokenRebuildIndex++ < Count)
				{
					// An existing token was updated so just continue
					continue;
				}

				Add(token);
			}
		}
		catch
		{
			// NextSection already skips a character on tokenizer faults.
			// If something still escapes, drop unprocessed tokens rather than crash.
		}

		while (TokenRebuildIndex < Count)
		{
			// Pool the remaining tokens.
			var tokenToPool = this[Count - 1];
			RemoveAt(Count - 1);
			_pool.Enqueue(tokenToPool);
		}

		TokenRebuildIndex = -1;

		NotifyComputedPropertyChanged(nameof(Count));
	}

	public bool TryPeek(out Token value)
	{
		return _pool.TryPeek(out value);
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

	#region Classes

	public readonly struct OverlappingTokens
	{
		private readonly TokenManager _tokens;
		private readonly int _endOffset;
		private readonly int _startOffset;

		internal OverlappingTokens(TokenManager tokens, int startOffset, int endOffset)
		{
			_tokens = tokens;
			_startOffset = startOffset;
			_endOffset = endOffset;
		}

		public Enumerator GetEnumerator()
		{
			return new Enumerator(_tokens, _startOffset, _endOffset);
		}

		public struct Enumerator
		{
			private readonly int _endOffset;
			private readonly int _startOffset;
			private readonly TokenManager _tokens;
			private int _index;

			internal Enumerator(TokenManager tokens, int startOffset, int endOffset)
			{
				_tokens = tokens;
				_startOffset = startOffset;
				_endOffset = endOffset;
				_index = tokens == null ? -1 : tokens.GetTokenIndexForOffset(startOffset);
				Current = null;
			}

			public Token Current { get; private set; }

			public bool MoveNext()
			{
				if ((_tokens == null) || (_index < 0))
				{
					return false;
				}

				var count = _tokens.Count;
				while (_index < count)
				{
					var token = _tokens[_index++];
					if (token.StartOffset >= _endOffset)
					{
						return false;
					}

					if (token.EndOffset > _startOffset)
					{
						Current = token;
						return true;
					}
				}

				return false;
			}
		}
	}

	#endregion
}