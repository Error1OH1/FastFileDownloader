using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastFileDownloader;



    internal struct DownloadChunk
{
    //stores the info for End
    public long End;
    //stores the info for End
    public long Start;
    //Creates and stores the size info, being the end minus the start
    public long Size => End - Start;
    }

