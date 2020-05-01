using System;
using System.Threading;
using System.Linq;
using System.Text;
using System.Web;
using System.Net;
using Newtonsoft.Json.Linq;

namespace YouTube_Downloader
{
    class Program
    {
        static bool done;

        static int progress;

        static void Main(string[] args)
        {
            string id = args[0];
            string infoUrl = "https://www.youtube.com/get_video_info?video_id=" + id + "&el=detailpage";

            WebClient Client = new WebClient();

            done = false;

            bool cipher = false;

            Client.DownloadProgressChanged += DownloadProgressCall;
            Client.DownloadFileCompleted += DownloadCompleteCall;

            byte[] info = Client.DownloadData(infoUrl);

            string infoString = Encoding.Unicode.GetString(info, 0, info.Length);

            string httpDecode = HttpUtility.UrlDecode(info, 0, info.Length, Encoding.UTF8);

            string[] paramSplit = httpDecode.Split('&');

            string player_response = paramSplit.First(x => x.StartsWith("player_response=")).Substring(16);

            JObject json = JObject.Parse(player_response);

            if (json["streamingData"]["formats"][0]["cipher"] != null) cipher = true;

            string title = json["videoDetails"]["title"].ToString();
            string lengthSeconds = json["videoDetails"]["lengthSeconds"].ToString();
            TimeSpan length = TimeSpan.FromSeconds(double.Parse(lengthSeconds));
            string channel = json["videoDetails"]["author"].ToString();
            string channelId = json["videoDetails"]["channelId"].ToString();
            JToken[] thumbs = json["videoDetails"]["thumbnail"]["thumbnails"].ToArray();
            int largestIndex = 0;
            int pixels = 0;
            for (int x = 0; x < thumbs.Length; ++x)
            {
                int p = int.Parse(thumbs[x]["width"].ToString()) * int.Parse(thumbs[x]["height"].ToString());
                if (p > pixels)
                {
                    largestIndex = x;
                    pixels = p;
                }
            }
            string viewCount = json["videoDetails"]["viewCount"].ToString();

            Console.WriteLine("Title:".PadRight(15) + title);
            Console.WriteLine("Channel:".PadRight(15) + channel + " (" + channelId + ")");
            Console.WriteLine("Length:".PadRight(15) + (length.Hours < 10 ? "0" : "") + length.Hours + ":" + (length.Minutes < 10 ? "0" : "") + length.Minutes + ":" + (length.Seconds < 10 ? "0" : "") + length.Seconds);
            Console.WriteLine("Views:".PadRight(15) + viewCount);
            Console.WriteLine("Thumbnail:".PadRight(15) + thumbs[largestIndex]["url"].ToString());

            JToken[] formats = json["streamingData"]["formats"].ToArray();
            JToken[] adaptiveFormats = json["streamingData"]["adaptiveFormats"].ToArray();
            JToken[] filteredAdaptive = adaptiveFormats.TakeWhile(x => x["mimeType"].ToString().Contains("video/mp4")).ToArray();
            JToken[] allFormats = new JToken[formats.Length + filteredAdaptive.Length];
            Array.Copy(formats, 0, allFormats, 0, formats.Length);
            Array.Copy(filteredAdaptive, 0, allFormats, formats.Length, filteredAdaptive.Length);

            int formatChoice = 1;

            if (allFormats.Length > 1)
            {
                Console.WriteLine("\nThis video has multiple formats, please choose one.\n");
                for (int x = 0; x < allFormats.Length; ++x) 
                {
                    Console.WriteLine("[" + (x + 1) + "]   " + 
                    (allFormats[x]["width"].ToString() + "x" + allFormats[x]["height"].ToString()).PadRight(10) + 
                    " (" + allFormats[x]["bitrate"].ToString() + ")");
                }
                Console.Write("\n>");
                while(!(int.TryParse(Console.ReadLine(), out formatChoice) && formatChoice > 0 && formatChoice <= allFormats.Length)) Console.Write("Invalid selection\n>");
            }

            System.IO.File.WriteAllText("new.json", json.ToString());

            string videoUrl;
            
            if (cipher)
            {
                string cipherUrl = allFormats[formatChoice - 1]["cipher"].ToString();
                videoUrl = HttpUtility.UrlDecode(cipherUrl.Substring(cipherUrl.IndexOf("url=") + 4));
            }
            else videoUrl = allFormats[formatChoice - 1]["url"].ToString();

            string fileName = title + " - " + allFormats[formatChoice - 1]["height"].ToString() + "p.mp4";

            foreach (char c in new string(System.IO.Path.GetInvalidFileNameChars())) fileName = fileName.Replace(c.ToString(), "");

            Console.WriteLine("Starting download.\n\n");

            Console.WriteLine(videoUrl);

            Client.DownloadFileAsync(new Uri(videoUrl), fileName);

            

            while(!done) ;
        }

        static void DownloadProgressCall(object sender, DownloadProgressChangedEventArgs e)
        {
            int newProgress = e.ProgressPercentage;
            if (newProgress != progress) 
            {
                progress = newProgress;
                Console.WriteLine(newProgress + "%");
            }
        }

        static void DownloadCompleteCall(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            Console.WriteLine(e.Error.Message);
            done = true;
        }
    }
}