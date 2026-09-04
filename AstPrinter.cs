using System.Text;

namespace SharpLox;

class AstPrinter : Expr.Visitor<string>
{
	public string Print(Expr expr) => expr.Accept(this);

	public string VisitBinaryExpr(Expr.Binary expr) =>
		Parenthesize(expr.oper.lexeme, expr.left, expr.right);

    public string VisitGroupingExpr(Expr.Grouping expr) =>
		Parenthesize("group", expr.expression);

    public string VisitLiteralExpr(Expr.Literal expr) =>
		expr.value is null
			? "nil"
			: expr.value?.ToString() ?? "";

    public string VisitUnaryExpr(Expr.Unary expr) =>
		Parenthesize(expr.oper.lexeme, expr.right);

	private string Parenthesize(string name, params Expr[] exprs)
	{
		var builder = new StringBuilder();

		builder.Append('(')
			.Append(name);

		foreach (var expr in exprs)
		{
			builder.Append(' ');
			builder.Append(expr.Accept(this));
		}

		builder.Append(')');

		return builder.ToString();
	}
}
