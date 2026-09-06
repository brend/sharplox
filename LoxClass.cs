namespace SharpLox;

sealed class LoxClass : LoxCallable
{
    public string Name { get; }

    public int Arity => 0;

    public LoxClass(string name)
    {
        Name = name;
    }

    public override string ToString() => Name;

    public object? Call(Interpreter interpreter, List<object?> arguments)
    {
        var instance = new LoxInstance(this);

        return instance;
    }
}