using System;
using System.IO;
using System.Runtime.InteropServices;

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

        public delegate int DRead(
                        [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex=1)] byte[] buffer, 
                        UInt64 size,
                        out UInt64 bytesWritten);
        public delegate bool DSeek(Int64 offset, int basis);

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi)]
        public struct DelegateTable
        {
            public DRead Read;
            public DSeek Seek;
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

            return SeekOrigin.Begin;
        }

        private bool SeekInternal(long offset, int basis) {
            SeekBasis realBasis = (SeekBasis) basis;

            if (BodyStream.CanSeek) {
                BodyStream.Seek(offset, SeekBasisToSeekOrigin(realBasis));
                return true;
            } 

            return false;
        }

        private int ReadInternal(byte[] buffer, ulong size, out ulong bytesWritten) {
            bytesWritten = 0;
            if (BodyStream != null)
            {
                var bufferStream = new MemoryStream(buffer);
                long prevPosition = BodyStream.Position;
                BodyStream.CopyTo(bufferStream, buffer.Length);
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

            if (stream != null) {
                delegates.Read = ReadInternal;
                delegates.Seek = SeekInternal;
            } else {
                delegates.Read = null;
                delegates.Seek = null;
            }

            Delegates = delegates;
        }
    }
}