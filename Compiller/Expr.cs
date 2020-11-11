using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Compiler {
    public abstract record Expr {

        public interface IVisitor<T> {
            T VisitBinaryExpr(Binary expr);
            T VisitGroupingExpr(Grouping expr);
            T VisitLiteralExpr(Literal expr);
            T VisitUnaryExpr(Unary expr);

        }

        public abstract T Accept<T>(IVisitor<T> visitor);

        public record Binary(Expr Left, Token Op, Expr Right) : Expr {
            public override T Accept<T>(IVisitor<T> visitor) => visitor.VisitBinaryExpr(this);
        }

        public record Grouping(Expr Expression) : Expr {
            public override T Accept<T>(IVisitor<T> visitor) => visitor.VisitGroupingExpr(this);
        }

        public record Literal(object? Value) : Expr {
            public override T Accept<T>(IVisitor<T> visitor) => visitor.VisitLiteralExpr(this);
        }

        public record Unary(Token Op, Expr Right) : Expr {
            public override T Accept<T>(IVisitor<T> visitor) => visitor.VisitUnaryExpr(this);
        }
    }
}
