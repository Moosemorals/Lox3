using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Compiler {
    public abstract record Stmt {

        public interface IVisitor<T> {
            T VisitBlockStmt(Block stmt);
            T VisitExpressionStmt(Expression stmt);
            T VisitPrintStmt(Print stmt);
            T VisitVarStmt(Var stmt);
        }

        public abstract T Accept<T>(IVisitor<T> visitor);

        public record Block(IList<Stmt> Statements) : Stmt {
            public override T Accept<T>(IVisitor<T> visitor) => visitor.VisitBlockStmt(this);
        }

        public record Expression(Expr Expr) : Stmt {
            public override T Accept<T>(IVisitor<T> visitor) => visitor.VisitExpressionStmt(this);
        }

        public record Print(Expr Expr) : Stmt {
            public override T Accept<T>(IVisitor<T> visitor) => visitor.VisitPrintStmt(this);
        }

        public record Var(Token Name, Expr? Initilizer) : Stmt {
            public override T Accept<T>(IVisitor<T> visitor) => visitor.VisitVarStmt(this);
        }
    }
}
