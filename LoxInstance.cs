namespace SharpLox;

sealed class LoxInstance
{
    private LoxClass klass;
    private readonly Dictionary<string, object?> fields = [];

    public LoxInstance(LoxClass klass)
    {
        this.klass = klass;
    }

    public override string ToString() => $"{klass.Name} instance";

    public object? Get(Token name)
    {
        if (fields.TryGetValue(name.lexeme, out var value))
        {
            return value;
        }

        throw new RuntimeError(name, $"Undefined property '{name.lexeme}'.");
    }

    public void Set(Token name, object? value)
    {
        fields[name.lexeme] = value;
    }
}