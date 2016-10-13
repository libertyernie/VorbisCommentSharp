using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VorbisCommentSharp;

namespace Sandbox {
    class Program {
        static void Main(string[] args) {
            byte[] b = File.ReadAllBytes(@"test.ogg");

            using (VorbisFile file = new VorbisFile(b)) { 
                List<OggPage> pageHeaders = file.GetPageHeaders();

                VorbisHeader commentHeader = null;
                foreach (var pageHeader in pageHeaders) {
                    commentHeader = pageHeader.GetCommentHeader();
                    if (commentHeader != null) break;
                }

                if (commentHeader == null) throw new Exception("comment header not found");
                    
                if (commentHeader.PacketType == 3) {
                    var c = commentHeader.ExtractComments();

                    Console.WriteLine(c.Vendor);
                    Console.WriteLine(string.Join(", ", c.Comments.Select(p => p.Key + ": " + p.Value)));

                    c.Comments["LOOPSTART"] = "100000";
                    c.Comments["LOOPLENGTH"] = "150000";

                    using (VorbisFile newFile = new VorbisFile(file, c)) {
                        File.WriteAllBytes("out.ogg", newFile.ToByteArray());
                    }
                }
            }
        }
    }
}
