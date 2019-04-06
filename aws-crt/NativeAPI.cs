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
using System.Reflection;
using System.Runtime.InteropServices;

namespace Aws.CRT {
    public static class NativeAPI {
        public static T Resolve<T>() 
            where T: new() 
        {
            T api = new T();
            Type type = typeof(T);
            FieldInfo[] fields = type.GetFields();
            MethodInfo getFunction = CRT.Binding.GetType().GetMethod("GetFunction");
            foreach (FieldInfo field in fields) {
                if (field.FieldType.BaseType.IsSubclassOf(typeof(System.Delegate))) {
                    Type[] args = new Type[]{typeof(IntPtr)};
                    var resolveFunction = getFunction.MakeGenericMethod(new Type[] { field.FieldType });
                    var function = resolveFunction.Invoke(CRT.Binding, new object[] {field.FieldType.Name});
                    field.SetValue(api, function);
                }
            }

            return api;
        }
    }
}