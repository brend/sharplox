namespace SharpLox;

sealed class Resolver : Expr.Visitor<Void>, Stmt.Visitor<Void>
{
    private enum FunctionType
    {
        NONE,
        FUNCTION,
    }

    private readonly Interpreter interpreter;
    private readonly Stack<Dictionary<string, bool>> scopes = [];
    private FunctionType currentFunction = FunctionType.NONE;

    public Resolver(Interpreter interpreter)
    {
        this.interpreter = interpreter;
    }

    public Void VisitBlockStmt(Stmt.Block stmt)
    {
        BeginScope();
        Resolve(stmt.statements);
        EndScope();
        return default;
    }

    public Void VisitExpressionStmt(Stmt.Expression stmt)
    {
        Resolve(stmt.expression);
        return default;
    }

    public Void VisitFunctionStmt(Stmt.Function stmt)
    {
        Declare(stmt.name);
        Define(stmt.name);
        ResolveFunction(stmt, FunctionType.FUNCTION);
        return default;
    }

    public Void VisitIfStmt(Stmt.If stmt)
    {
        Resolve(stmt.condition);
        Resolve(stmt.thenBranch);
        if (stmt.elseBranch is not null) Resolve(stmt.elseBranch);
        return default;
    }

    public Void VisitPrintStmt(Stmt.Print stmt)
    {
        Resolve(stmt.expression);
        return default;
    }

    public Void VisitReturnStmt(Stmt.Return stmt)
    {
        if (currentFunction == FunctionType.NONE)
        {
            Lox.Error(stmt.keyword, "Can't return from top-level code.");
        }
        if (stmt.value is not null)
        {
            Resolve(stmt.value);
        }
        return default;
    }

    public Void VisitVarStmt(Stmt.Var stmt)
    {
        Declare(stmt.name);
        if (stmt.initializer is not null)
        {
            Resolve(stmt.initializer);
        }
        Define(stmt.name);
        return default;
    }

    public Void VisitWhileStmt(Stmt.While stmt)
    {
        Resolve(stmt.condition);
        Resolve(stmt.body);
        return default;
    }

    public void Resolve(List<Stmt> statements)
    {
        foreach (Stmt statement in statements)
        {
            Resolve(statement);
        }
    }

    private void Resolve(Stmt statement) => 
        statement.Accept(this);

    private void Resolve(Expr expression) =>
        expression.Accept(this);

    private void BeginScope() => scopes.Push([]);
    
    private void EndScope() => scopes.Pop();

    private void Declare(Token name)
    {
        if (scopes.Count == 0) return;

        var scope = scopes.Peek();

        if (scope.ContainsKey(name.lexeme))
        {
            Lox.Error(name, "Already a variable with this name in this scope.");
        }

        scope[name.lexeme] = false;
    }

    private void Define(Token name)
    {
        if (scopes.Count == 0) return;
        scopes.Peek()[name.lexeme] = true;
    }

    public Void VisitAssignExpr(Expr.Assign expr)
    {
        Resolve(expr.value);
        ResolveLocal(expr, expr.name);
        return default;
    }

    public Void VisitBinaryExpr(Expr.Binary expr)
    {
        Resolve(expr.left);
        Resolve(expr.right);
        return default;
    }

    public Void VisitCallExpr(Expr.Call expr)
    {
        Resolve(expr.callee);

        foreach (var argument in expr.arguments)
        {
            Resolve(argument);
        }

        return default;
    }

    public Void VisitGroupingExpr(Expr.Grouping expr)
    {
        Resolve(expr.expression);
        return default;
    }

    public Void VisitLiteralExpr(Expr.Literal expr) => default;

    public Void VisitLogicalExpr(Expr.Logical expr)
    {
        Resolve(expr.left);
        Resolve(expr.right);
        return default;
    }

    public Void VisitUnaryExpr(Expr.Unary expr)
    {
        Resolve(expr.right);
        return default;
    }

    public Void VisitVariableExpr(Expr.Variable expr)
    {
        if 
        (
            scopes.TryPeek(out var scope) &&
            scope.TryGetValue(expr.name.lexeme, out var defined) &&
            !defined
        )
        {
            Lox.Error(expr.name, "Can't read local variable in its own initializer.");
        }

        ResolveLocal(expr, expr.name);
        return default;
    }

    private void ResolveLocal(Expr expr, Token name)
    {
        int i = 0;
        foreach (var scope in scopes)
        {
            if (scope.ContainsKey(name.lexeme))
            {
                interpreter.Resolve(expr, i);
                return;
            }
            i++;
        }
    }

    private void ResolveFunction(Stmt.Function function, FunctionType type)
    {
        var enclosingFunction = currentFunction;
        currentFunction = type;
        BeginScope();
        foreach (var param in function.parameters)
        {
            Declare(param);
            Define(param);
        }
        Resolve(function.body);
        EndScope();
        currentFunction = enclosingFunction;
    }

    public Void VisitClassStmt(Stmt.Class stmt)
    {
        Declare(stmt.name);
        Define(stmt.name);
        return default;
    }

    public Void VisitGetExpr(Expr.Get expr)
    {
        Resolve(expr.obj);
        return default;
    }

    public Void VisitSetExpr(Expr.Set expr)
    {
        Resolve(expr.value);
        Resolve(expr.obj);
        return default;
    }
}