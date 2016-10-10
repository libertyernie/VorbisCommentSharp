using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

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

    /*class OggPage {
        private OggPageHeader header;
        private List<VorbisPacket> packets;
        
        public IntPtr End { get; private set; }

        public static unsafe List<OggPage> ReadOggPages(byte[] data) {
            List<OggPage> list = new List<OggPage>();
            fixed (byte* start = data) {
                byte* end = start + data.Length;
                byte* ptr = start;
                while (ptr < end) {
                    OggPageHeader* h = (OggPageHeader*)ptr;
                    if (h->CapturePattern != 0x5367674f) {
                        throw new Exception("Wrong capture pattern, got: " + h->CapturePattern.ToString("X8"));
                    }
                    OggPage page = new OggPage(h);
                    list.Add(page);
                    ptr = (byte*)page.End;
                }
            }
            return list;
        }

        public unsafe OggPage(OggPageHeader* data) {
            init(data);
        }

        private unsafe void init(OggPageHeader* data) {
            header = *data;
            packets = new List<VorbisPacket>();

            byte* segmentTable = (byte*)(data + 1);
            byte* segmentPointer = segmentTable + header.PageSegments;
            List<byte> packet = null;
            for (int i = 0; i < header.PageSegments; i++) {
                byte length = segmentTable[i];
                byte[] segment = new byte[length];
                Marshal.Copy((IntPtr)segmentPointer, segment, 0, length);
                segmentPointer += length;
                
                if (packet == null) packet = new List<byte>();
                packet.AddRange(segment);
                if (length != 255) {
                    packets.Add(new VorbisPacket(packet));
                    packet = null;
                }
            }

            End = (IntPtr)segmentPointer;
        }

        public unsafe byte[] ToByteArray() {
            var segments = packets.SelectMany(p => p.ToSegments());
            byte[] array = new byte[27 + segments.Count() + segments.Select(s => s.Length).Sum()];

            int index = 27;
            foreach (var s in segments) {
                if (s.Length > 255) throw new Exception("Segment too long");
                array[index++] = (byte)s.Length;
            }
            foreach (var s in segments) {
                Array.Copy(s, 0, array, index, s.Length);
                index += s.Length;
            }

            fixed (byte* bptr = &array[0]) {
                OggPageHeader* ptr = (OggPageHeader*)bptr;
                *ptr = header;
                ptr->Checksum = 0;
                ptr->PageSegments = (byte)segments.Count();
            }

            Crc crc = new Crc();
            crc.Reset();
            foreach (byte b in array) {
                crc.Update(b);
            }
            
            fixed (byte* bptr = &array[0]) {
                OggPageHeader* ptr = (OggPageHeader*)bptr;
                ptr->Checksum = crc._crc;
            }

            return array;
        }

        public override string ToString() {
            return string.Join(", ", packets);
        }
    }

    class VorbisPacket {
        private byte[] data;

        public unsafe VorbisPacket(IEnumerable<byte> data) {
            this.data = data.ToArray();
            fixed (byte* ptr = &this.data[0]) {
                string s = new string((sbyte*)ptr, 1, 6);
                if (s != "vorbis") Console.WriteLine("Expected vorbis, got: " + s);
            }
        }

        private unsafe VorbisHeader GetHeader() {
            fixed (byte* ptr = &data[0]) {
                return *(VorbisHeader*)ptr;
            }
        }

        public List<byte[]> ToSegments() {
            List<byte[]> segments = new List<byte[]>();
            int pos = 0;
            while (data.Length - pos > 255) {
                byte[] A = new byte[255];
                Array.Copy(data, pos, A, 0, 255);
                segments.Add(A);

                pos += 255;
            }

            int remaining = data.Length - pos;
            byte[] B = new byte[remaining];
            Array.Copy(data, pos, B, 0, remaining);
            segments.Add(B);
            if (remaining == 255) {
                segments.Add(new byte[0]);
            }

            return segments;
        }

        public override string ToString() {
            switch (GetHeader().packet_type) {
                case 1:
                    return "Identification";
                case 3:
                    return "Comment";
                case 5:
                    return "Setup";
                default:
                    return GetHeader().packet_type.ToString();
            }
        }
    }*/

    public class VorbisComments {
        public string Vendor { get; set; }
        public Dictionary<string, string> Comments { get; private set; }

        public unsafe VorbisComments(byte* data) {
            Comments = new Dictionary<string, string>();

            byte* ptr = data;

            uint length = *(uint*)ptr;
            ptr += 4;

            Vendor = new string((sbyte*)ptr, 0, (int)length);
            ptr += length;

            uint user_comment_list_length = *(uint*)ptr;
            ptr += 4;

            for (int i = 0; i < user_comment_list_length; i++) {
                uint vector_length = *(uint*)ptr;
                ptr += 4;

                string combined = new string((sbyte*)ptr, 0, (int)vector_length);
                ptr += vector_length;

                int eq_index = combined.IndexOf('=');
                Comments.Add(combined.Substring(0, eq_index), combined.Substring(eq_index + 1));
            }

            if (*ptr == 0) throw new Exception("Unexpected end of comment header");
        }
    }

    public unsafe class VorbisHeader {
        private byte* start;
        private byte* end;
        private byte* packet_type;

        public byte PacketType {
            get {
                return *packet_type;
            }
        }

        public VorbisComments ExtractComments() {
            return new VorbisComments(packet_type + 7);
        }

        public VorbisHeader(byte* start, byte* end, byte* packet_type) {
            this.start = start;
            this.end = end;
            this.packet_type = packet_type;
        }

        public static List<VorbisHeader> Search(byte* start, byte* end) {
            char[] needle = "vorbis".ToArray();
            int index_in_needle = 0;
            List<VorbisHeader> list = new List<VorbisHeader>();

            byte* ptr = start;
            while (ptr < end) {
                if (needle[index_in_needle] == *ptr) {
                    index_in_needle++;
                    if (index_in_needle == "vorbis".Length) {
                        list.Add(new VorbisHeader(start, end, ptr - index_in_needle));
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

        unsafe OggPageHeader* PreviousOggPageHeader {
            get {
                char[] needle = "OggS".ToArray();
                int index_in_needle = 3;

                byte* ptr = packet_type;
                while (ptr >= start) {
                    if (needle[index_in_needle] == *ptr) {
                        index_in_needle--;
                        if (index_in_needle < 0) return (OggPageHeader*)ptr;
                    } else if (index_in_needle < 3) {
                        index_in_needle = 3;
                    } else {
                        ptr--;
                    }
                }
                throw new Exception("OggS not found");
            }
        }

        unsafe OggPageHeader* NextOggPageHeader {
            get {
                char[] needle = "OggS".ToArray();
                int index_in_needle = 0;

                byte* ptr = packet_type;
                while (ptr < end) {
                    if (needle[index_in_needle] == *ptr) {
                        index_in_needle++;
                        if (index_in_needle == "OggS".Length) return (OggPageHeader*)(ptr - index_in_needle);
                    } else if (index_in_needle > 0) {
                        index_in_needle = 0;
                    } else {
                        ptr++;
                    }
                }
                throw new Exception("OggS not found");
            }
        }
    }
}
