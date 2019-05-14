using System;
using System.Reflection;
using UnityEngine;

namespace R2API.Utils {
    // ReSharper disable once InconsistentNaming
    public static class ILHelper {
        public static void PrintMethods(this Type T) {
            foreach (var method in T.GetMethods()) {
                Debug.Log("M: " + method.Name);
            }

            foreach (var method in T.GetRuntimeMethods()) {
                Debug.Log("RM: " + method.Name);
            }
        }
    }
}
