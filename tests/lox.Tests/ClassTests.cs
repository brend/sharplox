namespace SharpLox.Tests;

using Xunit;

[Collection("Lox console")]
public class ClassTests
{
    // Semantics through https://craftinginterpreters.com/classes.html (no inheritance).
    [Fact]
    public void ClassesAreFirstClassAndInstancesHaveIdentityAndIndependentFields()
    {
        AssertOutput("""
            class Box {}
            var factory = Box;
            var a = factory();
            var b = factory();
            var alias = a;
            a.value = 1;
            b.value = 2;
            alias.value = 3;
            print Box;
            print a;
            print factory == Box;
            print a == alias;
            print a == b;
            print a.value;
            print b.value;
            """, "Box\nBox instance\ntrue\ntrue\nfalse\n3\n2\n");
    }

    [Fact]
    public void ExtractedMethodsKeepTheirReceiverEvenWhenStoredOnAnotherInstance()
    {
        AssertOutput("""
            class Box {
                init(value) { this.value = value; }
                get() { return this.value; }
            }
            var a = Box("A");
            var b = Box("B");
            var get = a.get;
            b.get = get;
            a.value = "changed";
            print get();
            print b.get();
            print b.value;
            """, "changed\nchanged\nB\n");
    }

    [Fact]
    public void FieldsIncludingNilShadowMethodsAndCanStorePlainFunctions()
    {
        AssertOutput("""
            class Box { value() { return "method"; } }
            var box = Box();
            print box.value();
            box.value = nil;
            print box.value;
            fun replacement() { return "function"; }
            box.value = replacement;
            print box.value();
            """, "method\nnil\nfunction\n");
    }

    [Theory]
    [InlineData("this.value = value;")]
    [InlineData("this.value = value; return; print missing;")]
    public void InitializersAlwaysReturnTheirReceiverIncludingWhenExtracted(string body)
    {
        AssertOutput("class Box { init(value) { " + body + " } } " + """
            var box = Box(1);
            print box.value;
            print box.init(2) == box;
            var init = box.init;
            print init(3) == box;
            print box.value;
            """, "1\ntrue\ntrue\n3\n");
    }

    [Fact]
    public void LocalClassAndEscapingClosureCaptureThisAndOuterBindings()
    {
        AssertOutput("""
            fun make(prefix) {
                class Box {
                    init(value) { this.value = value; }
                    reader() {
                        fun read(suffix) { return prefix + this.value + suffix; }
                        return read;
                    }
                    copy() { return Box(this.value); }
                }
                return Box;
            }
            var Box = make("prefix:");
            var box = Box("A");
            var read = box.reader();
            box.value = "B";
            print read("!");
            print box.copy().reader()("?");
            """, "prefix:B!\nprefix:B?\n");
    }

    [Fact]
    public void NestedClassRestoresOuterThisAndFunctionReturnContext()
    {
        AssertOutput("""
            class Outer {
                init() {
                    fun value() { return "outer"; }
                    this.name = value();
                }
                read() {
                    class Inner {
                        init() { this.name = "inner"; }
                        read() { return this.name; }
                    }
                    print Inner().read();
                    return this.name;
                }
            }
            print Outer().read();
            fun init() { return "ordinary function"; }
            print init();
            """, "inner\nouter\nordinary function\n");
    }

    [Fact]
    public void PropertyAssignmentEvaluatesReceiverOnceBeforeValueAndReturnsValue()
    {
        AssertOutput("""
            class Box {}
            var box = Box();
            fun receiver() { print "receiver"; return box; }
            fun value() { print "value"; return 42; }
            print receiver().a = box.b = value();
            print box.a;
            print box.b;
            """, "receiver\nvalue\n42\n42\n42\n");
    }

    [Fact]
    public void InvalidPropertyReceiverPreventsValueEvaluation()
    {
        using var session = new LoxTestSession();
        session.Interpret("var changed = false; nil.field = (changed = true);");
        Assert.True(session.HadRuntimeError);
        Assert.Equal("Only instances have fields.\n[line 1]\n", session.Errors);
        session.Interpret("print changed;");
        Assert.Equal("false\n", session.Output);
    }

    [Fact]
    public void BoundMethodRemainsResolvedAcrossSeparateRuns()
    {
        using var session = new LoxTestSession();
        session.Interpret("class Box { get() { return this.value; } } var box = Box(); box.value = 42; var get = box.get;");
        session.Interpret("print get();");
        Assert.Equal("42\n", session.Output);
        Assert.Empty(session.Errors);
    }

    [Theory]
    [InlineData("class Box {} Box(1);", "Expected 0 arguments but got 1.")]
    [InlineData("class Box { init(a) {} } Box();", "Expected 1 arguments but got 0.")]
    [InlineData("class Box { init(a) {} } Box(1, 2);", "Expected 1 arguments but got 2.")]
    [InlineData("class Box { method(a) {} } Box().method();", "Expected 1 arguments but got 0.")]
    [InlineData("class Box {} print Box().missing;", "Undefined property 'missing'.")]
    [InlineData("class Box {} print Box.field;", "Only instances have properties.")]
    [InlineData("print nil.field;", "Only instances have properties.")]
    [InlineData("print 123.field;", "Only instances have properties.")]
    [InlineData("class Box {} Box.field = 1;", "Only instances have fields.")]
    [InlineData("false.field = 1;", "Only instances have fields.")]
    public void RuntimeErrorsReportPropertyOrCallLocationAndStopExecution(string source, string error)
    {
        using var session = new LoxTestSession();
        session.Interpret("\n" + source + " print \"unreachable\";");
        Assert.False(session.HadError, session.Errors);
        Assert.True(session.HadRuntimeError);
        Assert.Equal(error + "\n[line 2]\n", session.Errors);
        Assert.Empty(session.Output);
    }

    [Theory]
    [InlineData("print this;", "this", "Can't use 'this' outside of a class.")]
    [InlineData("fun unused() { print this; }", "this", "Can't use 'this' outside of a class.")]
    [InlineData("class Box {} print this;", "this", "Can't use 'this' outside of a class.")]
    [InlineData("class Box { init() { return nil; } }", "return", "Can't return a value from an initializer.")]
    [InlineData("class Box { init() { return this; } }", "return", "Can't return a value from an initializer.")]
    [InlineData("class Box { init() { fun f() {} return 1; } }", "return", "Can't return a value from an initializer.")]
    [InlineData("class Box { init() { class Inner { method() { return 1; } } return 1; } }", "return", "Can't return a value from an initializer.")]
    [InlineData("class Box {} return;", "return", "Can't return from top-level code.")]
    [InlineData("{ var Box; class Box {} }", "Box", "Already a variable with this name in this scope.")]
    [InlineData("class Box { method(a, a) {} }", "a", "Already a variable with this name in this scope.")]
    public void ResolutionErrorsPreventAllExecution(string source, string token, string error)
    {
        using var session = new LoxTestSession();
        session.RunApplication("print \"unreachable\";\n" + source);
        Assert.True(session.HadError);
        Assert.False(session.HadRuntimeError);
        Assert.Empty(session.Output);
        Assert.Equal($"[line 2] Error at '{token}': {error}\n", session.Errors);
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
