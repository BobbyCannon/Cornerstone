#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Storage.Sql;

public class PredicateToSqlVisitor : ExpressionVisitor
{
	#region Fields

	private static readonly MethodInfo DateTimeEqualMethod;
	private static readonly MethodInfo DateTimeNotEqualMethod;
	private static readonly MethodInfo DateTimeOffsetEqualMethod;
	private static readonly MethodInfo DateTimeOffsetNotEqualMethod;
	private static readonly MethodInfo DecimalEqualMethod;
	private static readonly MethodInfo DecimalNotEqualMethod;
	private static readonly MethodInfo GuidEqualMethod;
	private static readonly MethodInfo GuidNotEqualMethod;

	private readonly string _close;
	private readonly string _open;
	private int _paramCounter;
	private ParameterExpression _parameter;
	private readonly int _parameterIndexStart;
	private readonly List<object> _parameters;
	private readonly SqlProvider _provider;
	private SourceTypeInfo _sourceType;
	private readonly StringBuilder _sql;

	#endregion

	#region Constructors

	public PredicateToSqlVisitor(SqlProvider provider)
		: this(provider, 0)
	{
	}

	public PredicateToSqlVisitor(SqlProvider provider, int parameterIndexStart)
	{
		var brackets = SqlGenerator.GetIdentifierBrackets(provider);
		_open = brackets.Open;
		_close = brackets.Close;
		_parameterIndexStart = parameterIndexStart;
		_paramCounter = parameterIndexStart;
		_parameters = [];
		_provider = provider;
		_sourceType = null;
		_sql = new();
	}

	static PredicateToSqlVisitor()
	{
		DateTimeEqualMethod = typeof(DateTime).GetMethod("op_Equality", BindingFlags.Static | BindingFlags.Public);
		DateTimeNotEqualMethod = typeof(DateTime).GetMethod("op_Inequality", BindingFlags.Static | BindingFlags.Public);
		DateTimeOffsetEqualMethod = typeof(DateTimeOffset).GetMethod("op_Equality", BindingFlags.Static | BindingFlags.Public);
		DateTimeOffsetNotEqualMethod = typeof(DateTimeOffset).GetMethod("op_Inequality", BindingFlags.Static | BindingFlags.Public);
		DecimalEqualMethod = typeof(decimal).GetMethod("op_Equality", BindingFlags.Static | BindingFlags.Public);
		DecimalNotEqualMethod = typeof(decimal).GetMethod("op_Inequality", BindingFlags.Static | BindingFlags.Public);
		GuidEqualMethod = typeof(Guid).GetMethod("op_Equality", BindingFlags.Static | BindingFlags.Public);
		GuidNotEqualMethod = typeof(Guid).GetMethod("op_Inequality", BindingFlags.Static | BindingFlags.Public);
	}

	#endregion

	#region Methods

