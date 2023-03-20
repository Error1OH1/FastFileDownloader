using FastFileDownloader;
using System.Net.Http;
using System.Net.Http.Headers;

namespace FFastDownloader;




class Program
{
    //Httpclient method is created outside of the methods as a public static so that it may be used as an single instance by all methods
    public static  HttpClient client = new HttpClient();
    public static async Task Main(string[] args)
    {
        //url used to retreive information
        string url = "https://cdn.akamai.steamstatic.com/client/installer/SteamSetup.exe";
        //Sends the Url to and Starts the HttpClientDownloadChunking method
        HttpClientDownloadChunking(url);

    }

    //Processes the information from the link
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
    //Processes the size & Downloads each chunk
    static void DownloadChunkAsync(string url, Stream OutPutStream, DownloadChunk chunk, HttpClient client)
    {
        //Creates a new instance of HttpRequestMessage that use the HttpMethod.Get to retrieve information from the url
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        //Gets the range(length) of each new chunk
        request.Headers.Range = new RangeHeaderValue(chunk.Start, chunk.End);

        //Sends the `request` function created ealier witht HttpCompletionOption.ResponseHeaders.Read which receives the headers as soon as they are available
        var response = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).Result;
        //Once the above response is received the response.Content.ReadAsStreamAsync().Result; is use to read the contents of the response
        var stream = response.Content.ReadAsStreamAsync().Result;
        //Once the response is read stream.CopyToAsync is used to copy the information and run it through FileStream, int.MaxValue is used to set the buffer size to the maximum value
        stream.CopyToAsync(OutPutStream, int.MaxValue).Wait();
    }
}

     //The DownloadUtils class is used to process the math required to get the size of each chunk
    class DownloadUtils
    {
        //This method retreives the: amount of parts that the download will by chunked(partitioned) into, the total size of the file, and the extra math used to get the size of each part
        public static DownloadChunk[] GetChunks(int parts, long totalsize, long partsize)
    {
        //creates an instance of the method followed by the amount of partitions to be used with the math
        DownloadChunk[] chunk = new DownloadChunk[parts];
        //Sets the byte value for the first chunk to start at which is updated to the end value of the previous chunk
        long start = 0;
        //Sets the end of the chunk to the partsize introduced via the math in downloadChunks the divides the overall size
        long end = partsize;

        //Sets the Value for i = 0 which is used to index the array, the loop continues as long as i is less than the length of the chunk array, after each loop it adds 1
        for (int i = 0; i < chunk.Length; i++)
        {
            //This creates an array for each chunk to run through
            chunk[i] = new()
            {
                //Sets the start value of each chunk to the value of `start`
                Start = start,
                //Checks if i is equal to parts(partitions) - 1,if it is it assigns the value of end = totalsize, else it subtracts the end by 1
                End = i == parts - 1 ? totalsize : end - 1
            };

            //Sets the start value to the end of the previous chunk
            start = end;
            //Increases the value of end by the size of each chunk
            end += partsize;
        }
        //Returns the chunk aray containing the DownloadChunk object representing the chunk(partition)
        return chunk;
        }
    }


