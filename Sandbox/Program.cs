using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VorbisCommentSharp;

namespace Sandbox {
    class Program {
        static unsafe void Main(string[] args) {
            byte[] b = File.ReadAllBytes(@"test.logg");
            using (VorbisFile file = new VorbisFile(b)) { 
                List<VorbisHeader> list = file.GetHeaders();
                foreach (var h in list) {
                    Console.WriteLine(h.PacketType);
                    OggPageHeader* prev = h.PreviousOggPageHeader;
                    SegmentPointers? ch = prev->GetCommentHeader();
                    if (ch != null) Console.WriteLine("    " + (uint)h.header + " " + (uint)ch.Value.SegmentStart);
                    if (h.PacketType == 3) {
                        var c = h.ExtractComments();
                        Console.WriteLine(c.Vendor);
                        Console.WriteLine(string.Join(", ", c.Comments.Select(p => p.Key + ": " + p.Value)));
                    }
                }
            }
        }
    }
}
