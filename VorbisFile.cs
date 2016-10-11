using System;
using System.Collections.Generic;
using System.IO;
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

        public unsafe List<OggPage> GetPageHeaders() {
            List<OggPage> list = new List<OggPage>();
            byte* ptr = (byte*)Data;
            byte* end = ptr + Length;
            while (ptr < end) {
                string capturePattern = new string((sbyte*)ptr, 0, 4);
                if (capturePattern != "OggS") throw new Exception("OggS expected, but not found");

                OggPageHeader* pageHeader = (OggPageHeader*)ptr;
                list.Add(new OggPage() {
                    Header = pageHeader,
                    Parent = this
                });

                byte* segmentTable = (byte*)(pageHeader + 1);
                ptr = segmentTable + pageHeader->PageSegments;
                for (int i = 0; i < pageHeader->PageSegments; i++) {
                    ptr += segmentTable[i];
                }
            }
            if (ptr > end) throw new Exception("Unexpected end of file");
            return list;
        }

        public VorbisFile(VorbisFile original, VorbisComments replacement) {
            VorbisHeader commentHeader = original.GetPageHeaders().Select(p => p.GetCommentHeader()).Single(h => h != null);
            VorbisCommentsFromFile originalComments = commentHeader.ExtractComments();
            byte[] replacementData;
            using (var ms = new MemoryStream()) {
                replacement.Write(ms).Wait();
                replacementData = ms.ToArray();
            }
            if (replacementData.Length + 7 >= 255) {
                throw new Exception("Comment header is too long for this program to handle (must be under 248 bytes)");
            }
            long length = this.Length
                - (originalComments.OrigEnd - originalComments.OrigStart)
                + replacementData.Length;
            throw new NotImplementedException();
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
