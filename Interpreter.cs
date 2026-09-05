namespace SharpLox;

using System.Linq.Expressions;
using static TokenType;

sealed class Interpreter : Expr.Visitor<object?>, Stmt.Visitor<Void>
{
    private Environment environment = new();

    public void Interpret(List<Stmt> statements)
    {
        try
        {
            foreach (var statement in statements)
            {
                Execute(statement);
            }
        }
        catch (RuntimeError error)
        {
            Lox.RuntimeError(error);
        }
    }

    private void Execute(Stmt stmt) => stmt.Accept(this);

    private static string Stringify(object? value)
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

    public object? VisitVariableExpr(Expr.Variable expr) =>
        environment.Get(expr.name);

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

    public Void VisitExpressionStmt(Stmt.Expression stmt)
    {
        Evaluate(stmt.expression);
        return default;
    }

    public Void VisitPrintStmt(Stmt.Print stmt)
    {
        var value = Evaluate(stmt.expression);
        Console.WriteLine(Stringify(value));
        return default;
    }

    public Void VisitVarStmt(Stmt.Var stmt)
    {
        object? value = null;

        if (stmt.initializer is Expr expr)
        {
            value = Evaluate(expr);
        }

        environment.Define(stmt.name.lexeme, value);
        return default;
    }

    public object? VisitAssignExpr(Expr.Assign expr)
    {
        var value = Evaluate(expr.value);
        environment.Assign(expr.name, value);
        return value;
    }

    public Void VisitBlockStmt(Stmt.Block stmt)
    {
        ExecuteBlock(stmt.statements, new Environment(environment));
        return default;
    }

    private void ExecuteBlock(List<Stmt> statements, Environment environment)
    {
        var previous = this.environment;

        try
        {
            this.environment = environment;

            foreach (var stmt in statements)
            {
                Execute(stmt);
            }
        }
        finally
        {
            this.environment = previous;
        }
    }

    public Void VisitIfStmt(Stmt.If stmt)
    {
        if (IsTruthy(Evaluate(stmt.condition)))
        {
            Execute(stmt.thenBranch);
        }
        else if (stmt.elseBranch is not null)
        {
            Execute(stmt.elseBranch);
        }
        return default;
    }

    public object? VisitLogicalExpr(Expr.Logical expr)
    {
        var left = Evaluate(expr.left);

        if (expr.oper.type == OR)
        {
            if (IsTruthy(left)) return left;
        }
        else
        {
            if (!IsTruthy(left)) return left;
        }

        return Evaluate(expr.right);
    }

    public Void VisitWhileStmt(Stmt.While stmt)
    {
        while (IsTruthy(Evaluate(stmt.condition)))
        {
            Execute(stmt.body);
        }
        return default;
    }
}