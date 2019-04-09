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
using System.Runtime.InteropServices;

namespace Aws.CRT {
    // Unique exceptiononly thrown by native code when something unrecoverable happens
    public class NativeException : Exception
    {
        public NativeException(string message)
            : base(message)
        {
        }

        internal delegate void NativeExceptionThrower(string message);
        internal delegate void SetExceptionCallback(NativeExceptionThrower callback);
        internal static void ThrowNativeException(string message)
        {
            throw new NativeException(message);
        }
    }

    public static class NativeAPI {

        static MethodInfo GetFunction = CRT.Binding.GetType().GetMethod("GetFunction");

        public static T Resolve<T>()
            where T : new()
        {
            T api = new T();
            FieldInfo[] fields = typeof(T).GetFields();
            foreach (FieldInfo field in fields)
            {
                if (field.FieldType.BaseType.IsSubclassOf(typeof(System.Delegate)))
                {
                    MethodInfo resolveFunction = GetFunction.MakeGenericMethod(new Type[] { field.FieldType });
                    Delegate function = (Delegate)resolveFunction.Invoke(CRT.Binding, new object[] { field.FieldType.Name });
                    field.SetValue(api, function);
                }
            }

            return api;
        }
    }
}