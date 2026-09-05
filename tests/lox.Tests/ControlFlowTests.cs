namespace SharpLox.Tests;

using Xunit;

[Collection("Lox console")]
public class ControlFlowTests
{
    [Theory]
    [InlineData("nil", "else")]
    [InlineData("false", "else")]
    [InlineData("true", "then")]
    [InlineData("0", "then")]
    [InlineData("\"\"", "then")]
    public void If_OnlyNilAndFalseAreFalsey(string condition, string expected)
    {
        using var session = new LoxTestSession();
        session.Interpret($"if ({condition}) print \"then\"; else print \"else\";");
        Assert.Equal(expected + "\n", session.Output);
        Assert.Empty(session.Errors);
    }

    [Theory]
    [InlineData("if (true) if (false) print \"wrong\"; else print \"inner\";", "inner\n")]
    [InlineData("if (false) print missing; print \"ok\";", "ok\n")]
    [InlineData("print \"left\" or missing; print nil and missing;", "left\nnil\n")]
    [InlineData("var a = 0; true or (a = 1); false and (a = 2); print a;", "0\n")]
    [InlineData("print nil or \"right\"; print 0 and \"right\"; print \"\" or missing;", "right\nright\n\n")]
    [InlineData("print \"left\" or nil and \"right\";", "left\n")]
    public void BranchesAndLogicalOperators_SelectAndShortCircuit(string source, string expected)
    {
        using var session = new LoxTestSession();
        session.Interpret(source);
        Assert.Equal(expected, session.Output);
        Assert.Empty(session.Errors);
    }

    [Fact]
    public void While_ReevaluatesConditionAndStopsOnRuntimeError()
    {
        using var session = new LoxTestSession();
        // A runtime-error sentinel also bounds this test if the condition becomes stale.
        session.Interpret("var i = 0; while (i < 3) { if (i > 3) print runaway; print i; i = i + 1; }");
        Assert.Equal("0\n1\n2\n", session.Output);
        Assert.Empty(session.Errors);
        session.Interpret("while (true) print missing; print \"unreachable\";");
        Assert.True(session.HadRuntimeError);
        Assert.Equal("0\n1\n2\n", session.Output);
    }

    [Fact]
    public void For_OrdersClausesAndScopesInitializer()
    {
        using var session = new LoxTestSession();
        session.Interpret("""
            var i = "outer";
            var trace = "";
            for (var i = 0; i < 2; i = i + 1) {
                trace = trace + "body";
                print i;
                if (trace == "bodybodybody") print runaway;
            }
            print trace;
            print i;
            """);
        Assert.Equal("0\n1\nbodybody\nouter\n", session.Output);
        Assert.Empty(session.Errors);
    }

    [Theory]
    [InlineData("var i = 0; for (; i < 2;) { print i; i = i + 1; if (i > 3) print runaway; }", "0\n1\n", false)]
    [InlineData("var i; for (i = 0; i < 0; i = i + 1) print missing; print i;", "0\n", false)]
    [InlineData("for (;;) print stop;", "", true)]
    [InlineData("for (var i = 0; false;) print i; print i;", "", true)]
    public void For_HandlesOmittedClausesAndDoesNotLeakLocals(string source, string expected, bool runtimeError)
    {
        using var session = new LoxTestSession();
        session.Interpret(source);
        Assert.Equal(expected, session.Output);
        Assert.Equal(runtimeError, session.HadRuntimeError);
        if (!runtimeError) Assert.Empty(session.Errors);
        else Assert.Contains("Undefined variable", session.Errors);
    }
}
