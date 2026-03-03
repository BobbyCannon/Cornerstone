using System;
using System.Linq.Expressions;
using System.Text;

namespace Cornerstone.Storage.Sql;

public class OrderByExpressionVisitor : ExpressionVisitor
{
	private readonly StringBuilder _sb = new();

	public string Translate(LambdaExpression expr)
	{
		_sb.Clear();

		if (expr.Parameters.Count != 1)
			throw new ArgumentException("OrderBy lambda must have exactly one parameter");

		Visit(expr.Body);
		return _sb.ToString();
	}

	protected override Expression VisitMember(MemberExpression node)
	{
		if (node.Expression is ParameterExpression)
		{
			_sb.Append($"[{node.Member.Name}]");
			return node;
		}

		// You could also support nested properties: x => x.Address.City
		// But for now we keep it simple (single column)
		throw new NotSupportedException("Only direct member access supported in OrderBy for now");
	}

	protected override Expression VisitParameter(ParameterExpression node)
	{
		// ignore the parameter itself
		return node;
	}

	protected override Expression VisitUnary(UnaryExpression node)
	{
		// Allow (x => x.Id) even if wrapped in Convert etc.
		return Visit(node.Operand);
	}
}