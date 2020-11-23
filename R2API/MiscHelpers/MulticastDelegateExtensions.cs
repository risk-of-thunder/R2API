using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MonoMod.Cil;

namespace R2API.MiscHelpers {
    internal static class MulticastDelegateExtensions {
        public static T InvokeSequential<T>(this Func<T, T> func, T initialValue) {
            var invList = func.GetInvocationList();
            if(invList is null || invList.Length <= 1) {
                return func(initialValue);
            }

            foreach(var v in invList.Where(a => a is Func<T, T>).Cast<Func<T, T>>()) {
                initialValue = v(initialValue);
            }

            return initialValue;
        }

        public static TOut InvokeSequential<TIn, TOut>(this Func<TIn, TOut> func, TIn initialValue, Func<TOut, TIn> inBetween) {
            var invList = func.GetInvocationList();
            if(invList is null || invList.Length <= 1) {
                return func(initialValue);
            }

            TOut result = default;
            Boolean first = true;
            foreach(var v in invList.Where(a => a is Func<TIn, TOut>).Cast<Func<TIn, TOut>>()) {
                if(first) {
                    result = v(initialValue);
                    first = false;
                    continue;
                }

                result = v(inBetween(result));
            }
            return result;
        }
    }
}
