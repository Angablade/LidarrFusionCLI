using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.RegularExpressions;
using TagLib;

class Funcs
{
    public static string TrimDatesinSquareBrackets(string input)
    {
        string pattern = @"\[\d+\]";
        string result = Regex.Replace(input, pattern, "");
        return result.Trim();
    }
    public static string FindClosestMatch(string input, dynamic allAlbums)
    {
        string closestMatch = null;
        int minDistance = int.MaxValue;

        foreach (var album in allAlbums)
        {
            string candidate = album.title;

            int distance = LevenshteinDistance(input, candidate);

            if (distance < minDistance)
            {
                minDistance = distance;
                closestMatch = album.id;
            }
        }

        return closestMatch;
    }
    private static int LevenshteinDistance(string s, string t)
    {
        int n = s.Length;
        int m = t.Length;
        int[,] d = new int[n + 1, m + 1];
        if (n == 0)
            return m;
        if (m == 0)
            return n;
        for (int i = 0; i <= n; d[i, 0] = i++) { }
        for (int j = 0; j <= m; d[0, j] = j++) { }
        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1,
                    d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
                if (i > 1 && j > 1 && s[i - 1] == t[j - 2] && s[i - 2] == t[j - 1])
                {
                    d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + cost);
                }
            }
        }
        return d[n, m];
    }
    public static string SearchYoutube(string query)
    {
        string queryEncoded = WebUtility.UrlEncode(query);
        string url = "https://www.youtube.com/results?search_query=" + queryEncoded;
        using (var client = new WebClient())
        {
            try
            {
                string response = client.DownloadString(url);
                if (response != null)
                {
                    var matches = Regex.Match(response, "\"videoRenderer\"\\s*:\\s*{\"videoId\"\\s*:\\s*\"([^\"]+)\"");
                    if (matches.Success)
                    {
                        string videoId = matches.Groups[1].Value;
                        string videoUrl = "https://www.youtube.com/watch?v=" + videoId;
                        System.Diagnostics.Debug.WriteLine("Video link found: " + videoUrl);
                        return videoUrl;
                    }
                    else
                    {
                        return "Video link not found in the search results.";
                    }
                }
                else
                {
                    return "Error fetching YouTube search results.";
                }
            }
            catch (WebException ex)
            {
                return "Error fetching YouTube search results: " + ex.Message;
            }
        }
    }
    public static string SearchYoutubeMusic(string query)
    {
        string queryEncoded = WebUtility.UrlEncode(query);
        string url = "https://music.youtube.com/search?q=" + queryEncoded;
        using (var client = new WebClient())
        {
            try
            {
                // Set the user-agent to simulate a browser request
                client.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:117.0) Gecko/20100101 Firefox/117.0");

                // Fetch the search results page
                string response = client.DownloadString(url)
                                        .Replace("\\x22", "\"")
                                        .Replace("\\x7b", "{")
                                        .Replace("\\x7d", "}")
                                        .Replace("\\x5b", "[")
                                        .Replace("\\x5d", "]");
                if (!string.IsNullOrEmpty(response))
                {
                    // Match the first video/track ID in the search results
                    var matches = Regex.Match(response, "\"videoId\"\\s*:\\s*\"([^\"]+)\"");
                    if (matches.Success)
                    {
                        string videoId = matches.Groups[1].Value;
                        string videoUrl = "https://music.youtube.com/watch?v=" + videoId;
                        System.Diagnostics.Debug.WriteLine("Music link found: " + videoUrl);
                        return videoUrl;
                    }
                    else
                    {
                        return "Music link not found in the search results.";
                    }
                }
                else
                {
                    return "Error fetching YouTube Music search results.";
                }
            }
            catch (WebException ex)
            {
                return "Error fetching YouTube Music search results: " + ex.Message;
            }
        }
    }

    public static string GetRandomInsult()
    {
        List<string> insults = new List<string>
        {
            "Usa tu cerebro, es gratis",
            "Eres más soso que la comida de un astronauta",
            "Eres más feo que una carretilla con pegatinas",
            "Eres una inversión a fondo perdido",
            "Te insultaría pero no lo haría tan bien como lo hizo la naturaleza",
            "Contigo se confirma la teoría científica de que un humano puede vivir sin cerebro",
            "Si los zombies comen cerebros, tú serías invisible para ellos",
            "Te daré dos medallas, una por tonto y la otra por si la pierdes",
            "Eres más lento que una tortuga con muletas",
            "Tienes menos luces que un árbol de Navidad en enero"
        };

        Random random = new Random();
        int index = random.Next(insults.Count);
        return insults[index];
    }

    public static Albuminfo GetAlbuminfo(string artist, string album)
    {
        string apiUrl = "https://musicbrainz.org/ws/2/";
        string formattedArtist = WebUtility.UrlEncode(artist);
        string formattedAlbum = WebUtility.UrlEncode(album);
        string url = $"{apiUrl}release/?query=artist:\"{formattedArtist}\"+AND+release:\"{formattedAlbum}\"&fmt=json";

        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        request.Method = "GET";
        request.UserAgent = "Listenarr V1";

        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
        using (Stream stream = response.GetResponseStream())
        using (StreamReader reader = new StreamReader(stream))
        {
            string jsonResponse = reader.ReadToEnd();

            if (!string.IsNullOrEmpty(jsonResponse))
            {
                dynamic data = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonResponse);
                Albuminfo Albuminfo = new Albuminfo();
                
                Albuminfo.formattedArtist = data.releases[0]["artist-credit"][0].name;
                Albuminfo.Country = data.releases[0].country;
                Albuminfo.lang = data.releases[0]["rext-representation"].language;
                Albuminfo.status = data.releases[0].status;
                Albuminfo.Date = data.releases[0].date;
                Albuminfo.formattedtitle = data.releases[0].title;
                Albuminfo.score = data.releases[0].score;
                Albuminfo.id = data.releases[0].id;

                return Albuminfo;
            }
        }
        return null;
    }
    public static string[] GetTrackList(string artist, string album)
    {
        string apiUrl = "https://musicbrainz.org/ws/2/";
        string formattedArtist = WebUtility.UrlEncode(artist);
        string formattedAlbum = WebUtility.UrlEncode(album);
        string url = $"{apiUrl}release/?query=artist:\"{formattedArtist}\"+AND+release:\"{formattedAlbum}\"&fmt=json";

        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        request.Method = "GET";
        request.UserAgent = "Listenarr V1";

        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
        using (Stream stream = response.GetResponseStream())
        using (StreamReader reader = new StreamReader(stream))
        {
            string jsonResponse = reader.ReadToEnd();

            if (!string.IsNullOrEmpty(jsonResponse))
            {
                dynamic data = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonResponse);

                if (data.releases != null && data.releases.Count > 0)
                {
                    string releaseId = data.releases[0].id;
                    string tracklistUrl = $"{apiUrl}release/{releaseId}?inc=recordings&fmt=json";

                    request = (HttpWebRequest)WebRequest.Create(tracklistUrl);
                    request.Method = "GET";
                    request.UserAgent = "Listenarr V1";

                    using (HttpWebResponse tracklistResponse = (HttpWebResponse)request.GetResponse())
                    using (Stream tracklistStream = tracklistResponse.GetResponseStream())
                    using (StreamReader tracklistReader = new StreamReader(tracklistStream))
                    {
                        string tracklistJsonResponse = tracklistReader.ReadToEnd();

                        if (!string.IsNullOrEmpty(tracklistJsonResponse))
                        {
                            dynamic tracklistData = Newtonsoft.Json.JsonConvert.DeserializeObject(tracklistJsonResponse);

                            if (tracklistData.media[0].tracks != null)
                            {
                                IEnumerable<dynamic> tracks = (IEnumerable<dynamic>)tracklistData.media[0].tracks;
                                return tracks.Select(t => (string)t.title).ToArray();
                            }
                        }
                    }
                }
            }
            return null;
        }
    }

    public static void PrintMinorHeader(string minorHeaderText)
    {
        string thinLine = new string('-', 60);
        string spaceLine = new string(' ', 60);

        Console.WriteLine();
        Console.WriteLine($"+{thinLine}+");
        Console.WriteLine($"|{spaceLine}|");
        Console.WriteLine($"|{minorHeaderText.ToUpper(),60}|");
        Console.WriteLine($"|{spaceLine}|");
        Console.WriteLine($"+{thinLine}+");
        Console.WriteLine();
    }

     static string GetLidarrRootFolderPath(string lidarrApiUrl, string lidarrApiKey)
    {
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("X-Api-Key", lidarrApiKey);
            try
            {
                HttpResponseMessage response = client.GetAsync($"{lidarrApiUrl}/api/v1/rootfolder").Result;
                response.EnsureSuccessStatusCode(); // Throws exception for non-success status codes
                string jsonResponse = response.Content.ReadAsStringAsync().Result;
                RootFolder[] rootFolders = Newtonsoft.Json.JsonConvert.DeserializeObject<RootFolder[]>(jsonResponse);
                if (rootFolders.Length > 0)
                {
                    return rootFolders[0].path;
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            return null;
        }
    }

    public static void WriteID3v3Tags(string filePath, string artist, string album, string track)
    {
        try
        {
            using (var file = TagLib.File.Create(filePath))
            {
                var tag = (TagLib.Id3v2.Tag)file.GetTag(TagTypes.Id3v2, true);

                tag.Title = track;
                tag.Artists = new[] { artist };
                tag.Album = album;
                file.Save();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing ID3v3 tags: {ex.Message}");
        }
    }

    public static string SanitizePath(string path)
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();
        string sanitizedPath = new string(path
            .Where(c => !invalidChars.Contains(c))
            .ToArray());

        return sanitizedPath.Replace("<","").Replace(">","").Replace("\"","");
    }
}
class RootFolder
{
    public string path { get; set; }
}

class Albuminfo
{
    public string formattedtitle { get; set; } 
    public string status { get; set; } 
    public string lang { get; set; } 
    public string Date { get; set; } 
    public string Country { get; set; } 
    public string formattedArtist { get; set; } 
    public string score { get; set; }
    public string id { get; set; }

}