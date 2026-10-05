#region References

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Storage.Sql;

public class OrderByExpressionVisitor : ExpressionVisitor
{
	#region Fields

	private readonly string _close;
	private readonly string _open;
	private ParameterExpression _parameter;
	private SourceTypeInfo _sourceType;
	private readonly StringBuilder _sql;

	#endregion

	#region Constructors

	public OrderByExpressionVisitor(SqlProvider provider)
	{
		var brackets = SqlGenerator.GetIdentifierBrackets(provider);
		_open = brackets.Open;
		_close = brackets.Close;
		_parameter = null;
		_sourceType = null;
		_sql = new StringBuilder();
	}

	#endregion

	#region Methods

	[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "OrderBy lambda parameter type is a [SourceReflection] entity supplied by SqlQuery.")]
	public string Translate(LambdaExpression expression)
	{
		_sql.Clear();

		if (expression.Parameters.Count != 1)
		{
			throw new ArgumentException("OrderBy lambda must have exactly one parameter");
		}

		_parameter = expression.Parameters[0];
		_sourceType = SourceReflector.GetRequiredSourceType(_parameter.Type);
		Visit(expression.Body);
		return _sql.ToString();
	}

	protected override Expression VisitMember(MemberExpression node)
	{
		if (node.Expression is ParameterExpression)
		{
			_sql.Append(_open);
			_sql.Append(SqlGenerator.GetColumnName(_sourceType, node.Member.Name));
			_sql.Append(_close);
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