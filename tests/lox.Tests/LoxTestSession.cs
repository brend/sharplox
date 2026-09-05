namespace SharpLox.Tests;

using System.Globalization;
using System.Reflection;
using Xunit;

// Console streams and Lox's error flags are process-wide, including in ScannerTests.
[CollectionDefinition("Lox console", DisableParallelization = true)]
public sealed class LoxConsoleCollection;

internal sealed class LoxTestSession : IDisposable
{
    private const BindingFlags PrivateStatic = BindingFlags.NonPublic | BindingFlags.Static;
    private static readonly FieldInfo ErrorFlag = typeof(Lox).GetField("hadError", PrivateStatic)!;
    private static readonly FieldInfo RuntimeErrorFlag = typeof(Lox).GetField("hadRuntimeError", PrivateStatic)!;
    private readonly bool previousError = (bool)ErrorFlag.GetValue(null)!;
    private readonly bool previousRuntimeError = (bool)RuntimeErrorFlag.GetValue(null)!;
    private readonly TextWriter previousOut = Console.Out;
    private readonly TextWriter previousErr = Console.Error;
    private readonly CultureInfo previousCulture = CultureInfo.CurrentCulture;
    private readonly StringWriter output = new();
    private readonly StringWriter errors = new();
    private readonly Interpreter interpreter = new();

    public LoxTestSession(string culture = "en-US")
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
        Console.SetOut(output);
        Console.SetError(errors);
        ErrorFlag.SetValue(null, false);
        RuntimeErrorFlag.SetValue(null, false);
    }

    public string Output => output.ToString().Replace("\r\n", "\n");
    public string Errors => errors.ToString().Replace("\r\n", "\n");
    public bool HadError => (bool)ErrorFlag.GetValue(null)!;
    public bool HadRuntimeError => (bool)RuntimeErrorFlag.GetValue(null)!;

    public List<Stmt> Parse(string source) => new Parser(new Scanner(source).ScanTokens()).Parse();

    public void Interpret(string source)
    {
        var statements = Parse(source);
        Assert.False(HadError, Errors);
        interpreter.Interpret(statements);
    }

    // Exercise the actual application's syntax-error gate, without invoking Main/Exit.
    public void RunApplication(string source) =>
        typeof(Lox).GetMethod("Run", PrivateStatic)!.Invoke(null, [source]);

    public void Dispose()
    {
        Console.SetOut(previousOut);
        Console.SetError(previousErr);
        CultureInfo.CurrentCulture = previousCulture;
        ErrorFlag.SetValue(null, previousError);
        RuntimeErrorFlag.SetValue(null, previousRuntimeError);
        output.Dispose();
        errors.Dispose();
    }
}
