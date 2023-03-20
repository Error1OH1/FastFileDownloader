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
       
    }

    static void HttpClientDownloadChunking(string url)
    {
        //Creates an instance of HttpRequestMessage that uses HttpMethod.Get to retrieve information from the url
        var headrequest = new HttpRequestMessage(HttpMethod.Get, url);
        //Sends a Head request to get the the content length of the file, or the size in bytes
        var headresponse = client.SendAsync(headrequest).Result;
        //Retrieves the ContentLength(File Size) from the headresponse
        var contentLength = headresponse.Content.Headers.ContentLength;

        //Sets the amount of parts
        var parts = 6;
        //calculate size of each part
        var partsize = (long)Math.Ceiling((double)contentLength / parts);

        //create an array of download chunks based on the number of parts
        var downloadChunks = DownloadUtils.GetChunks(parts, contentLength.Value, partsize);

        //Download Each Chunk in parallel
        using (var filestream = new FileStream(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads" + "\\steam.exe"), FileMode.CreateNew))
        {
            //Creates a count for each chunk
            var count = 1;
            //Creates a list of tasks to run in parallel received from the `downloadChunks` 
            var tasks = new List<Task>();
            //Takes each chunk in downloadChunks array and adds them to the tasks list
            foreach (var chunk in downloadChunks)
            {
                //For each time it downloads a full chunk it adds 1
                Console.WriteLine($"{count++} chunk(s) completed");
                //Each chunks from the downloadChunks is added to the List<Task> class and run through DownloadChunkAsync with the url, filestream info, chunk size, and httpclient 
                tasks.Add(Task.Run(() => DownloadChunkAsync(url, filestream, chunk, client)));


            }
            //Wait's for all taks in List<Task> to be downloaded and moves on
            Task.WaitAll(tasks.ToArray());
        }
        //Initiates after all chunks in List<Task> have finished
        Console.WriteLine("All chunk(s) completed");




    }
    static void DownloadChunkAsync(string url, Stream OutPutStream, DownloadChunk chunk, HttpClient client)
    {
        //Creates a new instance of HttpRequestMessage that use the HttpMethod.Get to retrieve information from the url
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        //Gets the range(length) of each chunk and downloads
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


