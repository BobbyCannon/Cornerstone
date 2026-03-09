#region References

using System;
using System.Linq.Expressions;
using System.Text;

#endregion

namespace Cornerstone.Storage.Sql;

public class OrderByExpressionVisitor : ExpressionVisitor
{
	#region Fields

	private readonly StringBuilder _sb = new();

	#endregion

	#region Methods

	public string Translate(LambdaExpression expression)
	{
		_sb.Clear();

		if (expression.Parameters.Count != 1)
		{
			throw new ArgumentException("OrderBy lambda must have exactly one parameter");
		}

		Visit(expression.Body);
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

	#endregion
}