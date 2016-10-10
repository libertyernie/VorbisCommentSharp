using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VorbisCommentSharp;

namespace Sandbox {
    class Program {
        unsafe static void Main(string[] args) {
            byte[] b = File.ReadAllBytes(@"test.logg");
            fixed (byte* start = b) {
                byte* end = start + b.Length;
                List<VorbisHeader> list = VorbisHeader.Search(start, end);
                foreach (var h in list) {
                    Console.WriteLine(h.PacketType);
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
