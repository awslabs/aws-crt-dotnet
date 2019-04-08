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
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace Aws.CRT {
    // Unique exceptiononly thrown by native code when something unrecoverable happens
    public class NativeException : Exception
    {
        public NativeException()
            // record this thread's exception message
            : base(exceptionMessage.Value)
        {
            // then reset this thread's exception message
            exceptionMessage.Value = null;
        }

        internal delegate void NativeExceptionThrower(string message);
        internal delegate void SetExceptionCallback(NativeExceptionThrower callback);
        internal static void RecordNativeException(string message)
        {
            exceptionMessage.Value = message;
            // HACK until we can get injection working
            ThrowNativeException();
        }
        internal static void ThrowNativeException()
        {
            if (exceptionMessage.Value != null)
            {
                throw new NativeException();
            }
        }
        private static ThreadLocal<string> exceptionMessage;
    }

    internal static class NativeAPI {

        static MethodInfo GetFunction = CRT.Binding.GetType().GetMethod("GetFunction");

#if true
        public static T Resolve<T>()
            where T : new()
        {
            T api = new T();
            FieldInfo[] fields = typeof(T).GetFields();
            foreach (FieldInfo field in fields)
            {
                if (field.FieldType.BaseType.IsSubclassOf(typeof(System.Delegate)))
                {
                    Type[] args = new Type[] { typeof(IntPtr) };
                    MethodInfo resolveFunction = GetFunction.MakeGenericMethod(new Type[] { field.FieldType });
                    Delegate function = (Delegate)resolveFunction.Invoke(CRT.Binding, new object[] { field.FieldType.Name });
                    field.SetValue(api, function);
                }
            }

            return api;
        }

#else // experimental injection of exception handler
        static MethodInfo[] WrapNativeVariants = Array.FindAll(
            typeof(NativeAPI).GetMethods(BindingFlags.Public | BindingFlags.Static),
            m => m.Name == "WrapNative");

        private static Delegate GetWrappedMethod(Type delegateType, Type[] delegateParams, Delegate targetFunction) {
            MethodInfo wrapNativeGeneric = Array.Find(WrapNativeVariants, w => w.GetParameters()[0].ParameterType.Name == delegateType.Name);
            MethodInfo wrapNative = wrapNativeGeneric.MakeGenericMethod(delegateParams);
            Delegate funcOrAction = Delegate.CreateDelegate(delegateType, targetFunction.Target, targetFunction.Method);
            return (Delegate)wrapNative.Invoke(null, new object[] { funcOrAction }); ;
        }
        public static T Resolve<T>() 
            where T: new() 
        {
            T api = new T();
            FieldInfo[] fields = typeof(T).GetFields();
            foreach (FieldInfo field in fields) {
                if (field.FieldType.BaseType.IsSubclassOf(typeof(System.Delegate))) {
                    Type[] args = new Type[]{typeof(IntPtr)};
                    MethodInfo resolveFunction = GetFunction.MakeGenericMethod(new Type[] { field.FieldType });
                    Delegate function = (Delegate)resolveFunction.Invoke(CRT.Binding, new object[] {field.FieldType.Name});
                    MethodInfo functionInfo = function.GetMethodInfo();
                    ParameterInfo[] paramInfos = functionInfo.GetParameters();
                    Type[] paramTypes = Array.ConvertAll<ParameterInfo, Type>(paramInfos, p => p.ParameterType);
                    Type returnType = functionInfo.ReturnType;
                    Type delegateType;
                    Type[] delegateParams; 
                    if (returnType == typeof(void)) {
                        Type actionType = typeof(Action<>).MakeGenericType(paramTypes);
                        delegateType = actionType;
                        delegateParams = paramTypes;
                    } else {
                        var funcParamsList = new List<Type>(paramTypes);
                        funcParamsList.Add(functionInfo.ReturnType);
                        delegateParams = funcParamsList.ToArray();
                        Type funcType = typeof(Func<,>).MakeGenericType(delegateParams);
                        delegateType = funcType;
                    }

                    Delegate wrapper = GetWrappedMethod(delegateType, delegateParams, function);
                    var wrapperAsDelegate = Delegate.CreateDelegate(field.FieldType, wrapper.Target, wrapper.Method);
                    field.SetValue(api, wrapperAsDelegate);
                }
            }

            return api;
        }

        public static Action WrapNative(Action func)
        {
            return () =>
            {
                func();
                NativeException.ThrowNativeException();
            };
        }

        public static Action<T1> WrapNative<T1>(Action<T1> func)
        {
            return (a1) =>
            {
                func(a1);
                NativeException.ThrowNativeException();
            };
        }

        public static Func<R> WrapNative<R>(Func<R> func)
        {
            return () =>
            {
                var res = func();
                NativeException.ThrowNativeException();
                return res;
            };
        }

        public static Func<T1, R> WrapNative<T1, R>(Func<T1, R> func)
        {
            return (a1) =>
            {
                // var res = func(a1);
                // NativeException.ThrowNativeException();
                // return res;
                return func(a1);
            };
        }

        public static Func<T1, T2, R> WrapNative<T1, T2, R>(Func<T1, T2, R> func)
        {
            return (a1, a2) =>
            {
                var res = func(a1, a2);
                NativeException.ThrowNativeException();
                return res;
            };
        }
        public static Func<T1, T2, T3, R> WrapNative<T1, T2, T3, R>(Func<T1, T2, T3, R> func)
        {
            return (a1, a2, a3) =>
            {
                var res = func(a1, a2, a3);
                NativeException.ThrowNativeException();
                return res;
            };
        }
#endif
    }
}