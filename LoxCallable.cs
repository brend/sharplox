namespace SharpLox;

interface LoxCallable
{
    int Arity { get; }
    object? Call(Interpreter interpreter, List<object?> arguments);
}