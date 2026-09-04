namespace SharpLox.Tool;

static class GenerateAst
{
	public static void Main(string[] args)
	{
		if (args.Length != 1)
		{
			Console.Error.WriteLine("Usage: generate_ast <output directory>");
			Environment.Exit(64);
			return;
		}

		var outputDir = args[0];

		DefineAst(outputDir, "Expr", 
			[
				"Binary		: Expr left, Token oper, Expr right",
				"Grouping	: Expr expression",
				"Literal	: object? value",
				"Unary		: Token oper, Expr right"
			]);
	}

	private static void DefineAst(
		string outputDir, string baseName,
		List<string> types)
	{
		var path = Path.Combine(outputDir, baseName + ".cs");
		using var writer = new StreamWriter(path);

		writer.WriteLine("namespace SharpLox;");
		writer.WriteLine();
		writer.WriteLine($"abstract class {baseName}");
		writer.WriteLine("{");

		DefineVisitor(writer, baseName, types);

		foreach (var type in types)
		{
			var className = type.Split(":")[0].Trim();
			var fields = type.Split(":")[1].Trim();
			DefineType(writer, baseName, className, fields);
		}

		// The base Accept() method
		writer.WriteLine();
		writer.WriteLine("  public abstract R Accept<R>(Visitor<R> visitor);");

		writer.WriteLine("}");
		writer.Close();
	}

	private static void DefineType(
		StreamWriter writer,
		string baseName,
		string className,
		string fieldList)
	{
		writer.WriteLine($"  public class {className} : {baseName}");
		writer.WriteLine("  {");

		// Constructor
		writer.WriteLine($"    public {className}({fieldList})");
		writer.WriteLine("    {");

		// Store parameters in fields
		var fields = fieldList.Split(", ");
		foreach (var field in fields)
		{
			var name = field.Split(" ")[1];
			writer.WriteLine($"      this.{name} = {name};");
		}

		writer.WriteLine("    }");
		
		// Visitor pattern
		writer.WriteLine();
		writer.WriteLine("    public override R Accept<R>(Visitor<R> visitor)");
		writer.WriteLine("    {");
		writer.WriteLine($"      return visitor.Visit{className}{baseName}(this);");
		writer.WriteLine("    }");

		// Fields
		writer.WriteLine();
		foreach (var field in fields)
		{
			writer.WriteLine($"    public readonly {field};");
		}

		writer.WriteLine("  }");
	}

	private static void DefineVisitor(
		StreamWriter writer,
		string baseName,
		List<string> types)
	{
		writer.WriteLine("  public interface Visitor<R>");
		writer.WriteLine("  {");

		foreach (var type in types)
		{
			var typeName = type.Split(":")[0].Trim();

			writer.WriteLine($"    R Visit{typeName}{baseName}({typeName} {baseName.ToLower()});");
		}

		writer.WriteLine("  }");
	}
}
