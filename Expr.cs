namespace SharpLox;

abstract class Expr
{
  public interface Visitor<R>
  {
    R VisitAssignExpr(Assign expr);
    R VisitBinaryExpr(Binary expr);
    R VisitGroupingExpr(Grouping expr);
    R VisitLiteralExpr(Literal expr);
    R VisitUnaryExpr(Unary expr);
    R VisitVariableExpr(Variable expr);
  }
  public class Assign : Expr
  {
    public Assign(Token name, Expr value)
    {
      this.name = name;
      this.value = value;
    }

    public override R Accept<R>(Visitor<R> visitor)
    {
      return visitor.VisitAssignExpr(this);
    }

    public readonly Token name;
    public readonly Expr value;
  }
  public class Binary : Expr
  {
    public Binary(Expr left, Token oper, Expr right)
    {
      this.left = left;
      this.oper = oper;
      this.right = right;
    }

    public override R Accept<R>(Visitor<R> visitor)
    {
      return visitor.VisitBinaryExpr(this);
    }

    public readonly Expr left;
    public readonly Token oper;
    public readonly Expr right;
  }
  public class Grouping : Expr
  {
    public Grouping(Expr expression)
    {
      this.expression = expression;
    }

    public override R Accept<R>(Visitor<R> visitor)
    {
      return visitor.VisitGroupingExpr(this);
    }

    public readonly Expr expression;
  }
  public class Literal : Expr
  {
    public Literal(object? value)
    {
      this.value = value;
    }

    public override R Accept<R>(Visitor<R> visitor)
    {
      return visitor.VisitLiteralExpr(this);
    }

    public readonly object? value;
  }
  public class Unary : Expr
  {
    public Unary(Token oper, Expr right)
    {
      this.oper = oper;
      this.right = right;
    }

    public override R Accept<R>(Visitor<R> visitor)
    {
      return visitor.VisitUnaryExpr(this);
    }

    public readonly Token oper;
    public readonly Expr right;
  }
  public class Variable : Expr
  {
    public Variable(Token name)
    {
      this.name = name;
    }

    public override R Accept<R>(Visitor<R> visitor)
    {
      return visitor.VisitVariableExpr(this);
    }

    public readonly Token name;
  }

  public abstract R Accept<R>(Visitor<R> visitor);
}
