namespace SharpLox;

record Token(
		TokenType type,
		string lexeme,
		object? literal,
		int line)
{
	public override string ToString() => $"{type} {lexeme} {literal}";
}
