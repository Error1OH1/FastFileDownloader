using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastFileDownloader;



    internal struct DownloadChunk
    {
        public long End;
        public long Start;
        public long Size => End - Start;
    }

