namespace SharpLox;

readonly record struct ForeignCallable(
    int Arity,
    Func<Interpreter, List<object?>, object?> Function
) : LoxCallable
{
    public object? Call(Interpreter interpreter, List<object?> arguments) =>
        Function(interpreter, arguments);

    public override string ToString() => "<native fn>";
}