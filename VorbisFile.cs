using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace VorbisCommentSharp {
    [StructLayout(LayoutKind.Explicit, Size = 27)]
    public struct OggPageHeader {
        [FieldOffset(0)]
        public uint CapturePattern;
        [FieldOffset(4)]
        public byte Version;
        [FieldOffset(5)]
        public byte HeaderType;
        [FieldOffset(6)]
        public long GranulePosition;
        [FieldOffset(14)]
        public int BitstreamSerialNumber;
        [FieldOffset(18)]
        public int PageSequenceNumber;
        [FieldOffset(22)]
        public uint Checksum;
        [FieldOffset(26)]
        public byte PageSegments;

        /*public unsafe byte[] PageSegmentLengths {
            get {
                fixed (OggPageHeader* ptr = &this) {
                    byte* table = (byte*)(ptr + 1);
                    byte[] arr = new byte[PageSegments];
                    for (int i=0; i<PageSegments; i++) {
                        arr[i] = table[i];
                    }
                    return arr;
                }
            }
        }

        public unsafe byte* GetSegmentPointer(int index) {
            if (index >= PageSegments) throw new IndexOutOfRangeException();
            fixed (OggPageHeader* ptr = &this) {
                byte* table = (byte*)(ptr + 1);
                byte* header = table + PageSegments;
                for (int i=0; i<index; i++) {
                    header += table[i];
                }
                return header;
            }
        }*/
    }

    public unsafe class OggPage {
        public VorbisFile Parent { get; internal set; }
        internal OggPageHeader* Header { get; set; }

        internal OggPage() { }

        public unsafe VorbisHeader GetCommentHeader() {
            byte* table = (byte*)(Header + 1);
            byte* header = table + Header->PageSegments;
            for (int i=0; i<Header->PageSegments; i++) {
                // Don't check a segment if its length is greater than 254 bytes (data is probably split across segments)
                if (table[i] < 255) {
                    VorbisHeader test = new VorbisHeader(this.Parent, header);
                    if (test.PacketType == 3 && test.VorbisTag == "vorbis") {
                        return test;
                    }
                }
                header += table[i];
            }
            return null;
        }
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

        public unsafe List<OggPage> GetPageHeaders() {
            char[] needle = "OggS".ToArray();
            int index_in_needle = 0;
            List<OggPage> list = new List<OggPage>();

            byte* ptr = (byte*)Data;
            byte* end = ptr + Length;
            while (ptr < end) {
                if (needle[index_in_needle] == *ptr) {
                    index_in_needle++;
                    ptr++;
                    if (index_in_needle == "OggS".Length) {
                        OggPageHeader* pageHeader = (OggPageHeader*)(ptr - index_in_needle);
                        if (pageHeader->CapturePattern != 0x5367674f) throw new Exception();
                        list.Add(new OggPage() {
                            Header = pageHeader,
                            Parent = this
                        });
                        index_in_needle = 0;

                        byte* segmentTable = (byte*)(pageHeader + 1);
                        ptr = segmentTable + pageHeader->PageSegments;
                        for (int i = 0; i < pageHeader->PageSegments; i++) {
                            ptr += segmentTable[i];
                        }
                    }
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
