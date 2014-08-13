using Nancy;
using Nancy.Hosting.Self;
using Nancy.ModelBinding;
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
            if (System.Diagnostics.Process.GetProcessesByName("PoshComplete").Count() == 1)
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
            Post["/poshcomplete"] = _ =>
            {
                var buffer = this.Bind<Buffer>();
                var serializer = new DataContractJsonSerializer(typeof(List<Candidate>));
                using (var ms = new System.IO.MemoryStream())
                {
                    List<Candidate> list = new List<Candidate>();

                    var ret = System.Management.Automation.CommandCompletion.MapStringInputToParsedInput(
                                                                            buffer.text, buffer.text.Length);
                    var candidates =
                        System.Management.Automation.CommandCompletion.CompleteInput(
                            ret.Item1, ret.Item2, ret.Item3, null,
                            System.Management.Automation.PowerShell.Create()
                        ).CompletionMatches;

                    foreach (var cand in candidates)
                        list.Add(new Candidate()
                        {
                            word = cand.CompletionText.Replace("'", ""),
                            kind = cand.ResultType.ToString(),
                            menu = cand.ToolTip.Replace("\r\n", "")
                        });

                    serializer.WriteObject(ms, list);
                    return Encoding.UTF8.GetString(ms.ToArray(), 0, (int)ms.Length);
                }
            };

            Get["/stop/"] = _ =>
            {
                Environment.Exit(0);
                return "";
            };
        }
    }

    public class Buffer
    {
        public string text { get; set; }
    }

    public class Candidate
    {
        public string word { get; set; }
        public string kind { get; set; }
        public string menu { get; set; }
    }
}
