namespace SharpLox;

static class Lox
{
	private static bool hadError = false;

	public static void Main(string[] args)
	{
		switch (args.Length)
		{
			case 0:
				RunPrompt();
				return;
			case 1:
				RunFile(args[0]);
				return;
			default:
				Console.Error.WriteLine("Usage: sharplox [script]");
				Environment.Exit(64);
				return;

		}
	}

	private static void RunFile(string file)
	{
		var script = File.ReadAllText(file);
		Run(script);

		if (hadError)
		{
			Environment.Exit(65);
		}
	}

	private static void RunPrompt()
	{
		for (;;) {
			Console.Write("> ");
			if (Console.ReadLine() is not string line)
			{
				break;
			}
			Run(line);
			hadError = false;
		}
	}

	private static void Run(String source)
	{
		var scanner = new Scanner(source);
		var tokens = scanner.ScanTokens();

		foreach (var token in tokens)
		{
			Console.WriteLine(token);
		}
	}

	public static void Error(int line, string message)
	{
		Report(line, "", message);
		}

	private static void Report(int line, string where, string message)
	{
		Console.Error.WriteLine($"[line {line}] Error{where}: {message}");
	}
}
