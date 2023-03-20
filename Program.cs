using FastFileDownloader;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace FFastDownloader;




internal class Program
{
    //Httpclient method is created outside of the methods as a public static so that it may be used as an single instance by all methods
    public static void Main(string[] args)
    {
        //url used to retreive information
        UserHandler();
        

    }
    private static bool IsUrl(string input)
    {
        // Regular expression to match URLs
        string pattern = @"^(https?://)?([\da-z.-]+)\.([a-z.]{2,6})([/\w .-]*)*/?$";
        Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);
        return regex.IsMatch(input);
    }
    private static void UserHandler()
    {
        Console.WriteLine("Paste the download link you would like to download");
        Console.Write("> ");
        string url = Console.ReadLine();
        if (string.IsNullOrEmpty(url))
        {
            InputVerification.ErrorMessage("Input is Invalid or Empty");
            UserHandler();
        }
        if (!IsUrl(url))
        {
            InputVerification.ErrorMessage("Input is Invalid or Empty");
            UserHandler();
        }
        
        else
        {
            Console.Clear();
            Console.WriteLine("What would you like to name the file?");
            Console.Write(">");
            string filename = Console.ReadLine();
            Console.Clear();
            
            Console.WriteLine("What type of file is this?");
            string filetype = Console.ReadLine();
            Console.Clear();
            
            if (filetype.Contains("."))
            {
                Console.Clear();
                string exporttype = filetype.Replace(".", "");
                HttpClientDownloadChunking(url, filename, exporttype);
            }
            if (!filetype.Contains("."))
            {
                Console.Clear();
                HttpClientDownloadChunking(url, filename, filetype);
            }
        }
    }

    private static void HttpClientDownloadChunking(string url, string filename, string filetype)
    {
        using HttpClient client = new HttpClient();
        using HttpRequestMessage headrequest = new(HttpMethod.Head, url);
        using HttpResponseMessage headresponse = client.Send(headrequest);
        long contentLength = headresponse.Content.Headers.ContentLength ?? 0;


        var parts = 12;
        var partsize = (long)Math.Ceiling((double)contentLength / parts);


        DownloadChunk[] downloadChunks = DownloadUtils.GetChunks(parts, contentLength, partsize);

        var tasks = new List<Task>();
        string[] partpaths = new string[downloadChunks.Length];
        for (int i = 0; i < downloadChunks.Length; i++)
        {
            DownloadChunk downloadChunk = downloadChunks[i];
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", $"{filename}.{filetype}.part{i}");
            partpaths[i] = path;
            tasks.Add(Task.Run(() => DownloadChunk(url, path, downloadChunk, client)));
        }
        Task.WaitAll(tasks.ToArray());
        Stitch(filename, filetype, partpaths);




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
    static void Stitch(string filename, string filetype, string[] chunks)
    {
        
        string finalpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", $"{filename}.{filetype}");

        using FileStream finalfileStream = new(finalpath, FileMode.Create, FileAccess.Write, FileShare.None);
        foreach (string path in chunks)
        {
            using FileStream chunkStream = new(path, FileMode.Open, FileAccess.Read, FileShare.None);
            chunkStream.CopyTo(finalfileStream);
        }

        deletepartfiles(chunks);


    }
    static void deletepartfiles(string[] chunks)
    {
        foreach (string path in chunks)
        {
            File.Delete(path);
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

class InputVerification
{
    public static void ErrorMessage(string message)
    {
        Console.Clear();
        Console.WriteLine(message + "\n(Press any key to continue.)");
        Console.ReadKey(true);
        Console.Clear();
    }
}


