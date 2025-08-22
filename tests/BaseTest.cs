/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
 */
using System;
using Xunit;

using Aws.Crt;
using Aws.Crt.Auth;

namespace tests
{
    public class BaseTest : IDisposable
    {
        // Class member to store the initial memory usage
        private int initialMemoryUsage;

        public BaseTest()
        {
            // This constructor will be called before each test case runs
            Console.WriteLine("Test case starting, performing setup...");

            // Add any setup code here
            // For example: initialize resources, set up test environment, etc.

            // Force garbage collection before the test to ensure a clean state
            GC.Collect();

            // Record the initial memory usage
            initialMemoryUsage = Aws.Crt.Auth.AwsSigner.GetMem();
            Console.WriteLine($"Initial memory usage: {initialMemoryUsage}");
        }

        public void Dispose()
        {
            // This method will be called after each test case runs
            Console.WriteLine("Test case completed, performing cleanup...");

            // Force garbage collection to ensure proper cleanup
            GC.Collect();

            // Get the current memory usage after the test
            int currentMemoryUsage = Aws.Crt.Auth.AwsSigner.GetMem();
            Console.WriteLine($"Final memory usage: {currentMemoryUsage}");

            // Check if memory usage has increased
            if (currentMemoryUsage > initialMemoryUsage)
            {
                Console.WriteLine($"Memory leak detected! Initial: {initialMemoryUsage}, Final: {currentMemoryUsage}, Difference: {currentMemoryUsage - initialMemoryUsage}");
                // Invoke MemDump to get detailed memory information
                Aws.Crt.Auth.AwsSigner.MemDump();
                // Fail the test
                Assert.True(false, $"Memory leak detected: Initial: {initialMemoryUsage}, Final: {currentMemoryUsage}, Difference: {currentMemoryUsage - initialMemoryUsage}");
            }
        }
    }
}
