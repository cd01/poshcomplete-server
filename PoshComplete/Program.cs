using Nancy;
using Nancy.Hosting.Self;
using NMaier.GetOptNet;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Linq;


namespace PoshComplete
{
	class Program
	{
		static void Main(string[] args)
		{
            Opts opts = new Opts();
            opts.Parse(args);

            using (var nancyHost = new NancyHost(new Uri("http://localhost:" + opts.Port.ToString())))
            {
                nancyHost.Start();
                Console.ReadLine();
                nancyHost.Stop();
            }
		}
	}

    class Opts : GetOpt
    {
         [Argument("port", HelpText = "")]
         [ShortArgument('p')]
         public int Port = 1234;
    }
 
    public class Server : Nancy.NancyModule
    {
        public Server()
        {
            Get["/poshcomplete/{inputText}"] = (query) =>
            {
                var serializer = new DataContractJsonSerializer(typeof(List<Candidate>));
                using (var ms = new System.IO.MemoryStream())
                {
                    string line = query.inputText;
                    List<Candidate> list = new List<Candidate>();

                    #if __MonoCS__
                        StaticConfiguration.DisableErrorTraces = false;
                        List<Candidate> candidates;

                        string json;
                        using (var sr = new System.IO.StreamReader("./dictionary/Cmdlet.json", Encoding.GetEncoding("UTF-8")))
                            json = sr.ReadToEnd();

                        using (var jsonMs = new System.IO.MemoryStream(Encoding.UTF8.GetBytes(json)))
                            candidates = (List<Candidate>)serializer.ReadObject(jsonMs);

                        foreach (var cand in candidates)
                            if (cand.word.StartsWith(line, StringComparison.OrdinalIgnoreCase))
                                list.Add(new Candidate() { word = cand.word.Replace("'", ""),
                                                           kind = cand.kind.ToString(),
                                                           menu = cand.menu.Replace("\r\n", "") });
                    #else
                        var candidates =
                            System.Management.Automation.CommandCompletion.CompleteInput(
                                line,
                                line.Length,
                                null,
                                System.Management.Automation.PowerShell.Create()
                            ).CompletionMatches;

                        foreach (var cand in candidates)
                            list.Add(new Candidate() { word = cand.CompletionText.Replace("'", ""),
                                                       kind = cand.ResultType.ToString(),
                                                       menu = cand.ToolTip.Replace("\r\n", "") });
                    #endif

                    serializer.WriteObject(ms, list);
                    return Encoding.UTF8.GetString(ms.ToArray(), 0, (int)ms.Length);
                }
            };

            Get["/stop/"] = (query) =>
            {
                Environment.Exit(0);
                return "";
            };
        }
    }

    public class Candidate
    {
        public string word { get; set; }
        public string kind { get; set; }
        public string menu { get; set; }
    }
}
