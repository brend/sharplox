namespace SharpLox.Tests;

using Xunit;

[Collection("Lox console")]
public class ClosureTests
{
    // Extensions of the lexical-scope and escaping-closure examples in:
    // https://craftinginterpreters.com/functions.html#local-functions-and-closures
    // https://craftinginterpreters.com/resolving-and-binding.html#static-scope
    // https://craftinginterpreters.com/closures.html#closed-upvalues
    [Fact]
    public void CallerShadowing_DoesNotChangeCapturedReadsOrWrites()
    {
        AssertOutput("""
            fun make() {
                var value = "captured";
                fun update() { print value; value = "updated"; }
                return update;
            }
            var update = make();
            fun caller() {
                var value = "caller";
                update();
                print value;
            }
            caller();
            update();
            """, "captured\ncaller\nupdated\n");
    }

    [Fact]
    public void SiblingClosures_ShareOneMutableBindingAfterReturn()
    {
        AssertOutput("""
            var read;
            var write;
            fun make() {
                var value = "initial";
                fun get() { print value; }
                fun set(next) { value = next; }
                read = get;
                write = set;
                value = "before return";
            }
            make();
            read();
            write("after return");
            read();
            """, "before return\nafter return\n");
    }

    [Fact]
    public void SeparateFactoryCalls_KeepIndependentCapturedParameters()
    {
        AssertOutput("""
            fun make(value) {
                fun append(suffix) { value = value + suffix; print value; }
                return append;
            }
            var first = make("A");
            var second = make("B");
            first("1");
            second("2");
            first("3");
            second("4");
            """, "A1\nB2\nA13\nB24\n");
    }

    [Fact]
    public void DeepClosure_CapturesThroughAnIntermediateFunctionThatDoesNotReadTheVariable()
    {
        AssertOutput("""
            fun outer() {
                var value = 0;
                fun middle() {
                    fun inner() { value = value + 1; print value; }
                    return inner;
                }
                return middle;
            }
            var middle = outer();
            var first = middle();
            var second = middle();
            first();
            second();
            first();
            """, "1\n2\n3\n");
    }

    [Fact]
    public void EscapedBlockLocal_SurvivesScopeExitAndLaterShadowing()
    {
        AssertOutput("""
            var escaped;
            {
                var value = "block";
                fun read() { print value; }
                escaped = read;
            }
            { var value = "other block"; escaped(); }
            var value = "global";
            escaped();
            """, "block\nblock\n");
    }

    [Fact]
    public void LaterLocalDeclaration_DoesNotRetargetAnEscapedAssignment()
    {
        AssertOutput("""
            var value = "global";
            fun make() {
                fun update() { value = "updated global"; }
                var value = "local";
                update();
                print value;
                return update;
            }
            var update = make();
            value = "reset";
            update();
            print value;
            """, "local\nupdated global\n");
    }

    [Fact]
    public void RecursiveCalls_PreserveEachInvocationsCapturedLocal()
    {
        AssertOutput("""
            var first;
            var second;
            fun collect(n) {
                fun read() { print n; }
                if (n == 1) first = read;
                if (n == 2) second = read;
                if (n > 1) collect(n - 1);
                print n;
            }
            collect(2);
            second();
            first();
            """, "1\n2\n2\n1\n");
    }

    [Fact]
    public void LoopClosures_ShareInitializerButCaptureSeparateBodyLocals()
    {
        AssertOutput("""
            var first;
            var second;
            for (var i = 0; i < 2; i = i + 1) {
                if (i > 2) print runaway;
                var copy = i;
                fun read() { print i; print copy; }
                if (i == 0) first = read;
                else second = read;
            }
            first();
            second();
            """, "2\n0\n2\n1\n");
    }

    [Theory]
    [InlineData("print later;")]
    [InlineData("later = 2;")]
    public void LaterLocalDeclaration_DoesNotProvideAnOtherwiseUndefinedGlobal(string use)
    {
        using var session = new LoxTestSession();
        session.Interpret("{ fun access() { " + use + " } var later = 1; access(); } print \"unreachable\";");

        Assert.False(session.HadError, session.Errors);
        Assert.True(session.HadRuntimeError);
        Assert.Empty(session.Output);
        Assert.Equal("Undefined variable 'later'.\n[line 1]\n", session.Errors);
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
