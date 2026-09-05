namespace SharpLox.Tests;

using SharpLox;
using Xunit;

using static TokenType;

[Collection("Lox console")]
public class ScannerTests
{
	[Fact]
	public void ScanTokens_ScansSingleAndDoubleCharacterTokens()
	{
		var tokens = Scan("(){},.-+;*! != = == < <= > >= /");

		AssertTokenTypes(tokens,
			LEFT_PAREN, RIGHT_PAREN, LEFT_BRACE, RIGHT_BRACE,
			COMMA, DOT, MINUS, PLUS, SEMICOLON, STAR,
			BANG, BANG_EQUAL,
			EQUAL, EQUAL_EQUAL,
			LESS, LESS_EQUAL,
			GREATER, GREATER_EQUAL,
			SLASH,
			EOF);
	}

	[Fact]
	public void ScanTokens_SkipsWhitespaceAndLineComments()
	{
		var tokens = Scan("// comment\nvar x = 123;\n  print x;");

		AssertTokenTypes(tokens,
			VAR, IDENTIFIER, EQUAL, NUMBER, SEMICOLON,
			PRINT, IDENTIFIER, SEMICOLON,
			EOF);

		Assert.Equal(2, tokens[0].line);
		Assert.Equal(2, tokens[4].line);
		Assert.Equal(3, tokens[5].line);
		Assert.Equal(3, tokens[^1].line);
	}

	[Fact]
	public void ScanTokens_ScansStringLiterals()
	{
		var tokens = Scan("print \"hello, lox\";");

		AssertTokenTypes(tokens, PRINT, STRING, SEMICOLON, EOF);
		Assert.Equal("\"hello, lox\"", tokens[1].lexeme);
		Assert.Equal("hello, lox", tokens[1].literal);
	}

	[Fact]
	public void ScanTokens_AllowsMultilineStringsAndTracksEndingLine()
	{
		var tokens = Scan("\"first\nsecond\"");

		AssertTokenTypes(tokens, STRING, EOF);
		Assert.Equal("first\nsecond", tokens[0].literal);
		Assert.Equal(2, tokens[0].line);
		Assert.Equal(2, tokens[1].line);
	}

	[Fact]
	public void ScanTokens_ScansNumbersOnlyWhenDotHasTrailingDigit()
	{
		var tokens = Scan("123 45.67 89.");

		AssertTokenTypes(tokens, NUMBER, NUMBER, NUMBER, DOT, EOF);
		Assert.Equal(123d, tokens[0].literal);
		Assert.Equal(45.67d, tokens[1].literal);
		Assert.Equal(89d, tokens[2].literal);
		Assert.Equal("89", tokens[2].lexeme);
		Assert.Equal(".", tokens[3].lexeme);
	}

	[Fact]
	public void ScanTokens_ScansKeywordsAndIdentifiersWithMaximalMunch()
	{
		var tokens = Scan("and orchid className class _name name123");

		AssertTokenTypes(tokens, AND, IDENTIFIER, IDENTIFIER, CLASS, IDENTIFIER, IDENTIFIER, EOF);
		Assert.Equal("orchid", tokens[1].lexeme);
		Assert.Equal("className", tokens[2].lexeme);
		Assert.Equal("_name", tokens[4].lexeme);
		Assert.Equal("name123", tokens[5].lexeme);
	}

	[Fact]
	public void ScanTokens_ReportsUnexpectedCharactersAndContinues()
	{
		var error = new StringWriter();
		var originalError = Console.Error;

		try
		{
			Console.SetError(error);

			var tokens = Scan("@var");

			AssertTokenTypes(tokens, VAR, EOF);
			Assert.Contains("[line 1] Error: Unexpected character.", error.ToString());
		}
		finally
		{
			Console.SetError(originalError);
		}
	}

	[Theory]
	[InlineData("@", "Unexpected character.")]
	[InlineData("\"unterminated", "Unterminated string.")]
	public void ScanTokens_MarksLoxAsHavingHadAnErrorWhenScannerReportsAnError(string source, string message)
	{
		var error = new StringWriter();
		var originalError = Console.Error;

		try
		{
			SetHadError(false);
			Console.SetError(error);

			Scan(source);

			Assert.True(GetHadError());
			Assert.Contains(message, error.ToString());
		}
		finally
		{
			Console.SetError(originalError);
			SetHadError(false);
		}
	}

	private static List<Token> Scan(string source) =>
		new Scanner(source).ScanTokens();

	private static void AssertTokenTypes(List<Token> tokens, params TokenType[] types) =>
		Assert.Equal(types, tokens.Select(token => token.type));

	private static bool GetHadError() =>
		(bool)HadErrorField.GetValue(null)!;

	private static void SetHadError(bool value) =>
		HadErrorField.SetValue(null, value);

	private static readonly System.Reflection.FieldInfo HadErrorField =
		typeof(Lox).GetField("hadError", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
		?? throw new InvalidOperationException("Could not find Lox.hadError.");
}
