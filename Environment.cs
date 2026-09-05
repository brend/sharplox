namespace SharpLox;

sealed class Environment
{
    private readonly Environment? enclosing;
    private readonly Dictionary<string, object?> values = [];

    public Environment(Environment? enclosing = null)
    {
        this.enclosing = enclosing;
    }

    public void Define(string name, object? value) =>
        values[name] = value;

    public object? Get(Token name)
    {
        if (values.TryGetValue(name.lexeme, out var value))
        {
            return value;
        }

        if (enclosing != null)
        {
            return enclosing.Get(name);
        }

        throw new RuntimeError(name, $"Undefined variable '{name.lexeme}'.");
    }

    public object? GetAt(int distance, string name) =>
        Ancestor(distance).values[name];

    private Environment Ancestor(int distance)
    {
        var environment = this;
        for (int i = 0; i < distance; i++)
        {
            environment = environment!.enclosing;
        }
        return environment!;
    }

    public void Assign(Token name, object? value)
    {
        if (values.ContainsKey(name.lexeme))
        {
            values[name.lexeme] = value;
            return;
        }

        if (enclosing != null)
        {
            enclosing.Assign(name, value);
            return;
        }

        throw new RuntimeError(name, $"Undefined variable '{name.lexeme}'.");
    }

    public void AssignAt(int distance, Token name, object? value) =>
        Ancestor(distance).values[name.lexeme] = value;
}