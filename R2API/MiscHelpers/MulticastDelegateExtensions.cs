using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MonoMod.Cil;

namespace R2API.MiscHelpers {
    public delegate T Modifier<T>(T input);
    internal static class MulticastDelegateExtensions {
        public static T InvokeSequential<T>(this Modifier<T> func, T initialValue, Boolean skipErrors = false) {
            var invList = func.GetInvocationList();
            if(invList is null || invList.Length <= 1) {
                return func(initialValue);
            }

            foreach(var v in invList.Where(a => a is Modifier<T>).Cast<Modifier<T>>()) {
                try {
                    initialValue = v(initialValue);
                } catch(Exception e) {
                    if(!skipErrors) throw e; else R2API.Logger.LogError(e);
                }
                
            }

            return initialValue;
        }

        public static TOut InvokeSequential<TIn, TOut>(this Func<TIn, TOut> func, TIn initialValue, Func<TOut, TIn> inBetween, Boolean skipErrors = false) {
            var invList = func.GetInvocationList();
            if(invList is null || invList.Length <= 1) {
                return func(initialValue);
            }

            TOut result = default;
            Boolean first = true;
            foreach(var v in invList.Where(a => a is Func<TIn, TOut>).Cast<Func<TIn, TOut>>()) {
                if(first) {
                    try {
                        result = v(initialValue);
                        first = false;
                    } catch(Exception e) {
                        if(!skipErrors) throw e; else R2API.Logger.LogError(e);
                    }           
                    continue;
                }

                result = v(inBetween(result));
            }
            return result;
        }
    }
}
