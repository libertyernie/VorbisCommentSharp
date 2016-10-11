using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace VorbisCommentSharp {
    public unsafe class VorbisComments {
        public string Vendor { get; set; }
        public Dictionary<string, string> Comments { get; private set; }

        internal byte* OrigStart { get; private set; } // inclusive
        internal byte* OrigEnd { get; private set; } // exclusive

        public unsafe VorbisComments(byte* data) {
            Comments = new Dictionary<string, string>();

            byte* ptr = this.OrigStart = data;

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

            this.OrigEnd = ptr + 1;
        }
    }

    public unsafe class VorbisHeader {
        private byte* file_start;
        private byte* file_end;
        private byte* header;

        public byte PacketType {
            get {
                return *header;
            }
        }

        public VorbisComments ExtractComments() {
            if (PacketType != 3) throw new Exception("This is not a comment header");
            return new VorbisComments(header + 7);
        }

        public VorbisHeader(byte* file_start, byte* file_end, byte* header) {
            this.file_start = file_start;
            this.file_end = file_end;
            this.header = header;
        }

        public VorbisHeader(VorbisFile file, byte* header) {
            this.file_start = (byte*)file.Data;
            this.file_end = this.file_start + file.Length;
            this.header = header;
        }

        unsafe OggPageHeader* PreviousOggPageHeader {
            get {
                char[] needle = "OggS".ToArray();
                int index_in_needle = 3;

                byte* ptr = header;
                while (ptr >= file_start) {
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
                while (ptr < file_end) {
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
