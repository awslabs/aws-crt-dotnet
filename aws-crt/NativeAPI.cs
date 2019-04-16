/*
 * Copyright 2010-2019 Amazon.com, Inc. or its affiliates. All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 *
 *  http://aws.amazon.com/apache2.0
 *
 * or in the "license" file accompanying this file. This file is distributed
 * on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Threading;

namespace Aws.Crt {
    // Unique exception only thrown by native code when something unrecoverable happens
    public class NativeException : Exception
    {
        public NativeException(int errorCode, string errorName, string message)
            : base(message)
        {
            ErrorCode = errorCode;
            ErrorName = errorName;
        }
        public int ErrorCode { get; private set; }
        public string ErrorName { get; private set; }

        public delegate void NativeExceptionRecorder(int errorCode, string errorName, string message);
        internal delegate void SetExceptionCallback(NativeExceptionRecorder callback);
        // Called from native code as a callback, store the exception in TLS and
        // throw it when we return to CLR code
        internal static void RecordNativeException(int errorCode, string errorName, string message)
        {
            exception.Value = new NativeException(errorCode, errorName, message);
        }

        internal static void CheckNativeException()
        {
            if (exception.Value != null) {
                var ex = exception.Value;
                exception.Value = null;
                throw ex;
            }
        }

        private static ThreadLocal<NativeException> exception = new ThreadLocal<NativeException>();
    }

    public static class NativeAPI {

        private static MethodInfo GetFunction = CRT.Binding.GetType().GetMethod("GetFunction");
        // mapping of number of generic params -> method
        private static Dictionary<int, MethodInfo> MakeCallImpls = GetMakeCallImpls("MakeCall");
        private static Dictionary<int, MethodInfo> MakeVoidCallImpls = GetMakeCallImpls("MakeVoidCall");

        private static Dictionary<int, MethodInfo> GetMakeCallImpls(string name) {
            var methodInfos = Array.FindAll(typeof(NativeAPI).GetMethods(), (m) => m.Name == name);
            var impls = new Dictionary<int, MethodInfo>();
            Array.ForEach(methodInfos, (m) => impls.Add(m.GetGenericArguments().Length, m));
            return impls;
        }

        private static MethodInfo GetMakeCallImpl(Type[] argTypes) {
            MethodInfo makeCallImpl = null;
            if (argTypes[argTypes.Length - 1] == typeof(void)) {
                Array.Resize(ref argTypes, argTypes.Length - 1);
                makeCallImpl = MakeVoidCallImpls[argTypes.Length];
            } else {
                makeCallImpl = MakeCallImpls[argTypes.Length];
            }
            return makeCallImpl.IsGenericMethod ? makeCallImpl.MakeGenericMethod(argTypes) : makeCallImpl;
        }

        public static D Bind<D>() {
            return (D)BindImpl<D>(typeof(D).Name);
        }

        public static D Bind<D>(string nativeFunctionName) {
            return (D)BindImpl<D>(nativeFunctionName);
        }

        // this returns dynamic because without delegate where clauses on generic functions (C# 7.3)
        // it is not possible to cast a Delegate to a generic parameter.  The Resolve functions are
        // perfectly capable of casting a dynamic -> generic parameter though
        private static dynamic BindImpl<D>(string nativeFunctionName)
        {
            var delegateType = typeof(D);

            // resolve the native function from the CRT Binding
            var resolve = GetFunction.MakeGenericMethod(new Type[] { delegateType });
            var function = (D)resolve.Invoke(CRT.Binding, new object[] { nativeFunctionName });

            // generate a call to MakeCall<> with the right generic parameters
            var makeCallImpl = GetMakeCallImpl(GetFuncParameterTypes(delegateType));

            // call MakeCall<> to make a lambda that captures the native function
            var callImpl = (Delegate)makeCallImpl.Invoke(null, new object[] { function });

            // convert the result to the delegate type of the native function
            return Delegate.CreateDelegate(delegateType, callImpl.Target, callImpl.Method);
        }

#region MakeCall/MakeVoidCall specializations
        public static Delegate MakeVoidCall(Delegate d) {
            return new Action(() => {
                d.DynamicInvoke(null);
                NativeException.CheckNativeException();
            });
        }
        public static Delegate MakeCall<R>(Delegate d) {
            return new Func<R>(() => {
                R res = (R)d.DynamicInvoke(null);
                NativeException.CheckNativeException();
                return res;
            });
        }

        public static Delegate MakeVoidCall<T1>(Delegate d)
        {
            return new Action<T1>((a1) =>
            {
                d.DynamicInvoke(new object[] { a1 });
                NativeException.CheckNativeException();
            });
        }
        public static Delegate MakeCall<T1, R>(Delegate d)
        {
            return new Func<T1, R>((a1) =>
            {
                R res = (R)d.DynamicInvoke(new object[] { a1 });
                NativeException.CheckNativeException();
                return res;
            });
        }

        public static Delegate MakeVoidCall<T1, T2>(Delegate d) {
            return new Action<T1, T2>((a1, a2) => {
                d.DynamicInvoke(new object[] { a1, a2 });
                NativeException.CheckNativeException();
            });
        }
        public static Delegate MakeCall<T1, T2, R>(Delegate d) {
            return new Func<T1, T2, R>((a1, a2) => {
                R res = (R)d.DynamicInvoke(new object[] { a1, a2 });
                NativeException.CheckNativeException();
                return res;
            });
        }

        public static Delegate MakeVoidCall<T1, T2, T3>(Delegate d)
        {
            return new Action<T1, T2, T3>((a1, a2, a3) =>
            {
                d.DynamicInvoke(new object[] { a1, a2, a3 });
                NativeException.CheckNativeException();
            });
        }
        public static Delegate MakeCall<T1, T2, T3, R>(Delegate d)
        {
            return new Func<T1, T2, T3, R>((a1, a2, a3) =>
            {
                R res = (R)d.DynamicInvoke(new object[] { a1, a2, a3 });
                NativeException.CheckNativeException();
                return res;
            });
        }

        public static Delegate MakeVoidCall<T1, T2, T3, T4>(Delegate d)
        {
            return new Action<T1, T2, T3, T4>((a1, a2, a3, a4) =>
            {
                d.DynamicInvoke(new object[] { a1, a2, a3, a4 });
                NativeException.CheckNativeException();
            });
        }
        public static Delegate MakeCall<T1, T2, T3, T4, R>(Delegate d)
        {
            return new Func<T1, T2, T3, T4, R>((a1, a2, a3, a4) =>
            {
                R res = (R)d.DynamicInvoke(new object[] { a1, a2, a3, a4 });
                NativeException.CheckNativeException();
                return res;
            });
        }

        public static Delegate MakeVoidCall<T1, T2, T3, T4, T5>(Delegate d)
        {
            return new Action<T1, T2, T3, T4, T5>((a1, a2, a3, a4, a5) =>
            {
                d.DynamicInvoke(new object[] { a1, a2, a3, a4, a5 });
                NativeException.CheckNativeException();
            });
        }
        public static Delegate MakeCall<T1, T2, T3, T4, T5, R>(Delegate d)
        {
            return new Func<T1, T2, T3, T4, T5, R>((a1, a2, a3, a4, a5) =>
            {
                R res = (R)d.DynamicInvoke(new object[] { a1, a2, a3, a4, a5 });
                NativeException.CheckNativeException();
                return res;
            });
        }

        public static Delegate MakeVoidCall<T1, T2, T3, T4, T5, T6>(Delegate d)
        {
            return new Action<T1, T2, T3, T4, T5, T6>((a1, a2, a3, a4, a5, a6) =>
            {
                d.DynamicInvoke(new object[] { a1, a2, a3, a4, a5, a6 });
                NativeException.CheckNativeException();
            });
        }
        public static Delegate MakeCall<T1, T2, T3, T4, T5, T6, R>(Delegate d)
        {
            return new Func<T1, T2, T3, T4, T5, T6, R>((a1, a2, a3, a4, a5, a6) =>
            {
                R res = (R)d.DynamicInvoke(new object[] { a1, a2, a3, a4, a5, a6 });
                NativeException.CheckNativeException();
                return res;
            });
        }

        public static Delegate MakeVoidCall<T1, T2, T3, T4, T5, T6, T7>(Delegate d)
        {
            return new Action<T1, T2, T3, T4, T5, T6, T7>((a1, a2, a3, a4, a5, a6, a7) =>
            {
                d.DynamicInvoke(new object[] { a1, a2, a3, a4, a5, a6, a7 });
                NativeException.CheckNativeException();
            });
        }
        public static Delegate MakeCall<T1, T2, T3, T4, T5, T6, T7, R>(Delegate d)
        {
            return new Func<T1, T2, T3, T4, T5, T6, T7, R>((a1, a2, a3, a4, a5, a6, a7) =>
            {
                R res = (R)d.DynamicInvoke(new object[] { a1, a2, a3, a4, a5, a6, a7 });
                NativeException.CheckNativeException();
                return res;
            });
        }

        public static Delegate MakeVoidCall<T1, T2, T3, T4, T5, T6, T7, T8>(Delegate d)
        {
            return new Action<T1, T2, T3, T4, T5, T6, T7, T8>((a1, a2, a3, a4, a5, a6, a7, a8) =>
            {
                d.DynamicInvoke(new object[] { a1, a2, a3, a4, a5, a6, a7, a8 });
                NativeException.CheckNativeException();
            });
        }
        public static Delegate MakeCall<T1, T2, T3, T4, T5, T6, T7, T8, R>(Delegate d)
        {
            return new Func<T1, T2, T3, T4, T5, T6, T7, T8, R>((a1, a2, a3, a4, a5, a6, a7, a8) =>
            {
                R res = (R)d.DynamicInvoke(new object[] { a1, a2, a3, a4, a5, a6, a7, a8 });
                NativeException.CheckNativeException();
                return res;
            });
        }

        public static Delegate MakeVoidCall<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Delegate d)
        {
            return new Action<T1, T2, T3, T4, T5, T6, T7, T8, T9>((a1, a2, a3, a4, a5, a6, a7, a8, a9) =>
            {
                d.DynamicInvoke(new object[] { a1, a2, a3, a4, a5, a6, a7, a8, a9 });
                NativeException.CheckNativeException();
            });
        }
        public static Delegate MakeCall<T1, T2, T3, T4, T5, T6, T7, T8, T9, R>(Delegate d)
        {
            return new Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, R>((a1, a2, a3, a4, a5, a6, a7, a8, a9) =>
            {
                R res = (R)d.DynamicInvoke(new object[] { a1, a2, a3, a4, a5, a6, a7, a8, a9 });
                NativeException.CheckNativeException();
                return res;
            });
        }

        public static Delegate MakeVoidCall<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Delegate d)
        {
            return new Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>((a1, a2, a3, a4, a5, a6, a7, a8, a9, a10) =>
            {
                d.DynamicInvoke(new object[] { a1, a2, a3, a4, a5, a6, a7, a8, a9, a10 });
                NativeException.CheckNativeException();
            });
        }
        public static Delegate MakeCall<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, R>(Delegate d)
        {
            return new Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, R>((a1, a2, a3, a4, a5, a6, a7, a8, a9, a10) =>
            {
                R res = (R)d.DynamicInvoke(new object[] { a1, a2, a3, a4, a5, a6, a7, a8, a9, a10 });
                NativeException.CheckNativeException();
                return res;
            });
        }

        public static Delegate MakeVoidCall<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(Delegate d)
        {
            return new Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>((a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11) =>
            {
                d.DynamicInvoke(new object[] { a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11 });
                NativeException.CheckNativeException();
            });
        }
        public static Delegate MakeCall<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, R>(Delegate d)
        {
            return new Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, R>((a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11) =>
            {
                R res = (R)d.DynamicInvoke(new object[] { a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11 });
                NativeException.CheckNativeException();
                return res;
            });
        }

        public static Delegate MakeVoidCall<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(Delegate d)
        {
            return new Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>((a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12) =>
            {
                d.DynamicInvoke(new object[] { a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12 });
                NativeException.CheckNativeException();
            });
        }
        public static Delegate MakeCall<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, R>(Delegate d)
        {
            return new Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, R>((a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12) =>
            {
                R res = (R)d.DynamicInvoke(new object[] { a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12 });
                NativeException.CheckNativeException();
                return res;
            });
        }

        public static Delegate MakeVoidCall<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(Delegate d)
        {
            return new Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>((a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13) =>
            {
                d.DynamicInvoke(new object[] { a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13 });
                NativeException.CheckNativeException();
            });
        }
        public static Delegate MakeCall<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, R>(Delegate d)
        {
            return new Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, R>((a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13) =>
            {
                R res = (R)d.DynamicInvoke(new object[] { a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13 });
                NativeException.CheckNativeException();
                return res;
            });
        }

        public static Delegate MakeVoidCall<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(Delegate d)
        {
            return new Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>((a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14) =>
            {
                d.DynamicInvoke(new object[] { a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14 });
                NativeException.CheckNativeException();
            });
        }
        public static Delegate MakeCall<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, R>(Delegate d)
        {
            return new Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, R>((a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14) =>
            {
                R res = (R)d.DynamicInvoke(new object[] { a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14 });
                NativeException.CheckNativeException();
                return res;
            });
        }

        public static Delegate MakeVoidCall<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(Delegate d)
        {
            return new Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>((a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15) =>
            {
                d.DynamicInvoke(new object[] { a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15 });
                NativeException.CheckNativeException();
            });
        }
        public static Delegate MakeCall<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, R>(Delegate d)
        {
            return new Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, R>((a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15) =>
            {
                R res = (R)d.DynamicInvoke(new object[] { a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15 });
                NativeException.CheckNativeException();
                return res;
            });
        }

        public static Delegate MakeVoidCall<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(Delegate d)
        {
            return new Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>((a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16) =>
            {
                d.DynamicInvoke(new object[] { a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16 });
                NativeException.CheckNativeException();
            });
        }
        public static Delegate MakeCall<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, R>(Delegate d)
        {
            return new Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, R>((a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16) =>
            {
                R res = (R)d.DynamicInvoke(new object[] { a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16 });
                NativeException.CheckNativeException();
                return res;
            });
        }
#endregion

        // Returns just the argument types for a given delegate type
        private static Type[] GetDelegateParameterTypes(Type dt) {
            var paramInfos = dt.GetMethod("Invoke").GetParameters();
            var paramTypes = Array.ConvertAll<ParameterInfo, Type>(paramInfos, (p) => p.ParameterType);
            return paramTypes;
        }

        // Returns types in the format of System.Func: [args..., return type]
        private static Type[] GetFuncParameterTypes(Type dt) {
            var paramTypes = GetDelegateParameterTypes(dt);
            var funcTypeList = new List<Type>(paramTypes);
            funcTypeList.Add(dt.GetMethod("Invoke").ReturnType);
            return funcTypeList.ToArray();
        }
    }
}
