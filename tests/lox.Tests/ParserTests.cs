namespace SharpLox.Tests;

using Xunit;

[Collection("Lox console")]
public class ParserTests
{
    [Fact]
    public void SyntaxDiagnostic_SeparatesErrorAndTokenLocation()
    {
        using var session = new LoxTestSession();
        session.Parse("print ;");
        Assert.Equal("[line 1] Error at ';': Expect expression.\n", session.Errors);
        Assert.True(session.HadError);
    }

    [Theory]
    [InlineData("print")]
    [InlineData("var a =")]
    [InlineData("{")]
    [InlineData("if (true)")]
    [InlineData("while (true)")]
    [InlineData("for (;;")]
    [InlineData("class")]
    [InlineData("class Box {")]
    [InlineData("class Box { method(")]
    [InlineData("class Box { method() {")]
    [InlineData("print object.")]
    [InlineData("object.field =")]
    public void TruncatedInput_ReportsErrorWithoutHostException(string source)
    {
        using var session = new LoxTestSession();
        session.Parse(source);
        Assert.True(session.HadError);
        Assert.Contains("Error at end:", session.Errors);
    }

    [Fact]
    public void Recovery_PreservesFollowingValidStatement()
    {
        using var session = new LoxTestSession();
        var statements = session.Parse("var broken = ; print 42;");
        var print = Assert.IsType<Stmt.Print>(Assert.Single(statements));
        Assert.Equal(42d, Assert.IsType<Expr.Literal>(print.expression).value);
        Assert.True(session.HadError);
    }

    [Theory]
    [InlineData("print \"must not run\"; @")]
    [InlineData("print \"must not run\"; var a = ;")]
    [InlineData("print \"must not run\"; (a) = 1;")]
    [InlineData("print \"must not run\"; if (true) var a = 1;")]
    [InlineData("print \"must not run\"; class Box { fun method() {} }")]
    [InlineData("print \"must not run\"; class Box { var field; }")]
    [InlineData("print \"must not run\"; this = 1;")]
    public void Application_DoesNotExecuteAnyStatementsAfterSyntaxError(string source)
    {
        using var session = new LoxTestSession();
        session.RunApplication(source);
        Assert.True(session.HadError);
        Assert.False(session.HadRuntimeError);
        Assert.Empty(session.Output);
    }
}
