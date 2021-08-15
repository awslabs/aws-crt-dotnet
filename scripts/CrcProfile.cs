/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
 */

using System;
using System.IO;
using Aws.Crt.Checksums;
using System.Text;
// void crcProfileSize(int size, string profile_name, string fn_name, Func<byte[], uint, uint> checksumFn){
//     Console.WriteLine($"********************* {fn_name} Profile {profile_name} ************************************\n\n");
//     Random rnd = new Random();
//     byte[] buffer = new byte[size];
//     rnd.NextBytes(buffer);
//     Console.WriteLine("****** 128 byte chunks ******");
//     crcProfileChunks(buffer, 128, false, checksumFn);
//     crcProfileChunks(buffer, 128, true, checksumFn);
//     Console.WriteLine("****** 256 byte chunks ******");
//     crcProfileChunks(buffer, 256, false, checksumFn);
//     crcProfileChunks(buffer, 128, true, checksumFn);
//     Console.WriteLine("****** 512 byte chunks ******");
//     crcProfileChunks(buffer, 512, false, checksumFn);
//     crcProfileChunks(buffer, 128, true, checksumFn);
//     Console.WriteLine("******** oneshot run ********");
//     checksumFn(buffer, 0);
//     long start = DateTime.Now.Ticks;
//     checksumFn(buffer, 0);
//     long end = DateTime.Now.Ticks;
//     Console.WriteLine($"CRC streaming computation took {(end - start) * 100} ns\n");
// }

// void crcProfileChunks(byte[] buffer, int chunk_size, bool print, Func<byte[], uint, uint> checksumFn){
//     int i = 0;
//     uint prev = 0;
//     long start = DateTime.Now.Ticks;
//     while(i + chunk_size < buffer.Length){
//         prev = checksumFn(buffer[i..(i + chunk_size)], prev);
//         i = i + chunk_size;
//     }
//     prev = checksumFn(buffer[i..buffer.Length], prev);
//     long end = DateTime.Now.Ticks;
//     if (print) {
//         Console.WriteLine($"CRC streaming computation took {(end - start) * 100} ns\n");
//     }
// }

// Console.WriteLine("Starting profile run for Crc32 using implementation \n\n");
// crcProfileSize(1024, "1 KB", "CRC32", Crc.crc32);
// crcProfileSize(1024 * 64, "64 KB", "CRC32", Crc.crc32);
// crcProfileSize(1024 * 128, "128 KB", "CRC32", Crc.crc32);
// crcProfileSize(1024 * 512, "512 KB", "CRC32", Crc.crc32);
// Console.WriteLine("Starting profile run for Crc32C using implementation \n\n");
// crcProfileSize(1024, "1 KB", "CRC32C", Crc.crc32c);
// crcProfileSize(1024 * 64, "64 KB", "CRC32C", Crc.crc32c);
// crcProfileSize(1024 * 128, "128 KB", "CRC32C", Crc.crc32c);
// crcProfileSize(1024 * 512, "512 KB", "CRC32C", Crc.crc32c);

// Welfords online algorithm
void update_summary(long count, ref double mean, ref double M2, ref double my_min, ref double my_max, double new_value) {
    double delta = new_value - mean;
    mean += delta / count;
    double delta2 = new_value - mean;
    M2 += delta * delta2;
    my_min = Math.Min(my_min, new_value);
    my_max = Math.Max(my_max, new_value);
}

double finalize_summary(uint count, double M2) {
    return M2 / count;
}

void print_stats(double[] means, double [] variances, double[] mins, double[] maxs, uint[] chunk_sizes){
    for (int i = 0; i < means.Length; i++){
        Console.WriteLine($"chunk size: {chunk_sizes[i]}, min: {mins[i]}, max: {maxs[i]}, mean: {means[i]}, variance: {variances[i]}");
    }
}


double profile_sequence_chunks(byte[] to_hash, uint chunk_size, uint iterations, Func<byte[], uint, uint> checksum_fn){
    double mean = 0;
    double M2 = 0;
    double min = Double.MaxValue;
    double max = 0;
    for (int x = 0; x < iterations; x++){
        uint i = 0;
        uint prev = 0;
        byte[] buffer = new  byte[chunk_size];
        long start = DateTime.Now.Ticks;
        while(i + chunk_size < to_hash.Length){
            Array.Copy(to_hash, i, buffer, 0, chunk_size);
            prev = checksum_fn(buffer, prev);
            i = i + chunk_size;
        }
        long remainder = to_hash.Length - i;
        buffer = new byte[remainder];
        Array.Copy(to_hash, i, buffer, 0, remainder);
        prev = checksum_fn(buffer, prev);
        long end =  DateTime.Now.Ticks;
        update_summary(x + 1, ref mean, ref M2, ref min, ref max, (end - start) * 100);
    }
    return mean;
}

double[] profile_sequence(byte[] to_hash, uint[] chunk_sizes, uint iterations_per_sequence, Func<byte[], uint, uint> checksum_fn){
    double[] times = new double[chunk_sizes.Length];
    for(uint i = 0; i < chunk_sizes.Length; i++) {
        double toss = profile_sequence_chunks(to_hash, chunk_sizes[i], iterations_per_sequence, checksum_fn);
        times[i] = (profile_sequence_chunks(to_hash, chunk_sizes[i], iterations_per_sequence, checksum_fn));
    }
    return times;
}

void profile(long size, uint[] chunk_sizes, uint num_sequences, uint iterations_per_sequence, Func<byte[], uint, uint> checksum_fn){
    double[] means = new double[chunk_sizes.Length];
    double [] variances = new double[chunk_sizes.Length];
    double[] mins = new double[chunk_sizes.Length];
    for (int i = 0; i < mins.Length; i++){
        mins[i] = Double.MaxValue;
    }
    double[] maxs = new double[chunk_sizes.Length];
    Random rnd = new Random();
    for(uint x = 0; x < num_sequences; x++){
        byte[] buffer = new byte[size];
        rnd.NextBytes(buffer);
        if(x % 100 == 0){
            Console.WriteLine($"count: {x}");
        }
        double[] times = profile_sequence(buffer, chunk_sizes, iterations_per_sequence, checksum_fn);
        for(uint i = 0; i < chunk_sizes.Length; i++) {
            update_summary(x + 1, ref means[i], ref variances[i], ref mins[i], ref maxs[i], times[i]);
        }
    }
    for (uint i = 0; i < variances.Length; i++){
        variances[i] = finalize_summary(num_sequences, variances[i]);
    }
    print_stats(means, variances, mins, maxs, chunk_sizes);
}

Console.WriteLine("crc32");
uint[] chunk_sizes = new uint[] {1 << 22, 1 << 20, 1 << 10, 1 << 9, 1 << 8, 1 << 7};
profile(1 << 22, chunk_sizes, 1000, 1,  Crc.crc32);
Console.WriteLine("crc32c");
profile(1 << 22, chunk_sizes, 1000, 1,  Crc.crc32c);
