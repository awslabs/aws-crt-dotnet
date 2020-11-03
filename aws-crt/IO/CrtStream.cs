using System;
using System.IO;
using System.Runtime.InteropServices;

using Aws.Crt;

namespace Aws.Crt.IO
{
    public sealed class CrtStreamWrapper
    {
        public enum StreamState 
        {
            InProgress = 0,
            Done = 1,
        }

        /* Match native aws_stream_seek_basis */
        public enum SeekBasis {
            Begin = 0,
            End = 2
        }

        public delegate int CrtStreamReadCallback(
                        [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex=1)] byte[] buffer, 
                        UInt64 size,
                        out UInt64 bytesWritten);
        public delegate bool CrtStreamSeekCallback(Int64 offset, int basis);

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi)]
        public struct DelegateTable
        {
            public CrtStreamReadCallback ReadCallback;
            public CrtStreamSeekCallback SeekCallback;
        }

        private Stream BodyStream;

        public DelegateTable Delegates { get; private set; }

        private SeekOrigin SeekBasisToSeekOrigin(SeekBasis basis) {
            switch(basis) {
                case SeekBasis.Begin:
                    return SeekOrigin.Begin;

                case SeekBasis.End:
                    return SeekOrigin.End;
            }

            throw new ArgumentException("Seek basis must be Begin or End");
        }

        private bool SeekInternal(long offset, int basis) {
            SeekBasis realBasis = (SeekBasis) basis;

            try {
                if (BodyStream.CanSeek) {
                    BodyStream.Seek(offset, SeekBasisToSeekOrigin(realBasis));
                    return true;
                }
            } catch (ArgumentException) {
                ;
            }

            return false;
        }

        private int ReadInternal(byte[] buffer, ulong size, out ulong bytesWritten) {
            bytesWritten = 0;
            if (BodyStream != null)
            {
                var bufferStream = new MemoryStream(buffer);
                long prevPosition = BodyStream.Position;
                CRT.CopyStream(BodyStream, bufferStream);
                bytesWritten = (ulong)(BodyStream.Position - prevPosition);
                if (BodyStream.Position != BodyStream.Length)
                {
                    return (int) StreamState.InProgress;
                }
            }

            return (int) StreamState.Done;
        }

        public CrtStreamWrapper(Stream stream)
        {
            BodyStream = stream;
            var delegates = new DelegateTable();

            /*
             * We pass the delegate table by value to C, so we indicate a null stream with a nulled table.
             */
            if (stream != null) {
                delegates.ReadCallback = ReadInternal;
                delegates.SeekCallback = SeekInternal;
            } else {
                delegates.ReadCallback = null;
                delegates.SeekCallback = null;
            }

            Delegates = delegates;
        }
    }
}
