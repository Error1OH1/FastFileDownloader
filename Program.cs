using FastFileDownloader;
using System.Net.Http;
using System.Net.Http.Headers;

namespace FFastDownloader;




internal class Program
{
    //Httpclient method is created outside of the methods as a public static so that it may be used as an single instance by all methods
    public static void Main(string[] args)
    {
        //url used to retreive information
        string url = "https://releases.ubuntu.com/22.04.2/ubuntu-22.04.2-desktop-amd64.iso?_ga=2.256134540.680978053.1679289601-993827178.1679289601";
        HttpClientDownloadChunking(url);

    }

    private static void HttpClientDownloadChunking(string url)
    {
        using HttpClient client = new HttpClient();
        using HttpRequestMessage headrequest = new(HttpMethod.Head, url);
        using HttpResponseMessage headresponse = client.Send(headrequest);
        long contentLength = headresponse.Content.Headers.ContentLength ?? 0;


        var parts = 6;
        var partsize = (long)Math.Ceiling((double)contentLength / parts);


        DownloadChunk[] downloadChunks = DownloadUtils.GetChunks(parts, contentLength, partsize);

        var tasks = new List<Task>();
        string[] partpaths = new string[downloadChunks.Length];
        for (int i = 0; i < downloadChunks.Length; i++)
        {
            DownloadChunk downloadChunk = downloadChunks[i];
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", $"ubuntu.iso.part{i}");
            partpaths[i] = path;
            tasks.Add(Task.Run(() => DownloadChunk(url, path, downloadChunk, client)));
        }
        Task.WaitAll(tasks.ToArray());
        Stitch(partpaths);




    }
    public static void DownloadChunk(string url, string path, DownloadChunk chunk, HttpClient client)
    {
        using FileStream fileStream = new(path, FileMode.Create, FileAccess.Write, FileShare.None);

        using HttpRequestMessage request = new(HttpMethod.Get, url);
        request.Headers.Range = new RangeHeaderValue(chunk.Start, chunk.End);


        using HttpResponseMessage response = client.Send(request, HttpCompletionOption.ResponseHeadersRead);
        using Stream stream = response.Content.ReadAsStreamAsync().Result;

        stream.CopyToAsync(fileStream, int.MaxValue).Wait();
    }
    static void Stitch(string[] chunks)
    {
        string finalpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "ubuntu.iso");

        using FileStream finalfileStream = new(finalpath, FileMode.Create, FileAccess.Write, FileShare.None);
        foreach (string path in chunks)
        {
            using FileStream chunkStream = new(path, FileMode.Open, FileAccess.Read, FileShare.None);
            chunkStream.CopyTo(finalfileStream);
        }

    }

    internal class DownloadUtils
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
}


