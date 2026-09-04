namespace SharpLox;

using static TokenType;

class Scanner
{
	private readonly string source;
	private int start = 0;
	private int current = 0;
	private int line = 1;
	private readonly List<Token> tokens = [];

	private readonly Dictionary<string, TokenType> keywords = new()
	{
		["and"] = AND,
			["class"] = CLASS,
			["else"] = ELSE,
			["false"] = FALSE,
			["for"] = FOR,
			["fun"] = FUN,
			["if"] = IF,
			["nil"] = NIL,
			["or"] = OR,
			["print"] = PRINT,
			["return"] = RETURN,
			["super"] = SUPER,
			["this"] = THIS,
			["true"] = TRUE,
			["var"] = VAR,
			["while"] = WHILE,
	};

	public Scanner(string source)
	{
		this.source = source;
	}

	public List<Token> ScanTokens()
	{
		while (!IsAtEnd())
		{
			start = current;
			ScanToken();
		}

		tokens.Add(new Token(EOF, "", null, line));

		return tokens;
	}

	private bool IsAtEnd() => current >= source.Length;

	private void ScanToken()
	{
		char c = Advance();

		switch (c)
		{
			case '(': AddToken(LEFT_PAREN); break;
			case ')': AddToken(RIGHT_PAREN); break;
			case '{': AddToken(LEFT_BRACE); break;
			case '}': AddToken(RIGHT_BRACE); break;
			case ',': AddToken(COMMA); break;
			case '.': AddToken(DOT); break;
			case '-': AddToken(MINUS); break;
			case '+': AddToken(PLUS); break;
			case ';': AddToken(SEMICOLON); break;
			case '*': AddToken(STAR); break;
			case '!':
								AddToken(Match('=') ? BANG_EQUAL : BANG);
								break;
			case '=':
								AddToken(Match('=') ? EQUAL_EQUAL : EQUAL);
								break;
			case '<':
								AddToken(Match('=') ? LESS_EQUAL : LESS);
								break;
			case '>':
								AddToken(Match('=') ? GREATER_EQUAL : GREATER);
								break;
			case '/':
								if (Match('/')) 
								{
									// A comment goes until the end of the line
									while (Peek() != '\n' && !IsAtEnd()) Advance();
								}
								else
								{
									AddToken(SLASH);
								}
								break;
			case ' ':
			case '\r':
			case '\t':
								// Ignore whitespace
								break;
			case '\n':
								line++;
								break;
			case '"': String(); break;
			default:
								if (IsDigit(c))
								{
									Number();
								}
								else if (IsAlpha(c))
								{
									Identifier();
								}
								else
								{
									Lox.Error(line, "Unexpected character.");
								}
								break;
		}
	}

	private char Advance() => source[current++];

	private void AddToken(TokenType type, object? literal = null)
	{
		var text = source[start..current];

		tokens.Add(new Token(type, text, literal, line));
	}

	private bool Match(char expected)
	{
		if (IsAtEnd()) return false;
		if (source[current] != expected) return false;

		current++;
		return true;
	}

	private char Peek() => IsAtEnd() ? '\0' : source[current];

	private void String()
	{
		while (Peek() != '"' && !IsAtEnd())
		{
			if (Peek() == '\n') line++;
			Advance();
		}

		if (IsAtEnd())
		{
			Lox.Error(line, "Unterminated string.");
			return;
		}

		// The closing "
		Advance();

		// Trim the surrounding quotes
		var stringStart = start + 1;
		var stringEnd = current - 1;
		var value = source[stringStart .. stringEnd];

		AddToken(STRING, value);
	}

	private static bool IsDigit(char c) => c >= '0' && c <= '9';

	private void Number()
	{
		while (IsDigit(Peek())) Advance();

		// Look for a fractional part
		if (Peek() == '.' && IsDigit(PeekNext()))
		{
			// Consume the "."
			Advance();

			while (IsDigit(Peek())) Advance();
		}

		AddToken(NUMBER, double.Parse(source[start .. current]));
	}

	private char PeekNext() =>
		current + 1 >= source.Length
		? '\0'
		: source[current + 1];

	private static bool IsAlpha(char c) => 
		c switch 
		{
			>= 'a' and <= 'z' or >='A' and <= 'Z' or '_' => true,
				_ => false
		};

	private static bool IsAlphaNumeric(char c) =>
		IsAlpha(c) || IsDigit(c);

	private void Identifier()
	{
		while (IsAlphaNumeric(Peek())) Advance();

		var text = source[start .. current];
		TokenType type;

		if (!keywords.TryGetValue(text, out type))
		{
			type = IDENTIFIER;
		}

		AddToken(type);
	}
}
