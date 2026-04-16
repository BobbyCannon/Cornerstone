#region References

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

#endregion

namespace Cornerstone.Avalonia.NumberBox;

internal static class NumberBoxParser
{
	#region Constants

	public const string NumberBoxOperators = "+-*/^";

	#endregion

	#region Methods

	public static double? Compute(string expr)
	{
		var tokens = GetTokens(expr.AsSpan());
		if (tokens is { Count: > 0 })
		{
			// Rearrange to postfix notation
			var postfixTokens = ConvertInfixToPostfix(tokens);
			if (postfixTokens?.Count > 0)
			{
				// Compute expression
				return ComputePostfixExpression(postfixTokens);
			}
		}

		return null;
	}

	public static double? ComputePostfixExpression(IList<MathToken> tokens)
	{
		var stack = new Stack<double?>();

		foreach (var token in tokens)
		{
			if (token.Type == MathTokenType.Operator)
			{
				// There has to be at least two values on the stack to apply
				if (stack.Count < 2)
				{
					return null;
				}

				var op1 = stack.Pop().Value;
				var op2 = stack.Pop().Value;

				double? result = 0;

				switch (token.Char)
				{
					case '-':
						result = op2 - op1;
						break;

					case '+':
						result = op1 + op2;
						break;

					case '*':
						result = op1 * op2;
						break;

					case '/':
						if (op1 == 0)
						{
							return double.NaN;
						}
						result = op2 / op1;
						break;

					case '^':
						result = Math.Pow(op2, op1);
						break;

					default:
						result = null;
						break;
				}
				stack.Push(result);
			}
			else if (token.Type == MathTokenType.Numeric)
			{
				stack.Push(token.Value);
			}
		}

		if (stack.Count != 1)
		{
			return null;
		}

		return stack.Pop();
	}

	// Converts a list of tokens from infix format (e.g. "3 + 5") to postfix (e.g. "3 5 +")
	public static IList<MathToken> ConvertInfixToPostfix(IList<MathToken> infixTokens)
	{
		var postFixTokens = new List<MathToken>();
		var operatorTokens = new Stack<MathToken>();

		foreach (var token in infixTokens)
		{
			if (token.Type == MathTokenType.Numeric)
			{
				postFixTokens.Add(token);
			}
			else if (token.Type == MathTokenType.Operator)
			{
				while (operatorTokens.Count != 0)
				{
					var top = operatorTokens.Peek();
					if ((top.Type != MathTokenType.Parenthesis) && (GetPrecedenceValue(top.Char) >= GetPrecedenceValue(token.Char)))
					{
						postFixTokens.Add(top);
						operatorTokens.Pop();
					}
					else
					{
						break;
					}
				}
				operatorTokens.Push(token);
			}
			else if (token.Type == MathTokenType.Parenthesis)
			{
				if (token.Char == '(')
				{
					operatorTokens.Push(token);
				}
				else
				{
					while ((operatorTokens.Count != 0) && (operatorTokens.Peek().Char != '('))
					{
						// Pop operators onto output until we reach a left paren
						postFixTokens.Add(operatorTokens.Peek());
						operatorTokens.Pop();
					}

					if (operatorTokens.Count == 0)
					{
						// Broken parenthesis
						return null;
					}

					// Pop left paren and discard
					operatorTokens.Pop();
				}
			}
		}

		// Pop all remaining operators
		while (operatorTokens.Count != 0)
		{
			if (operatorTokens.Peek().Type == MathTokenType.Parenthesis)
			{
				// Broken parenthesis
				return null;
			}

			postFixTokens.Add(operatorTokens.Pop());
		}

		return postFixTokens;
	}

	public static (double value, int charLen) GetNextNumber(ReadOnlySpan<char> inputSpan)
	{
		//TODO: Remove Regex impl (even tho this was copied from WinUI)
		var input = inputSpan.ToString();
		var rg = new Regex("^-?([^-+/*\\(\\)\\^\\s]+)");
		var match = rg.Match(input);
		if (match.Success)
		{
			// Might be a number
			var matchLength = match.Value.Length;

			//var parsedNum = parser.ParseDouble(input.Substring(0, matchLength));
			if (double.TryParse(input.Substring(0, matchLength), NumberStyles.Any, CultureInfo.CurrentCulture, out var result))
			{
				return (result, matchLength);
			}
		}

		return (double.NaN, 0);
	}

	public static int GetPrecedenceValue(char c)
	{
		if ((c == '*') || (c == '/'))
		{
			return 1;
		}
		if (c == '^')
		{
			return 2;
		}

		return 0;
	}

	public static IList<MathToken> GetTokens(ReadOnlySpan<char> input)
	{
		var tokens = new List<MathToken>();

		var expectNumber = true;
		while (input.Length > 0)
		{
			var nextChar = input[0];
			if (nextChar != ' ')
			{
				if (expectNumber)
				{
					if (nextChar == '(')
					{
						tokens.Add(new MathToken(MathTokenType.Parenthesis, nextChar));
					}
					else
					{
						var (value, charLen) = GetNextNumber(input);

						if (charLen > 0)
						{
							tokens.Add(new MathToken(MathTokenType.Numeric, value));
							input = input.Slice(charLen - 1);
							expectNumber = false;
						}
						else
						{
							// Error case -- next token is not a number
							return null;
						}
					}
				}
				else
				{
					if (NumberBoxOperators.IndexOf(nextChar) != -1)
					{
						tokens.Add(new MathToken(MathTokenType.Operator, nextChar));
						expectNumber = true;
					}
					else if (nextChar == ')')
					{
						// Closed parens are also acceptable, but don't change the next expected token type.
						tokens.Add(new MathToken(MathTokenType.Parenthesis, nextChar));
					}
					else
					{
						// Error case -- could not evaluate part of the expression
						return null;
					}
				}
			}
			input = input.Slice(1);
		}

		return tokens;
	}

	#endregion
}

internal enum MathTokenType
{
	Numeric,
	Operator,
	Parenthesis
}

internal struct MathToken
{
	#region Constructors

	public MathToken(MathTokenType t, char c)
	{
		Type = t;
		Char = c;
		Value = double.NaN;
	}

	public MathToken(MathTokenType t, double d)
	{
		Type = t;
		Value = d;
		Char = '\0';
	}

	#endregion

	#region Properties

	public char Char { get; }

	public MathTokenType Type { get; }

	public double Value { get; }

	#endregion
}