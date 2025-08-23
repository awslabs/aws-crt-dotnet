/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
 */
using System;
using Xunit;

using Aws.Crt;

namespace tests
{
    public class BaseTest : IDisposable
    {
        // Class member to store the initial memory usage
        private UInt64 initialMemoryUsage;

        public BaseTest()
        {
            // Force garbage collection before the test to ensure a clean state
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.GetTotalMemory(true);

            // Record the initial memory usage
            initialMemoryUsage = Aws.Crt.CRT.GetNativeMem();
        }

        public void Dispose()
        {
            // Collect all generations of memory.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.GetTotalMemory(true);
            // Wait for native threads to join.
            Aws.Crt.CRT.JoinThreads();

            // Get the current memory usage after the test
            UInt64 currentMemoryUsage = Aws.Crt.CRT.GetNativeMem();

            // Check if memory usage has increased
            if (currentMemoryUsage > initialMemoryUsage)
            {
                Console.WriteLine($"Memory leak detected! Initial: {initialMemoryUsage}, Final: {currentMemoryUsage}, Difference: {currentMemoryUsage - initialMemoryUsage}");
                // Invoke MemDump to get detailed memory information
                Aws.Crt.CRT.NativeMemDump();
                // Fail the test
                Assert.True(false, $"Memory leak detected: Initial: {initialMemoryUsage}, Final: {currentMemoryUsage}, Difference: {currentMemoryUsage - initialMemoryUsage}");
            }
        }
    }
}
