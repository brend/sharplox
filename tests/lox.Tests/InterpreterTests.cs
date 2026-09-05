namespace SharpLox.Tests;

using Xunit;

[Collection("Lox console")]
public class InterpreterTests
{
    [Fact]
    public void Print_CustomHostNumberFormatCannotCauseRangeException()
    {
        using var session = new LoxTestSession();
        var culture = (System.Globalization.CultureInfo)System.Globalization.CultureInfo.InvariantCulture.Clone();
        // Ordinary double.ToString() rarely ends in .0. Force that suffix to
        // expose the invalid text[..-2] range without changing production code.
        culture.NumberFormat.PositiveInfinitySymbol = "Infinity.0";
        System.Globalization.CultureInfo.CurrentCulture = culture;

        var exception = Record.Exception(() => session.Interpret("print 1 / 0;"));

        Assert.Null(exception);
        Assert.Empty(session.Errors);
    }

    [Fact]
    public void Print_UsesLoxBooleanSpelling()
    {
        using var session = new LoxTestSession();
        session.Interpret("print true; print false; print !nil; print 1 == 2;");
        Assert.Equal("true\nfalse\ntrue\nfalse\n", session.Output);
        Assert.Empty(session.Errors);
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    [InlineData("fr-FR")]
    public void Print_NumbersAreIndependentOfHostCulture(string culture)
    {
        using var session = new LoxTestSession(culture);
        session.Interpret("print 1.5; print 3 / 2; print 42; print nil;");
        Assert.Equal("1.5\n1.5\n42\nnil\n", session.Output);
        Assert.Empty(session.Errors);
    }

    [Theory]
    [InlineData("print 2 + 3 * 4; print (2 + 3) * 4;", "14\n20\n")]
    [InlineData("print 8 / 2 / 2; print 8 - 2 - 1;", "2\n5\n")]
    [InlineData("var a; var b; print a = b = 7; print a; print b;", "7\n7\n7\n")]
    [InlineData("var a = 0; print (a = 1) + (a = 2); print a;", "3\n2\n")]
    [InlineData("var a = \"outer\"; { var a = a; print a; a = \"inner\"; } print a;", "outer\nouter\n")]
    [InlineData("var a = 1; { { a = 2; } } print a;", "2\n")]
    public void ExpressionsAndScope_FollowChapterSemantics(string source, string expected)
    {
        using var session = new LoxTestSession();
        session.Interpret(source);
        Assert.Equal(expected, session.Output);
        Assert.Empty(session.Errors);
    }

    [Theory]
    [InlineData("print missing;", "Undefined variable 'missing'.")]
    [InlineData("missing = 1;", "Undefined variable 'missing'.")]
    [InlineData("print -\"text\";", "Operand must be a number.")]
    [InlineData("print 1 + \"text\";", "Operands must be two numbers or two strings.")]
    [InlineData("print nil * 2;", "Operand must be a number.")]
    public void RuntimeErrors_AreReportedAndStopRemainingStatements(string source, string message)
    {
        using var session = new LoxTestSession();
        session.Interpret("\n" + source + " print \"must not run\";");
        Assert.True(session.HadRuntimeError);
        Assert.Equal(message + "\n[line 2]\n", session.Errors);
        Assert.Empty(session.Output);
    }

    [Fact]
    public void RuntimeError_RestoresEnclosingEnvironmentForNextInterpretation()
    {
        using var session = new LoxTestSession();
        session.Interpret("var a = \"global\"; { var a = \"local\"; { print missing; } }");
        Assert.True(session.HadRuntimeError);
        session.Interpret("print a;");
        Assert.Equal("global\n", session.Output);
    }
}
