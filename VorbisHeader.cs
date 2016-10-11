using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace VorbisCommentSharp {
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
        private byte* header;

        public byte PacketType {
            get {
                return *header;
            }
        }

        public VorbisComments ExtractComments() {
            return new VorbisComments(header + 7);
        }

        public VorbisHeader(byte* start, byte* end, byte* header) {
            this.start = start;
            this.end = end;
            this.header = header;
        }

        public VorbisHeader(VorbisFile file, byte* header) {
            this.start = (byte*)file.Data;
            this.end = this.start + file.Length;
            this.header = header;
        }

        unsafe OggPageHeader* PreviousOggPageHeader {
            get {
                char[] needle = "OggS".ToArray();
                int index_in_needle = 3;

                byte* ptr = header;
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

                byte* ptr = header;
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
