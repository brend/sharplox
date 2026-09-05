namespace SharpLox.Tests;

using Xunit;

[Collection("Lox console")]
public class ResolverTests
{
    [Theory]
    [InlineData("{ var a = 1; { print a; } }", "1\n")]
    [InlineData("{ { var a = 2; print a; } }", "2\n")]
    [InlineData("{ var a = 1; { var a = 2; { print a; } } }", "2\n")]
    [InlineData("{ var a = 1; { a = 2; } print a; }", "2\n")]
    [InlineData("{ var a = 1; { var a = 2; a = 3; print a; } print a; }", "3\n1\n")]
    [InlineData("{ var a; { print a; } }", "nil\n")]
    public void NestedScopes_ReadAndAssignTheNearestDeclaration(string source, string expected)
    {
        AssertOutput(source, expected);
    }

    [Theory]
    [InlineData("var a = \"global\"; { fun show() { print a; } show(); var a = \"block\"; show(); }", "global\nglobal\n")]
    [InlineData("var a = 1; { fun set() { a = 2; } var a = 3; set(); print a; } print a;", "3\n2\n")]
    [InlineData("{ var a = 1; { fun show() { print a; } var a = 2; show(); } }", "1\n")]
    [InlineData("fun make() { var n = 0; fun count() { n = n + 1; return n; } return count; } var c = make(); print c(); print c(); var d = make(); print d();", "1\n2\n1\n")]
    [InlineData("{ fun fact(n) { if (n <= 1) return 1; return n * fact(n - 1); } print fact(5); }", "120\n")]
    [InlineData("fun outer(a) { fun inner(a) { return a; } print inner(2); return a; } print outer(1);", "2\n1\n")]
    [InlineData("fun f() { fun g() { return 1; } { return g(); } print missing; } print f();", "1\n")]
    [InlineData("fun f() { return; } fun g() {} print f(); print g();", "nil\nnil\n")]
    [InlineData("fun f() { print later; } var later = 4; f(); var later = later + 1; print later;", "4\n5\n")]
    public void Functions_UseLexicalBindingsAndUnwindReturns(string source, string expected)
    {
        AssertOutput(source, expected);
    }

    [Theory]
    [InlineData("{ var a = a; }", "a", "Can't read local variable in its own initializer.")]
    [InlineData("var a = 1; { var a = a; }", "a", "Can't read local variable in its own initializer.")]
    [InlineData("{ var a; var a; }", "a", "Already a variable with this name in this scope.")]
    [InlineData("fun f(a, a) {}", "a", "Already a variable with this name in this scope.")]
    [InlineData("fun f(a) { var a; }", "a", "Already a variable with this name in this scope.")]
    [InlineData("{ var f; fun f() {} }", "f", "Already a variable with this name in this scope.")]
    [InlineData("return 1;", "return", "Can't return from top-level code.")]
    [InlineData("if (false) { return; }", "return", "Can't return from top-level code.")]
    [InlineData("fun f() {} return;", "return", "Can't return from top-level code.")]
    [InlineData("while (false) { var a = a; }", "a", "Can't read local variable in its own initializer.")]
    [InlineData("fun unused() { var a = a; }", "a", "Can't read local variable in its own initializer.")]
    public void ResolutionErrors_PreventAllExecution(string source, string token, string message)
    {
        using var session = new LoxTestSession();
        session.RunApplication("print \"must not run\";\n" + source);
        Assert.True(session.HadError);
        Assert.False(session.HadRuntimeError);
        Assert.Empty(session.Output);
        Assert.Equal($"[line 2] Error at '{token}': {message}\n", session.Errors);
    }

    [Fact]
    public void Closure_RemainsResolvedAcrossSeparateRuns()
    {
        using var session = new LoxTestSession();
        session.Interpret("fun make(a) { fun get() { return a; } return get; } var get = make(42);");
        session.Interpret("print get();");
        Assert.Equal("42\n", session.Output);
        Assert.Empty(session.Errors);
    }

    private static void AssertOutput(string source, string expected)
    {
        using var session = new LoxTestSession();
        session.Interpret(source);
        Assert.False(session.HadError, session.Errors);
        Assert.False(session.HadRuntimeError, session.Errors);
        Assert.Empty(session.Errors);
        Assert.Equal(expected, session.Output);
    }
}
