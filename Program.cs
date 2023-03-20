using FastFileDownloader;
using System.Net.Http;
using System.Net.Http.Headers;

namespace FFastDownloader;




class Program
{
    public static  HttpClient client = new HttpClient();
    public static async Task Main(string[] args)
    {
        
        string url = "https://cdn.akamai.steamstatic.com/client/installer/SteamSetup.exe";
        HttpClientDownloadChunking(url);
        Console.WriteLine("finished");
    }

    static void HttpClientDownloadChunking(string url)
    {

        //Sends a Head request to get the the content length of the file, or the size in bytes
        var headrequest = new HttpRequestMessage(HttpMethod.Get, url);
        var headresponse = client.SendAsync(headrequest).Result;
        var contentLength = headresponse.Content.Headers.ContentLength;

        //calculate size of each part
        var parts = 6;
        var partsize = (long)Math.Ceiling((double)contentLength / parts);

        //create an array of download chunk based on the number of parts
        var downloadChunks = DownloadUtils.GetChunks(parts, contentLength.Value, partsize);

        //Download Each Chunk in parallel
        using (var filestream = new FileStream(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads" + "\\steam.exe"), FileMode.Create))
        {
            var count = 0;
            var tasks = new List<Task>();
            foreach (var chunk in downloadChunks)
            {
                count++;
                Console.WriteLine($"{count} chunk");
                tasks.Add(Task.Run(() => DownloadChunkAsync(url, filestream, chunk, client)));
                
            }
                Task.WaitAll(tasks.ToArray());
        }
        Console.WriteLine("download complete");




    }
    static void DownloadChunkAsync(string url, Stream OutPutStream, DownloadChunk chunk, HttpClient client)
    {
        
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Range = new RangeHeaderValue(chunk.Start, chunk.End);

        var response = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).Result;
        var stream = response.Content.ReadAsStreamAsync().Result;
        stream.CopyToAsync(OutPutStream, int.MaxValue).Wait();
    }
}

    class DownloadUtils
    {
        public static DownloadChunk[] GetChunks(int parts, long totalsize, long partsize)
        {
            DownloadChunk[] chunk = new DownloadChunk[parts];
        long start = 0;
        long end = partsize;

            for (int i = 0; i < chunk.Length; i++)
            {
            chunk[i] = new()
            {
                Start = start,
                    End = i == parts - 1 ? totalsize : end - 1
            };

            start = end;
            end += partsize;
        }
        return chunk;
        }
    }


