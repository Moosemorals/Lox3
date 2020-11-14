using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler {

    public interface ILoxIndexable {
        public LoxValue GetAt(int index);
        public LoxValue GetAt(double index) => GetAt((int)index);
        public int Length {get; }
    }

    public abstract record LoxValue {

        public static readonly NilType Nil = new NilType();

        public static readonly Bool True = new Bool(true);
        public static readonly Bool False = new Bool(false);

        public record NilType : LoxValue {
            public override string ToString() {
                return "nil";
            }
        };

        public record Bool(bool Value) : LoxValue {
            public override string ToString() {
                return Value.ToString();
            }
        }


        public record Number(double Value) : LoxValue {
            public static Number operator +(Number a, Number b) => new Number(a.Value + b.Value);
            public static Number operator -(Number a, Number b) => new Number(a.Value - b.Value);
            public static Number operator *(Number a, Number b) => new Number(a.Value * b.Value);
            public static Number operator /(Number a, Number b) => new Number(a.Value / b.Value);
            public static Bool operator >=(Number a, Number b) => new Bool(a.Value >= b.Value);
            public static Bool operator >(Number a, Number b) => new Bool(a.Value > b.Value);
            public static Bool operator <(Number a, Number b) => new Bool(a.Value < b.Value);
            public static Bool operator <=(Number a, Number b) => new Bool(a.Value <= b.Value);
            public override string ToString() {
                return Value.ToString();
            }
        }

        public record String(string Value) : LoxValue, ILoxIndexable {
            public static String operator +(String a, String b) => new String(string.Concat(a, b));
            public override string ToString() {
                return Value.ToString();
            }

            public LoxValue GetAt(int index) => New(Value[index].ToString());

            public int Length => Value.Length;
        }

        public record Function(LoxFunction Value) : LoxValue {
            public override string ToString() {
                return Value.ToString();
            }
        };

        public static LoxValue New(object? value) {
            switch (value) {

            case bool b:
                return b ? True : False;
            case double d:
                return new Number(d);
            case string s:
                return new String(s);
            case LoxFunction f:
                return new Function(f);
            default:
                return Nil;
            }


        }

    }
}
