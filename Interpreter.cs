namespace SharpLox;

using System.Globalization;
using static TokenType;

sealed class Interpreter : Expr.Visitor<object?>, Stmt.Visitor<Void>
{
    public readonly Environment Globals = new();

    private Environment environment;
    private readonly Dictionary<Expr, int> locals = [];

    public Interpreter()
    {
        environment = Globals;
        Globals.Define("clock", new ForeignCallable
        {
            Arity = 0,
            Function = (_, _) => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0,
        });
    }

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
            var text = d.ToString(CultureInfo.InvariantCulture);

            return text.EndsWith(".0")
                ? text[..^2]
                : text;
        }

        return value switch
        {
            true => "true", // true.ToString() produces "True"
            false => "false", // false.ToString() produces "False"
            _ => value.ToString() ?? ""
        };
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
        LookUpVariable(expr.name, expr);

    private object? LookUpVariable(Token name, Expr expr)
    {
        if (locals.TryGetValue(expr, out int distance))
        {
            return environment.GetAt(distance, name.lexeme);
        }
        else
        {
            return Globals.Get(name);
        }
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
        
        if (locals.TryGetValue(expr, out int distance))
        {
            environment.AssignAt(distance, expr.name, value);
        }
        else
        {
            Globals.Assign(expr.name, value);
        }
        
        return value;
    }

    public Void VisitBlockStmt(Stmt.Block stmt)
    {
        ExecuteBlock(stmt.statements, new Environment(environment));
        return default;
    }

    public void ExecuteBlock(List<Stmt> statements, Environment environment)
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

    public object? VisitCallExpr(Expr.Call expr)
    {
        object? callee = Evaluate(expr.callee);

        var arguments = new List<object?>();

        foreach (var argument in expr.arguments)
        {
            arguments.Add(Evaluate(argument));
        }

        if (callee is not LoxCallable function)
        {
            throw new RuntimeError(expr.paren, "Can only call functions and methods.");
        }

        if (arguments.Count != function.Arity)
        {
            throw new RuntimeError(expr.paren, $"Expected {function.Arity} arguments but got {arguments.Count}.");
        }

        return function.Call(this, arguments);
    }

    public Void VisitFunctionStmt(Stmt.Function stmt)
    {
        var function = new LoxFunction(stmt, environment);
        environment.Define(stmt.name.lexeme, function);
        return default;
    }

    public Void VisitReturnStmt(Stmt.Return stmt)
    {
        object? value = null;
        if (stmt.value is not null)
        {
            value = Evaluate(stmt.value);
        }
        throw new Return(value);
    }

    public void Resolve(Expr expr, int depth)
    {
        locals[expr] = depth;
    }

    public Void VisitClassStmt(Stmt.Class stmt)
    {
        environment.Define(stmt.name.lexeme, null);
        var klass = new LoxClass(stmt.name.lexeme);
        environment.Assign(stmt.name, klass);
        return default;
    }
}