using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Compiler {
    public abstract record Stmt {

        public interface IVisitor<T> {
            T VisitBlockStmt(Block stmt);
            T VisitExpressionStmt(Expression stmt);
            T VisitIfStmt(If stmt);
            T VisitFunctionStmt(Function stmt);
            T VisitPrintStmt(Print stmt);
            T VisitReturnStmt(Return stmt);
            T VisitVarStmt(Var stmt);
            T VisitWhileStmt(While stmt);
        }

        public abstract T Accept<T>(IVisitor<T> visitor);

        public record Block(IList<Stmt> Statements) : Stmt {
            public override T Accept<T>(IVisitor<T> visitor) => visitor.VisitBlockStmt(this);
        }

        public record Expression(Expr Expr) : Stmt {
            public override T Accept<T>(IVisitor<T> visitor) => visitor.VisitExpressionStmt(this);
        }

        public record Function(Token Name, IList<Token> Params, IList<Stmt> Body) : Stmt {
            public override T Accept<T>(IVisitor<T> visitor) => visitor.VisitFunctionStmt(this); 
        }

        public record If(Expr Condition, Stmt ThenBranch, Stmt? ElseBranch) : Stmt {
            public override T Accept<T>(IVisitor<T> visitor) => visitor.VisitIfStmt(this);
        }

        public record Print(Expr Expr) : Stmt {
            public override T Accept<T>(IVisitor<T> visitor) => visitor.VisitPrintStmt(this);
        }

        public record Return(Token Keyword, Expr? Value) : Stmt {
            public override T Accept<T>(IVisitor<T> visitor) => visitor.VisitReturnStmt(this);
        }

        public record Var(Token Name, Expr? Initilizer) : Stmt {
            public override T Accept<T>(IVisitor<T> visitor) => visitor.VisitVarStmt(this);
        }

        public record While(Expr Condition, Stmt Body) : Stmt {
            public override T Accept<T>(IVisitor<T> visitor) => visitor.VisitWhileStmt(this); 
        }
    }
}
