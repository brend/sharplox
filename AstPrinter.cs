using System.Text;

namespace SharpLox;

class AstPrinter : Expr.Visitor<string>
{
    public string Print(Expr expr) => expr.Accept(this);

    public string VisitAssignExpr(Expr.Assign expr) =>
        Parenthesize("=" + expr.name.lexeme, expr.value);

    public string VisitBinaryExpr(Expr.Binary expr) =>
        Parenthesize(expr.oper.lexeme, expr.left, expr.right);

    public string VisitCallExpr(Expr.Call expr) =>
        Parenthesize(expr.callee.ToString() ?? "fn", expr.arguments.ToArray());

    public string VisitGetExpr(Expr.Get expr) =>
        Parenthesize(expr.name.lexeme, expr.obj);

    public string VisitGroupingExpr(Expr.Grouping expr) =>
        Parenthesize("group", expr.expression);

    public string VisitLiteralExpr(Expr.Literal expr) =>
        expr.value is null
            ? "nil"
            : expr.value?.ToString() ?? "";

    public string VisitLogicalExpr(Expr.Logical expr) =>
        Parenthesize(expr.oper.lexeme, expr.left, expr.right);

    public string VisitSetExpr(Expr.Set expr) =>
        Parenthesize("set", expr.obj, expr.value);

    public string VisitThisExpr(Expr.This expr) => expr.keyword.lexeme;

    public string VisitUnaryExpr(Expr.Unary expr) =>
        Parenthesize(expr.oper.lexeme, expr.right);

    public string VisitVariableExpr(Expr.Variable expr) =>
        expr.name.lexeme;

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
