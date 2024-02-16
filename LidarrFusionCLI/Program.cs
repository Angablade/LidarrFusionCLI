using System.Diagnostics;
using System.Net;
using Figgle;

class Program
{
    static async Task Main(string[] args)
    {
        string lidarrApiUrl = null;
        string lidarrApiKey = null;
        bool autoDownload = false;
        bool verbose = false;
        bool tagger = true;
        string generateList = null;
        string rootpath = null;
        string ytqappend = null;
        string ytdlp = null;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "-lidarrapiurl":
                case "-lau":
                    if (i + 1 < args.Length)
                    {
                        lidarrApiUrl = args[i + 1];
                        i++;
                    }
                    break;

                case "-lidarrapikey":
                case "-lak":
                    if (i + 1 < args.Length)
                    {
                        lidarrApiKey = args[i + 1];
                        i++;
                    }
                    break;

                case "-lidarrootpath":
                case "-lrp":
                    if (i + 1 < args.Length)
                    {
                        rootpath = args[i + 1];
                        i++;
                    }
                    break;
                case "-autodownload":
                case "-adl":
                    autoDownload = true;
                    break;

                case "-notagger":
                case "-ntg":
                    tagger = false;
                    break;

                case "-verbose":
                case "-v":
                    verbose = true;
                    break;

                case "-generate":
                case "-gen":
                    if (i + 1 < args.Length)
                    {
                        generateList = args[i + 1];
                        i++;
                    }
                    break;

                case "-ytdlp":
                case "-ytd":
                    if (i + 1 < args.Length)
                    {
                        ytdlp = args[i + 1];
                        i++;
                    }
                    break;

                case "-ytquery":
                case "-ytq":
                    if (i + 1 < args.Length)
                    {
                        ytqappend = args[i + 1];
                        i++;
                    }
                    break;

                case "-h":

                    Console.WriteLine("Usage: <app> [options]");
                    Console.WriteLine("-lidarrapiurl or -lau: Set Lidarr API URL (required)");
                    Console.WriteLine("-lidarrapikey or -lak: Set Lidarr API Key (required))");
                    Console.WriteLine("-lidarrootpath or -lrp: Set Lidarr root path (optional)");
                    Console.WriteLine("-autodownload or -adl: Enable auto download (optional)");
                    Console.WriteLine("-verbose or -v: Enable verbose mode (optional)" );
                    Console.WriteLine("-generate or -gen: Generate batch gen (optional)");
                    Console.WriteLine("-notagger or -ntg: turn off auto tagging (optional)");
                    Console.WriteLine("-ytdlp or -ytd: Specify the binary for yt-dlp.exe");
                    Console.WriteLine("-ytquery or -ytq: Appends query string on youtube search (Optional)");
                    Console.WriteLine("-help or -h: Display this help message");
                    return;
            }
        }

        bool generateListFlag = !string.IsNullOrEmpty(generateList);

        if (generateListFlag)
        {
            if(File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,generateList))) {
                File.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, generateList));
                File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, generateList), "@echo off" + Environment.NewLine);
            }
        }

        Console.WriteLine($"lidarrApiUrl: {lidarrApiUrl}");
        Console.WriteLine($"lidarrApiKey: {lidarrApiKey}");
        if (verbose) { 
            Console.WriteLine($"autoDownload: {autoDownload}");
            Console.WriteLine($"verbose: {verbose}");
            Console.WriteLine($"generateList: {generateList}");
            Console.WriteLine($"Generate_List: {generateListFlag}");
            Console.WriteLine($"YTQuery Append: \"{ytqappend}\"");
        };
        Console.WriteLine();

        int estupidocounter = 0;
    RetryCheck:

        if (estupidocounter >= 10) {
            Console.WriteLine(Funcs.GetRandomInsult());
            Environment.Exit(0);
        }

        Console.WriteLine("Do your settings look correct? (y/n)");

        ConsoleKeyInfo key = Console.ReadKey();

        if (key.Key == ConsoleKey.Y)
        {
            Console.WriteLine("\nUser agreed. Continuing with the process...");
            goto Contig;
        }
        else if (key.Key == ConsoleKey.N)
        {
            Console.WriteLine("\nUser declined. Exiting the process...");
            Environment.Exit(0);
        }
        else
        {
            Console.WriteLine("\nInvalid input. Please press 'y' or 'n'.");
            estupidocounter++;
            goto RetryCheck;
        }
    Contig:
        Console.WriteLine();
        using (var client = new WebClient())
        {
            client.Headers.Add("X-Api-Key", lidarrApiKey);
            string url = null;
            if(rootpath == null) { 
                url = $"{lidarrApiUrl}/api/v1/rootfolder";
                if (verbose) { Console.WriteLine(url); Console.WriteLine(); };
                try
                {
                    string response = client.DownloadString(url);
                    dynamic root = Newtonsoft.Json.JsonConvert.DeserializeObject(response).ToString().Split(",");
                    foreach (dynamic item in root)
                    {
                        string x = item.ToString();
                        if (x.Contains("path")){
                           rootpath = x.Split((char)34)[3].Replace("\\\\","\\");
                        }
                    }
                    if (verbose) { Console.WriteLine(rootpath); Console.WriteLine(); };
                }
                catch (WebException ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            url = $"{lidarrApiUrl}/api/v1/wanted/missing?pageSize=1000000000&includeArtist=true&monitored=true";
            if (verbose) { Console.WriteLine(url); Console.WriteLine(); };
            try
            {
                string response = client.DownloadString(url);
                dynamic data = Newtonsoft.Json.JsonConvert.DeserializeObject(response);

                if (verbose) { Console.WriteLine(data); Console.WriteLine(); };

                if (data.records != null && data.records.Count > 0)
                {
                    Console.WriteLine(FiggleFonts.Ogre.Render("LidarrFusionCLI"));
                    Console.WriteLine("By Angablade");
                    foreach (var record in data.records)
                    {
                        if (record.artist.artistName != null && record.title != null)
                        {
                            string artist = record.artist.artistName;
                            string album = record.title;
                            string id = record.id;
                            Funcs.PrintMinorHeader($"{artist} - {album} ({id})");
                            string[] tracks = Funcs.GetTrackList(artist, album);
                            try
                            {
                                if (tracks != null && tracks.Length > 0) { 
                                    foreach (var track in tracks)
                                    {
                                        Console.WriteLine(track);

                                        string youtubelink = Funcs.SearchYoutube($"{artist} - {album} - {track}{ytqappend}");
                                        if (verbose) { Console.WriteLine(youtubelink); Console.WriteLine(); };

                                        string tracklocation = Path.Combine(rootpath, Funcs.SanitizePath(artist), Funcs.SanitizePath(album), Funcs.SanitizePath(track) + ".mp3");

                                        if (!Directory.Exists(Path.Combine(rootpath, Funcs.SanitizePath(artist), Funcs.SanitizePath(album))))
                                        {
                                            Directory.CreateDirectory(Path.Combine(rootpath, Funcs.SanitizePath(artist), Funcs.SanitizePath(album)));
                                        }

                                        Console.WriteLine(tracklocation);
                                        string processPath = ytdlp;
                                        string processArguments = $"{youtubelink} -x --audio-format \"mp3\" --match-filter \"is_live != true & was_live != true & duration < 3600\" --download-archive \"archive.txt\" --break-on-existing --output \"{tracklocation}\"";

                                        if (generateListFlag)
                                        {
                                            File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, generateList), "yt-dlp_min.exe " + processArguments + Environment.NewLine);
                                        }

                                        if (autoDownload)
                                        {
                                            Console.WriteLine("Downloading: " + track);
                                            Process process = new Process();
                                            process.StartInfo.FileName = processPath;
                                            process.StartInfo.Arguments = processArguments;
                                            process.StartInfo.UseShellExecute = false;
                                            process.StartInfo.RedirectStandardOutput = true;
                                            process.StartInfo.RedirectStandardError = true;

                                            process.Start();
                                            string standardOutput = process.StandardOutput.ReadToEnd();

                                            Console.WriteLine(standardOutput.Trim());
                                            process.WaitForExit();

                                            if (verbose)
                                            {
                                                string standardError = process.StandardError.ReadToEnd();
                                                Console.WriteLine("Standard Error:");
                                                Console.WriteLine(standardError);
                                            };

                                            if (tagger)
                                            {
                                                if (!standardOutput.Contains("has already been recorded in the archive"))
                                                {
                                                    Console.WriteLine("Writing Tags");
                                                    Funcs.WriteID3v3Tags(tracklocation, artist, album, track);
                                                }
                                                else
                                                {
                                                    Console.WriteLine("Skipping Tags");
                                                }
                                            }
                                            Console.WriteLine();
                                        }
                                    }
                                }
                            }
                            catch { }
                        }
                        else
                        {
                                Console.WriteLine("Incomplete information for a missing album.");
                        }
                        Thread.Sleep(3500);
                    }
                }
                else
                {
                    Funcs.PrintMinorHeader(response.ToString());
                    Console.WriteLine("No missing tracks found on server or unable to parse the response.");
                }
            }
            catch (WebException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
