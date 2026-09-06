namespace SharpLox;

sealed class LoxClass : LoxCallable
{
    public string Name { get; }

    public Dictionary<string, LoxFunction> Methods { get; }

    public int Arity => FindMethod("init")?.Arity ?? 0;

    public LoxClass? Superclass { get; }

    public LoxClass(
        string name,
        LoxClass? superclass,
        Dictionary<string, LoxFunction> methods
        )
    {
        Name = name;
        Superclass = superclass;
        Methods = methods;
    }

    public override string ToString() => Name;

    public object? Call(Interpreter interpreter, List<object?> arguments)
    {
        var instance = new LoxInstance(this);
        if (FindMethod("init") is LoxFunction initializer)
        {
            initializer.Bind(instance).Call(interpreter, arguments);
        }

        return instance;
    }

    public LoxFunction? FindMethod(string name)
    {
        if (Methods.TryGetValue(name, out var method))
        {
            return method;
        }

        if (Superclass is not null)
        {
            return Superclass.FindMethod(name);
        }

        return null;
    }
}