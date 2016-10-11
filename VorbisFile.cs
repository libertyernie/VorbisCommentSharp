using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace VorbisCommentSharp {
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct OggPageHeader {
        public uint CapturePattern;
        public byte Version;
        public byte HeaderType;
        public long GranulePosition;
        public int BitstreamSerialNumber;
        public int PageSequenceNumber;
        public uint Checksum;
        public byte PageSegments;
    }

    public unsafe sealed class VorbisFile : IDisposable {
        internal IntPtr Data { get; private set; }
        internal int Length { get; private set; }

        public VorbisFile(byte[] data) {
            this.Data = Marshal.AllocHGlobal(data.Length);
            this.Length = data.Length;
            Marshal.Copy(data, 0, this.Data, this.Length);
        }

        public List<VorbisHeader> GetHeaders() {
            char[] needle = "vorbis".ToArray();
            int index_in_needle = 0;
            List<VorbisHeader> list = new List<VorbisHeader>();

            byte* ptr = (byte*)Data;
            byte* end = ptr + Length;
            while (ptr < end) {
                if (needle[index_in_needle] == *ptr) {
                    index_in_needle++;
                    if (index_in_needle == "vorbis".Length) {
                        list.Add(new VorbisHeader(this, ptr - index_in_needle));
                        index_in_needle = 0;
                    }
                    ptr++;
                } else if (index_in_needle > 0) {
                    index_in_needle = 0;
                } else {
                    ptr++;
                }
            }
            return list;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) { }
                Marshal.FreeHGlobal(Data);
                disposedValue = true;
            }
        }
        
        ~VorbisFile() {
            Dispose(false);
        }
        
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

    }
}