	[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Predicate lambda parameter type is a [SourceReflection] entity supplied by SqlQuery / SqlRepository.")]
	public (string Sql, object[] Parameters) Translate(LambdaExpression expression)
	{
		if (expression.Parameters.Count != 1)
		{
			throw new ArgumentException("Expected a lambda expression with exactly one parameter (e.g. x => ...)", nameof(expression));
		}

		_parameter = expression.Parameters[0];
		_sourceType = SourceReflector.GetRequiredSourceType(_parameter.Type);
		_paramCounter = _parameterIndexStart;
		_parameters.Clear();
		_sql.Clear();

		Visit(expression.Body);

		return (_sql.ToString(), _parameters.ToArray());
	}

	protected override Expression VisitBinary(BinaryExpression node)
	{
		if (TryTranslateCompareTo(node))
		{
			return node;
		}

		// Special null checks (IS NULL / IS NOT NULL)
		if (node.NodeType is ExpressionType.Equal or ExpressionType.NotEqual)
		{
			var left = node.Left;
			var right = node.Right;

			var isNullCheck =
				(IsMemberOfParameter(left) && IsNullConstantOrValue(right)) ||
				(IsMemberOfParameter(right) && IsNullConstantOrValue(left));

			if (isNullCheck)
			{
				var column = IsMemberOfParameter(left) ? left : right;
				_sql.Append('(');
				Visit(column);
				_sql.Append(node.NodeType == ExpressionType.Equal ? " IS NULL" : " IS NOT NULL");
				_sql.Append(')');
				return node;
			}

			// Boolean member == true/false handling
			if (IsBooleanMember(node.Left)
				&& IsBoolConstant(node.Right, out var isTrue))
			{
				if (node.NodeType == ExpressionType.NotEqual)
				{
					isTrue = !isTrue;
				}
				_sql.Append('(');
				AppendIdentifier(((MemberExpression) node.Left).Member.Name);
				_sql.Append(" = ");
				_sql.Append(isTrue ? 1 : 0);
				_sql.Append(')');
				return node;
			}
			if (IsBooleanMember(node.Right) && IsBoolConstant(node.Left, out isTrue))
			{
				if (node.NodeType == ExpressionType.NotEqual)
				{
					isTrue = !isTrue;
				}
				_sql.Append('(');
				AppendIdentifier(((MemberExpression) node.Right).Member.Name);
				_sql.Append(" = ");
				_sql.Append(isTrue ? 1 : 0);
				_sql.Append(')');
				return node;
			}
		}

		// === AOT-SAFE COMPARISON HANDLING ===
		if (node.NodeType is ExpressionType.Equal or ExpressionType.NotEqual or
			ExpressionType.GreaterThan or ExpressionType.GreaterThanOrEqual or
			ExpressionType.LessThan or ExpressionType.LessThanOrEqual)
		{
			MethodInfo method = null;

			var leftType = node.Left.Type;
			var rightType = node.Right.Type;

			// Handle common problematic types
			if ((leftType == typeof(decimal)) && (rightType == typeof(decimal)))
			{
				method = node.NodeType == ExpressionType.Equal ? DecimalEqualMethod : DecimalNotEqualMethod;
			}
			else if ((leftType == typeof(DateTime)) && (rightType == typeof(DateTime)))
			{
				method = node.NodeType == ExpressionType.Equal ? DateTimeEqualMethod : DateTimeNotEqualMethod;
			}
			else if ((leftType == typeof(DateTimeOffset)) && (rightType == typeof(DateTimeOffset)))
			{
				method = node.NodeType == ExpressionType.Equal ? DateTimeOffsetEqualMethod : DateTimeOffsetNotEqualMethod;
			}
			else if ((leftType == typeof(Guid)) && (rightType == typeof(Guid)))
			{
				method = node.NodeType == ExpressionType.Equal ? GuidEqualMethod : GuidNotEqualMethod;
			}

			_sql.Append('(');
			Visit(node.Left);

			_sql.Append(GetOperator(node.NodeType));

			if (method != null)
			{
				// Use the explicit operator method (safe under AOT)
				var binaryWithMethod = Expression.MakeBinary(node.NodeType, node.Left, node.Right, false, method);
				Visit(binaryWithMethod.Right);
			}
			else
			{
				// Normal path for types that usually survive trimming (int, string, bool, etc.)
				Visit(node.Right);
			}

			_sql.Append(')');
			return node;
		}

		// Fallback for AndAlso / OrElse etc.
		_sql.Append('(');
		Visit(node.Left);
		_sql.Append(GetOperator(node.NodeType));
		Visit(node.Right);
		_sql.Append(')');

		return node;
	}

	protected override Expression VisitConstant(ConstantExpression node)
	{
		if (node.Value is bool boolean)
		{
			// SQL Server WHERE cannot use a BIT parameter as a condition.
			_sql.Append(boolean ? "(1 = 1)" : "(1 = 0)");
			return node;
		}

		_parameters.Add(node.Value ?? DBNull.Value);
		_sql.Append("@p").Append(_paramCounter++);
		return node;
	}

	protected override Expression VisitMember(MemberExpression node)
	{
		// Handle string.Length (or other length-like properties)
		if (IsMemberOfParameter(node)
			&& (node.Member.Name == nameof(string.Length))
			&& (node.Member.DeclaringType == typeof(string)))
		{
			_sql.Append(_provider == SqlProvider.Sqlite ? "LENGTH(" : "LEN(");
			Visit(node.Expression);
			_sql.Append(')');
			return node;
		}

		if (IsMemberOfParameter(node) && (node.Type == typeof(bool)))
		{
			_sql.Append('(');
			AppendIdentifier(node.Member.Name);
			_sql.Append(" = 1)");
			return node;
		}

		if (IsMemberOfParameter(node))
		{
			AppendIdentifier(node.Member.Name);
			return node;
		}

		var value = EvaluateExpression(node);
		_parameters.Add(value ?? DBNull.Value);
		_sql.Append($"@p{_paramCounter++}");
		return node;
	}

	protected override Expression VisitMethodCall(MethodCallExpression node)
	{
		if (TryTranslateIn(node))
		{
			return node;
		}

		var instance = node.Object;
		var search = node.Arguments.Count > 0 ? node.Arguments[0] : null;
		if ((instance == null) && (node.Arguments.Count >= 2))
		{
			instance = node.Arguments[0];
			search = node.Arguments[1];
		}

		if ((node.Method.DeclaringType == typeof(string))
			|| node.Method.Name is "Contains" or "StartsWith" or "EndsWith" or "ToLower" or "ToUpper")
		{
			string pattern = null;

			if ((node.Method.Name == "Contains") && (search != null))
			{
				pattern = "%" + EscapeLikeValue(EvaluateExpression(search)) + "%";
			}
			else if ((node.Method.Name == "StartsWith") && (search != null))
			{
				pattern = EscapeLikeValue(EvaluateExpression(search)) + "%";
			}
			else if ((node.Method.Name == "EndsWith") && (search != null))
			{
				pattern = "%" + EscapeLikeValue(EvaluateExpression(search));
			}
			else if ((node.Method.Name == "IsNullOrEmpty") && (node.Arguments.Count == 1))
			{
				var arg = node.Arguments[0];
				_sql.Append('(');
				Visit(arg);
				_sql.Append(" IS NULL OR ");
				Visit(arg);
				_sql.Append(" = '')");
				return node;
			}
			else if ((node.Method.Name == "IsNullOrWhiteSpace") && (node.Arguments.Count == 1))
			{
				var arg = node.Arguments[0];
				_sql.Append('(');
				Visit(arg);
				_sql.Append(" IS NULL OR LTRIM(RTRIM(");
				Visit(arg);
				_sql.Append(")) = '')");
				return node;
			}

			if (pattern is not null)
			{
				_sql.Append('(');
				Visit(instance);
				_sql.Append(" LIKE ");
				_parameters.Add(pattern);
				_sql.Append("@p").Append(_paramCounter++);
				_sql.Append(" ESCAPE '\\')");
				return node;
			}

			if (node.Method.Name is "ToLower" or "ToUpper")
			{
				_sql.Append(node.Method.Name == "ToLower" ? "LOWER(" : "UPPER(");
				Visit(instance ?? node.Object);
				_sql.Append(')');
				return node;
			}
		}

		throw new NotSupportedException($"Method call not supported: {node.Method.DeclaringType?.Name}.{node.Method.Name}");
	}

	protected override Expression VisitNew(NewExpression node)
	{
		var value = EvaluateExpression(node);
		_parameters.Add(value ?? DBNull.Value);
		_sql.Append($"@p{_paramCounter++}");
		return node;
	}

	protected override Expression VisitUnary(UnaryExpression node)
	{
		if (node.NodeType == ExpressionType.Not)
		{
			// 1. Double negation: !(!bool) == 1
			if (node.Operand is UnaryExpression { NodeType: ExpressionType.Not, Operand: MemberExpression m1 }
				&& IsMemberOfParameter(m1) && (m1.Type == typeof(bool)))
			{
				_sql.Append('(');
				AppendIdentifier(m1.Member.Name);
				_sql.Append(" = 1)");
				return node;
			}

			// 2. Boolean negation: !p.IsDeleted == 0
			if (node.Operand is MemberExpression m2
				&& IsMemberOfParameter(m2)
				&& (m2.Type == typeof(bool)))
			{
				_sql.Append('(');
				AppendIdentifier(m2.Member.Name);
				_sql.Append(" = 0)");
				return node;
			}

			// 3. Negate comparison: !(x > y) → (x <= y), !(x == y) → (x <> y), etc.
			if (node.Operand is BinaryExpression binary)
			{
				var negatedOp = binary.NodeType switch
				{
					ExpressionType.Equal => " <> ",
					ExpressionType.NotEqual => " = ",
					ExpressionType.GreaterThan => " <= ",
					ExpressionType.GreaterThanOrEqual => " < ",
					ExpressionType.LessThan => " >= ",
					ExpressionType.LessThanOrEqual => " > ",
					_ => null
				};

				if (negatedOp != null)
				{
					_sql.Append('(');
					Visit(binary.Left);
					_sql.Append(negatedOp);
					Visit(binary.Right);
					_sql.Append(')');
					return node;
				}
			}

			// 4. General fallback
			_sql.Append("(NOT ");
			Visit(node.Operand);
			_sql.Append(")");
			return node;
		}

		return base.VisitUnary(node);
	}

	private void AppendIdentifier(string name)
	{
		_sql.Append(_open);
		_sql.Append(SqlGenerator.GetColumnName(_sourceType, name));
		_sql.Append(_close);
	}

	private static string EscapeLikeValue(object value)
	{
		var text = value?.ToString() ?? string.Empty;
		return text
			.Replace("\\", "\\\\", StringComparison.Ordinal)
			.Replace("%", "\\%", StringComparison.Ordinal)
			.Replace("_", "\\_", StringComparison.Ordinal);
	}

	private object EvaluateExpression(Expression expr)
	{
		return expr switch
		{
			ConstantExpression c => c.Value,
			UnaryExpression { NodeType: ExpressionType.Convert } u => EvaluateExpression(u.Operand),
			MemberExpression m => EvaluateMember(m),
			NewExpression n => EvaluateNew(n),
			NewArrayExpression n => EvaluateNewArray(n),
			MethodCallExpression mc => EvaluateMethodCall(mc),
			_ => FallbackCompile(expr)
		};
	}

	private object EvaluateMember(MemberExpression m)
	{
		var target = m.Expression is not null ? EvaluateExpression(m.Expression) : null;
		return m.Member switch
		{
			FieldInfo f => f.GetValue(target),
			PropertyInfo p => p.GetValue(target),
			_ => FallbackCompile(m)
		};
	}

	private object EvaluateMethodCall(MethodCallExpression mc)
	{
		if (mc.Method.Name is "AsSpan" or "AsMemory" || IsSpanType(mc.Method.ReturnType))
		{
			return EvaluateExpression(mc.Object ?? mc.Arguments[0]);
		}

		var target = mc.Object is not null ? EvaluateExpression(mc.Object) : null;
		var args = new object[mc.Arguments.Count];
		for (var i = 0; i < args.Length; i++)
		{
			args[i] = EvaluateExpression(mc.Arguments[i]);
		}
		return mc.Method.Invoke(target, args);
	}

	private object EvaluateNew(NewExpression n)
	{
		if (IsSpanType(n.Type) && (n.Arguments.Count > 0))
		{
			return EvaluateExpression(n.Arguments[0]);
		}

		var args = new object[n.Arguments.Count];
		for (var i = 0; i < args.Length; i++)
		{
			args[i] = EvaluateExpression(n.Arguments[i]);
		}
		return n.Constructor?.Invoke(args);
	}

	private object EvaluateNewArray(NewArrayExpression n)
	{
		var items = new List<object>(n.Expressions.Count);
		for (var i = 0; i < n.Expressions.Count; i++)
		{
			items.Add(EvaluateExpression(n.Expressions[i]));
		}

		return items;
	}

	private static object FallbackCompile(Expression expr)
	{
		throw new NotSupportedException(
			$"Cannot evaluate expression of type {expr.NodeType} without Expression.Compile, which is not supported on Native AOT."
		);
	}

	private string GetOperator(ExpressionType type)
	{
		return type switch
		{
			ExpressionType.Equal => " = ",
			ExpressionType.NotEqual => " <> ",
			ExpressionType.GreaterThan => " > ",
			ExpressionType.GreaterThanOrEqual => " >= ",
			ExpressionType.LessThan => " < ",
			ExpressionType.LessThanOrEqual => " <= ",
			ExpressionType.AndAlso => " AND ",
			ExpressionType.OrElse => " OR ",
			_ => throw new NotSupportedException($"Operator {type} not supported")
		};
	}

	private bool IsBoolConstant(Expression expr, out bool value)
	{
		value = false;
		if (expr is ConstantExpression c && c.Value is bool b)
		{
			value = b;
			return true;
		}
		return false;
	}

	private bool IsBooleanMember(Expression expr)
	{
		return expr is MemberExpression m &&
			IsMemberOfParameter(m) &&
			(m.Type == typeof(bool));
	}

	private bool IsMemberOfParameter(Expression node)
	{
		var current = node;

		while (current is MemberExpression member)
		{
			current = member.Expression;
		}

		return current is ParameterExpression parameter && (parameter == _parameter);
	}

	private bool IsNullConstantOrValue(Expression expr)
	{
		if (expr is ConstantExpression c)
		{
			return c.Value is null;
		}

		if (expr is DefaultExpression)
		{
			return !expr.Type.IsValueType;
		}

		// Only attempt evaluation for expressions we know we can resolve
		if (expr is MemberExpression or MethodCallExpression or UnaryExpression { NodeType: ExpressionType.Convert })
		{
			try
			{
				var value = EvaluateExpression(expr);
				return value is null;
			}
			catch
			{
				return false;
			}
		}

		return false;
	}

	private static bool IsSpanType(Type type)
	{
		return (type == typeof(Span<>))
			|| (type == typeof(ReadOnlySpan<>))
			|| (type.IsGenericType
				&& ((type.GetGenericTypeDefinition() == typeof(Span<>))
					|| (type.GetGenericTypeDefinition() == typeof(ReadOnlySpan<>))));
	}

	private bool TryTranslateCompareTo(BinaryExpression node)
	{
		if (node.NodeType is not (ExpressionType.Equal or ExpressionType.NotEqual
			or ExpressionType.GreaterThan or ExpressionType.GreaterThanOrEqual
			or ExpressionType.LessThan or ExpressionType.LessThanOrEqual))
		{
			return false;
		}

		if (node.Left is not MethodCallExpression call
			|| (call.Method.Name != nameof(IComparable.CompareTo))
			|| (call.Arguments.Count != 1)
			|| (call.Object == null))
		{
			return false;
		}

		if (node.Right is not ConstantExpression right
			|| right.Value is not int compared
			|| (compared != 0))
		{
			return false;
		}

		_sql.Append('(');
		Visit(call.Object);
		_sql.Append(GetOperator(node.NodeType));
		Visit(call.Arguments[0]);
		_sql.Append(')');
		return true;
	}

	private bool TryTranslateIn(MethodCallExpression node)
	{
		if (node.Method.Name != "Contains")
		{
			return false;
		}

		if (node.Method.DeclaringType == typeof(string))
		{
			return false;
		}

		Expression collection;
		Expression item;
		if (node.Object != null)
		{
			if (node.Arguments.Count < 1)
			{
				return false;
			}

			collection = node.Object;
			item = node.Arguments[0];
		}
		else if (node.Arguments.Count >= 2)
		{
			collection = node.Arguments[0];
			item = node.Arguments[1];
		}
		else
		{
			return false;
		}

		if (!IsMemberOfParameter(item) || IsMemberOfParameter(collection))
		{
			return false;
		}

		if (collection.Type == typeof(string))
		{
			return false;
		}

		var values = EvaluateExpression(collection);
		if (values is string || values is not IEnumerable enumerable)
		{
			return false;
		}

		var items = new List<object>();
		foreach (var value in enumerable)
		{
			items.Add(value);
		}

		if (items.Count == 0)
		{
			_sql.Append("(1 = 0)");
			return true;
		}

		_sql.Append('(');
		Visit(item);
		_sql.Append(" IN (");
		for (var i = 0; i < items.Count; i++)
		{
			if (i > 0)
			{
				_sql.Append(", ");
			}

			_parameters.Add(items[i] ?? DBNull.Value);
			_sql.Append("@p").Append(_paramCounter++);
		}

		_sql.Append("))");
		return true;
	}

	#endregion
}