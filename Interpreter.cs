namespace SharpLox;

using System.Reflection.Emit;
using static TokenType;

sealed class Interpreter : Expr.Visitor<object?>
{
    public void Interpret(Expr expression)
    {
        try
        {
            var value = Evaluate(expression);
            Console.WriteLine(Stringify(value));
        }
        catch (RuntimeError error)
        {
            Lox.RuntimeError(error);
        }
    }

    private string Stringify(object? value)
    {
        if (value is null) return "nil";

        if (value is double d)
        {
            var text = d.ToString();

            return text.EndsWith(".0") 
                ? text[..-2]
                : text;
        }

        return value!.ToString() ?? "";
    }

    public object? VisitBinaryExpr(Expr.Binary expr)
    {
        var left = Evaluate(expr.left);
        var right = Evaluate(expr.right);

        return expr.oper.type switch
        {
            GREATER => CheckNumberOperand(expr.oper, left) > CheckNumberOperand(expr.oper, right),
            GREATER_EQUAL => CheckNumberOperand(expr.oper, left) >= CheckNumberOperand(expr.oper, right),
            LESS => CheckNumberOperand(expr.oper, left) < CheckNumberOperand(expr.oper, right),
            LESS_EQUAL => CheckNumberOperand(expr.oper, left) <= CheckNumberOperand(expr.oper, right),
            BANG_EQUAL => !IsEqual(left, right),
            EQUAL_EQUAL => IsEqual(left, right),
            MINUS => CheckNumberOperand(expr.oper, left) - CheckNumberOperand(expr.oper, right),
            PLUS when left is double a && right is double b =>
                a + b,
            PLUS when left is string s && right is string t =>
                s + t,
            PLUS => 
                throw new RuntimeError(expr.oper, "Operands must be two numbers or two strings."),
            SLASH => CheckNumberOperand(expr.oper, left) / CheckNumberOperand(expr.oper, right),
            STAR => CheckNumberOperand(expr.oper, left) * CheckNumberOperand(expr.oper, right),
            // Unreachable
            _ => null,
        };
    }

    public object? VisitGroupingExpr(Expr.Grouping expr) =>
        Evaluate(expr.expression);

    public object? VisitLiteralExpr(Expr.Literal expr) => 
        expr.value;

    public object? VisitUnaryExpr(Expr.Unary expr)
    {
        var right = Evaluate(expr.right);

        return expr.oper.type switch
        {
            MINUS => -CheckNumberOperand(expr.oper, right),
            BANG => !IsTruthy(right),
            // Unreachable
            _ => null,
        };
    }

    private object? Evaluate(Expr expr) => expr.Accept(this);

    private static bool IsTruthy(object? value) => value switch
    {
        null => false,
        bool b => b,
        _ => true,
    };

    private static bool IsEqual(object? a, object? b)
    {
        if (a == null && b == null) return true;
        if (a == null) return false;

        return a.Equals(b);
    }

    private static double CheckNumberOperand(Token oper, object? value)
    {
        if (value is double d) return d;
        throw new RuntimeError(oper, "Operand must be a number.");
    }
}