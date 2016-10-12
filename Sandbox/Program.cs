using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VorbisCommentSharp;

namespace Sandbox {
    class Program {
        static void Main(string[] args) {
            byte[] b = File.ReadAllBytes(@"test.logg");
            using (VorbisFile file = new VorbisFile(b)) { 
                List<OggPage> list = file.GetPageHeaders();
                foreach (var ph in list) {
                    var h = ph.GetCommentHeader();
                    if (h == null) continue;
                    
                    if (h.PacketType == 3) {
                        var c = h.ExtractComments();
                        Console.WriteLine(c.Vendor);
                        Console.WriteLine(string.Join(", ", c.Comments.Select(p => p.Key + ": " + p.Value)));

                        //c.Comments["LOOPSTART"] = "100000";
                        //c.Comments["LOOPLENGTH"] = "150000";

                        VorbisFile newFile = new VorbisFile(file, c);
                        File.WriteAllBytes("out.logg", newFile.ToByteArray());
                    }
                }
            }
        }
    }
}
