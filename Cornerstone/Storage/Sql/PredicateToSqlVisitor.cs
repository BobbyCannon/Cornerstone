#region References

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

#endregion

namespace Cornerstone.Storage.Sql;

public class PredicateToSqlVisitor : ExpressionVisitor
{
	#region Fields

	private int _paramCounter;
	private ParameterExpression _parameter;
	private readonly List<object> _parameters;
	private readonly StringBuilder _sql;

	private static readonly MethodInfo DateTimeEqualMethod;
	private static readonly MethodInfo DateTimeNotEqualMethod;
	private static readonly MethodInfo DecimalEqualMethod;
	private static readonly MethodInfo DecimalNotEqualMethod;

	#endregion

	#region Constructors

	public PredicateToSqlVisitor() : this(0)
	{
	}

	public PredicateToSqlVisitor(int parameterIndexStart)
	{
		_paramCounter = parameterIndexStart;
		_parameters = [];
		_sql = new();
	}

	static PredicateToSqlVisitor()
	{
		DateTimeEqualMethod = typeof(DateTime).GetMethod("op_Equality", BindingFlags.Static | BindingFlags.Public);
		DateTimeNotEqualMethod = typeof(DateTime).GetMethod("op_Inequality", BindingFlags.Static | BindingFlags.Public);
		DecimalEqualMethod = typeof(decimal).GetMethod("op_Equality", BindingFlags.Static | BindingFlags.Public);
		DecimalNotEqualMethod = typeof(decimal).GetMethod("op_Inequality", BindingFlags.Static | BindingFlags.Public);
	}

	#endregion

	#region Methods

	public (string Sql, object[] Parameters) Translate(LambdaExpression expression)
	{
		if (expression.Parameters.Count != 1)
		{
			throw new ArgumentException("Expected a lambda expression with exactly one parameter (e.g. x => ...)", nameof(expression));
		}

		_parameter = expression.Parameters[0];
		_paramCounter = 0;
		_parameters.Clear();
		_sql.Clear();

		Visit(expression.Body);

		return (_sql.ToString(), _parameters.ToArray());
	}

	protected override Expression VisitBinary(BinaryExpression node)
	{
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
				_sql.Append("([")
					.Append(((MemberExpression) node.Left).Member.Name)
					.Append("] = ")
					.Append(isTrue ? 1 : 0)
					.Append(')');
				return node;
			}
			if (IsBooleanMember(node.Right) && IsBoolConstant(node.Left, out isTrue))
			{
				if (node.NodeType == ExpressionType.NotEqual)
				{
					isTrue = !isTrue;
				}
				_sql.Append($"([{((MemberExpression) node.Right).Member.Name}] = {(isTrue ? 1 : 0)})");
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

			// Add more types here if you hit them (Guid, etc.)

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
			_sql.Append("LEN(");
			Visit(node.Expression); // This will append [Name]
			_sql.Append(')');
			return node;
		}

		if (IsMemberOfParameter(node) && (node.Type == typeof(bool)))
		{
			_sql.Append($"([{node.Member.Name}] = 1)");
			return node;
		}

		if (IsMemberOfParameter(node))
		{
			_sql.Append($"[{node.Member.Name}]");
			return node;
		}

		var value = EvaluateExpression(node);
		_parameters.Add(value ?? DBNull.Value);
		_sql.Append($"@p{_paramCounter++}");
		return node;
	}

	protected override Expression VisitMethodCall(MethodCallExpression node)
	{
		// Handle string methods: Contains, StartsWith, EndsWith
		if (node.Method.DeclaringType == typeof(string))
		{
			string pattern = null;

			if ((node.Method.Name == "Contains") && (node.Arguments.Count == 1))
			{
				pattern = $"%{EvaluateExpression(node.Arguments[0])}%";
			}
			else if ((node.Method.Name == "StartsWith") && (node.Arguments.Count == 1))
			{
				pattern = $"{EvaluateExpression(node.Arguments[0])}%";
			}
			else if ((node.Method.Name == "EndsWith") && (node.Arguments.Count == 1))
			{
				pattern = $"%{EvaluateExpression(node.Arguments[0])}";
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
				Visit(node.Object);
				_sql.Append(" LIKE ");
				_parameters.Add(pattern);
				_sql.Append($"@p{_paramCounter++})");
				return node;
			}

			if (node.Method.Name is "ToLower" or "ToUpper" && (node.Arguments.Count == 0))
			{
				Visit(node.Object);
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
				_sql.Append($"([{m1.Member.Name}] = 1)");
				return node;
			}

			// 2. Boolean negation: !p.IsDeleted == 0
			if (node.Operand is MemberExpression m2
				&& IsMemberOfParameter(m2)
				&& (m2.Type == typeof(bool)))
			{
				_sql.Append($"([{m2.Member.Name}] = 0)");
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

	private object EvaluateExpression(Expression expr)
	{
		return expr switch
		{
			ConstantExpression c => c.Value,
			UnaryExpression { NodeType: ExpressionType.Convert } u => EvaluateExpression(u.Operand),
			MemberExpression m => EvaluateMember(m),
			NewExpression n => EvaluateNew(n),
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
		var args = new object[n.Arguments.Count];
		for (var i = 0; i < args.Length; i++)
		{
			args[i] = EvaluateExpression(n.Arguments[i]);
		}
		return n.Constructor?.Invoke(args);
	}

	private static object FallbackCompile(Expression expr)
	{
		var lambda = Expression.Lambda(expr);
		var compiled = lambda.Compile();
		return compiled.DynamicInvoke();
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

	#endregion
}