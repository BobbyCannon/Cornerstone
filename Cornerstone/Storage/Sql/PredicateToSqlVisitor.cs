#region References

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
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
		// Special handling for null checks
		if (node.NodeType
			is ExpressionType.Equal
			or ExpressionType.NotEqual)
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
		}

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
		_sql.Append($"@p{_paramCounter++}");
		return node;
	}

	protected override Expression VisitMember(MemberExpression node)
	{
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
		if (node.Object?.Type == typeof(string))
		{
			string pattern = null;

			if ((node.Method.Name == "Contains") && (node.Method.GetParameters().Length == 1))
			{
				pattern = $"%{EvaluateExpression(node.Arguments[0])}%";
			}
			else if ((node.Method.Name == "StartsWith") && (node.Method.GetParameters().Length == 1))
			{
				pattern = $"{EvaluateExpression(node.Arguments[0])}%";
			}
			else if ((node.Method.Name == "EndsWith") && (node.Method.GetParameters().Length == 1))
			{
				pattern = $"%{EvaluateExpression(node.Arguments[0])}";
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
			if (node.Operand is MemberExpression member &&
				IsMemberOfParameter(member) &&
				(member.Type == typeof(bool)))
			{
				_sql.Append('(');
				_sql.Append($"[{member.Member.Name}] = 0");
				_sql.Append(')');
				return node;
			}

			// existing general NOT handling
			_sql.Append("(NOT ");
			Visit(node.Operand);
			_sql.Append(")");
			return node;
		}

		return base.VisitUnary(node);
	}

	private object EvaluateExpression(Expression expr)
	{
		try
		{
			var lambda = Expression.Lambda(expr);
			var compiled = lambda.Compile();
			return compiled.DynamicInvoke();
		}
		catch (Exception ex)
		{
			return expr switch
			{
				ConstantExpression c => c.Value,
				UnaryExpression { NodeType: ExpressionType.Convert } u => EvaluateExpression(u.Operand),
				_ => throw new NotSupportedException($"Cannot evaluate expression to constant: {expr}\n{ex.Message}", ex)
			};
		}
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
			return c.Value == null;
		}

		try
		{
			var value = EvaluateExpression(expr);
			return value == null;
		}
		catch
		{
			return false;
		}
	}

	#endregion
}