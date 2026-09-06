namespace SharpLox;

sealed class LoxFunction : LoxCallable
{
    private readonly Stmt.Function declaration;
    private readonly Environment closure;
    private readonly bool isInitializer;

    public LoxFunction(
        Stmt.Function declaration, 
        Environment closure,
        bool isInitializer
        )
    {
        this.isInitializer = isInitializer;
        this.closure = closure;
        this.declaration = declaration;
    }

    public object? Call(Interpreter interpreter, List<object?> arguments)
    {
        var environment = new Environment(closure);
        for (int i = 0; i < declaration.parameters.Count; i++)
        {
            environment.Define(declaration.parameters[i].lexeme, arguments[i]);
        }

        try 
        {
            interpreter.ExecuteBlock(declaration.body, environment);
        }
        catch (Return returnValue)
        {
            if (isInitializer) return closure.GetAt(0, "this");
            
            return returnValue.Value;
        }

        if (isInitializer) return closure.GetAt(0, "this");

        return null;
    }

    public int Arity => declaration.parameters.Count;

    public override string ToString() => $"<fn {declaration.name.lexeme}>";

    public LoxFunction Bind(LoxInstance instance)
    {
        var environment = new Environment(closure);
        environment.Define("this", instance);
        return new LoxFunction(declaration, environment, isInitializer);
    }
}