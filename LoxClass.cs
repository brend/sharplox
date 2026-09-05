namespace SharpLox;

sealed class LoxClass
{
    public string Name { get; }

    public LoxClass(string name)
    {
        Name = name;
    }

    public override string ToString() => Name;
}