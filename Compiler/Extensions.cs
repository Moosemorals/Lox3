using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler {
    internal static class Extensions {

        internal static bool IsEmpty<T>(this Stack<T> stack) {
            return stack.Count == 0;
        }
    }
}
