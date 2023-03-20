// LFInteractive LLC. - All Rights Reserved
using FastFileDownloader;
using System.Net.Http.Headers;

namespace FFastDownloader;

internal class Program
{
    public static async Task Main(string[] args)
    {
        string url = "https://cdn.akamai.steamstatic.com/client/installer/SteamSetup.exe";
        HttpClientDownloadChunking(url);
        Console.WriteLine("finished");
    }

    private static void DownloadChunk(string url, string path, DownloadChunk chunk, HttpClient client)
    {
        using FileStream filestream = new(path, FileMode.Create, FileAccess.Write, FileShare.None);

        using HttpRequestMessage request = new(HttpMethod.Get, url);
        request.Headers.Range = new RangeHeaderValue(chunk.Start, chunk.End);

        using HttpResponseMessage response = client.Send(request, HttpCompletionOption.ResponseHeadersRead);
        using Stream stream = response.Content.ReadAsStreamAsync().Result;
        stream.CopyToAsync(filestream, int.MaxValue).Wait();
    }

    private static void HttpClientDownloadChunking(string url)
    {
        //Sends a Head request to get the the content length of the file, or the size in bytes
        using HttpClient client = new();
        using HttpRequestMessage headrequest = new(HttpMethod.Head, url);
        using HttpResponseMessage headresponse = client.Send(headrequest);
        long contentLength = headresponse.Content.Headers.ContentLength ?? 0;

        //calculate size of each part
        var parts = 6;
        var partsize = (long)Math.Ceiling((double)contentLength / parts);

        //create an array of download chunk based on the number of parts
        DownloadChunk[] downloadChunks = DownloadUtils.GetChunks(parts, contentLength, partsize);

        var tasks = new List<Task>();
        for (int i = 0; i < downloadChunks.Length; i++)
        {
            DownloadChunk chunk = downloadChunks[i];
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", $"steam.exe.part{i}");
            tasks.Add(Task.Run(() => DownloadChunk(url, path, chunk, client)));
        }
        Task.WaitAll(tasks.ToArray());
        Console.WriteLine("download complete");
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