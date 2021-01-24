using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using Console = Colorful.Console;
using Leaf.xNet;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using JNogueira.Discord.Webhook.Client;

namespace ProxyChecker
{
    public class Config
    {
        public bool enableWebhook { get; set; }
        public string webhook_url  { get; set; }
        public string proxy_test_site { get; set; }
    }
    class Program
    {
        [STAThread]
        
        static void Main(string[] args)
        {
            if (!File.Exists("config.json"))
            {
                File.WriteAllText("config.json", "{\n\"enableWebhook\":\"false\",\n\"webhook_url\":\"https://discord.com/api/webhooks/<webhook-id>/<webhook-token>\",\n\"proxy_test_site\":\"https://azenv.net\"\n}");
            }
            config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));

            date = DateTime.Now.ToString("MMM dd, yyyy — HH.mm.ss");
            Console.Title = "ProxyChecker | Made By Wykt";
            var proxies = new String[] { };
            Console.WriteLine("[1] Choose proxies", Color.BlueViolet);
            Console.WriteLine("[2] Parse from ProxyScrape", Color.BlueViolet);
            Console.Write("\r\n-> ", Color.BlueViolet);
            int mode = int.Parse(Console.ReadLine());



            if(mode == 1)
            {
                using (var openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
                    openFileDialog.FilterIndex = 1;
                    openFileDialog.Title = "Choose Proxies";
                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        proxies = File.ReadAllLines(openFileDialog.FileName);
                        if (proxies == null)
                        {
                            Console.Clear();
                            Console.WriteLine("File is empty or invalid!", Color.DarkRed);
                        }
                    }
                }
            }

            Console.Clear();


            if (mode == 1)
            {
                Console.WriteLine(proxies.Length + " proxies loaded.", Color.BlueViolet);
            }
            Console.Write("Threads: ", Color.BlueViolet);
            int threads = 0;
            try
            {
                threads = int.Parse(Console.ReadLine());
            } catch
            {
                Console.WriteLine("Must be a number! Shutdowning in 3 seconds...", Color.BlueViolet);
                Thread.Sleep(3000);
                Environment.Exit(-1);
            }
            Console.Write("Timeout (5s = 5000): ", Color.BlueViolet);
            int timeout = 0;
            try
            {
                timeout = int.Parse(Console.ReadLine());
            } catch
            {
                Console.WriteLine("Must be a number! Shutdowning in 3 seconds...", Color.BlueViolet);
                Thread.Sleep(3000);
                Environment.Exit(-1);
            }
            Console.Write("Proxy Connect Timeout (5s = 5000): ", Color.BlueViolet);
            int proxyTimeout = 0;
            try
            {
                proxyTimeout = int.Parse(Console.ReadLine());
            }
            catch
            {
                Console.WriteLine("Must be a number! Shutdowning in 3 seconds...", Color.BlueViolet);
                Thread.Sleep(3000);
                Environment.Exit(-1);
            }
            Console.Write("Proxy Type (HTTP | SOCKS4 | SOCKS4A | SOCKS5): ", Color.BlueViolet);
            proxyTypeString = Console.ReadLine();
            if(!proxyTypeString.ToLower().Equals("http") && !proxyTypeString.ToLower().Equals("socks4") && !proxyTypeString.ToLower().Equals("socks4a") && !proxyTypeString.ToLower().Equals("socks5"))
            {
                Console.WriteLine("Invalid Proxy Type, Shutdowning in 3 seconds...", Color.BlueViolet);
                Thread.Sleep(3000);
                Environment.Exit(-1);
            }
            ProxyType proxyType = ProxyType.HTTP;

            switch (proxyTypeString.ToLower())
            {
                case "http":
                    proxyType = ProxyType.HTTP;
                    if(mode == 2)
                    {
                        WebClient webClient = new WebClient();
                        Console.WriteLine("Scraping HTTP Proxies.", Color.BlueViolet);
                        proxies = webClient.DownloadString("https://api.proxyscrape.com/v2/?request=getproxies&protocol=http&timeout=" + timeout + "&country=all&ssl=all&anonymity=all").Split('\n');
                        Console.WriteLine("Scraped " + proxies.Length + " proxies !", Color.BlueViolet);
                    }
                    break;
                case "socks4":
                    proxyType = ProxyType.Socks4;
                    if (mode == 2)
                    {
                        WebClient webClient = new WebClient();
                        Console.WriteLine("Scraping Socks4 Proxies.", Color.BlueViolet);
                        proxies = webClient.DownloadString("https://api.proxyscrape.com/v2/?request=getproxies&protocol=socks4&timeout=" + timeout + "&country=all&ssl=all&anonymity=all").Split('\n');
                        Console.WriteLine("Scraped " + proxies.Length + " proxies !", Color.BlueViolet);
                    }
                    break;
                case "socks4a":
                    proxyType = ProxyType.Socks4A;
                    if (mode == 2)
                    {
                        WebClient webClient = new WebClient();
                        Console.WriteLine("ProxyScrape don't have Socks4A Proxy Type. Scraping Socks4 Proxies.", Color.BlueViolet);
                        proxies = webClient.DownloadString("https://api.proxyscrape.com/v2/?request=getproxies&protocol=socks4&timeout=" + timeout + "&country=all&ssl=all&anonymity=all").Split('\n');
                        Console.WriteLine("Scraped " + proxies.Length + " proxies !", Color.BlueViolet);
                        proxyType = ProxyType.Socks4;
                        
                    }
                    break;
                case "socks5":
                    proxyType = ProxyType.Socks5;
                    if (mode == 2)
                    {
                        WebClient webClient = new WebClient();
                        Console.WriteLine("Scraping Socks5 Proxies.", Color.BlueViolet);
                        proxies = webClient.DownloadString("https://api.proxyscrape.com/v2/?request=getproxies&protocol=socks5&timeout=" + timeout + "&country=all&ssl=all&anonymity=all").Split('\n');
                        Console.WriteLine("Scraped " + proxies.Length + " proxies !", Color.BlueViolet);
                    }
                    break;
            }
            Thread.Sleep(500);
            Console.Clear();
            Console.Write("Checking " + proxies.Length + " " + proxyTypeString + " proxies with " + threads + " threads.\r\n\r\n", Color.BlueViolet);
            Thread.Sleep(750);
            int good = 0;
            int bad = 0;
            int checkedProxies = 0;
            Random cpmHelper = new Random();
            Parallel.ForEach(proxies, new ParallelOptions() { MaxDegreeOfParallelism = threads }, proxy =>
             {
                 try
                 {
                     using (var r = new HttpRequest())
                     {
                         r.ConnectTimeout = timeout;
                         r.Proxy = ProxyClient.Parse(proxyType, proxy);
                         r.Proxy.ConnectTimeout = proxyTimeout;
                         r.Get(config.proxy_test_site);
                         Console.Write("\r\n[GOOD] " + proxy, Color.BlueViolet);
                         good++;
                         checkedProxies++;
                         if (!Directory.Exists(Directory.GetCurrentDirectory() + "Results of " + date)) Directory.CreateDirectory("Results of " + date);
                         cpm.TryAdd(cpmHelper.Next(1, 2147483647), DateTimeOffset.Now.ToUnixTimeSeconds());
                         Console.Title = "ProxyChecker | Made By Wykt | Checked: " + checkedProxies + " | Good: " + good + " | Bad: " + bad + " | CPM: " + getCPM();
                         File.AppendAllText("Results of " +date+ "\\good.txt", proxy.Trim() + Environment.NewLine, System.Text.Encoding.Unicode);
                     }
                 } catch
                 {
                     Console.Write("\n[BAD] " + proxy, Color.DarkRed);
                     bad++;
                     checkedProxies++;
                     cpm.TryAdd(cpmHelper.Next(1, 2147483647), DateTimeOffset.Now.ToUnixTimeSeconds());
                     Console.Title = "ProxyChecker | Made By Wykt | Checked: " + checkedProxies + " | Good: " + good + " | Bad: " + bad + " | CPM: " + getCPM();
                 }
             });
            Console.WriteLine("\n\nDone!\n" + good + " good proxies, " + bad + " bad proxies.", Color.BlueViolet);
            if (config.enableWebhook && good > 0)
            {
                uploadProxiesToWebhook();
            }
            Console.ReadKey();

        }
        public static string date = "";
        public static string proxyTypeString = "";
        public static Config config;
        private static readonly ConcurrentDictionary<long, long> cpm = new ConcurrentDictionary<long, long>();

        private static long getCPM()
        {
            long e = 0;
            foreach(KeyValuePair<long, long> cp in cpm)
            {
                if(cp.Value >= DateTimeOffset.Now.ToUnixTimeSeconds() - 60L)
                {
                    e += 1;
                }
            }
            return e;
        }
        public static async void uploadProxiesToWebhook()
        {
            Console.WriteLine("[INFO] Trying to upload proxies to webhook you set.", Color.BlueViolet);
            try
            {
                DiscordWebhookClient discordWebhookClient = new DiscordWebhookClient(config.webhook_url);
                var message = new DiscordMessage(proxyTypeString + " Proxies.", username: "ProxyChecker | Made By Wykt | Webhook Uploader");
                var file = new DiscordFile(proxyTypeString + "_proxies.txt", System.Text.Encoding.UTF8.GetBytes(File.ReadAllText("Results of " + date + "\\good.txt")));
                DiscordFile[] files = { file };
                Console.WriteLine("[SUCCESS] Proxies uploaded.", Color.BlueViolet);

                await discordWebhookClient.SendToDiscord(message, files);
            } catch
            {
                Console.WriteLine("Invalid Webhook or No Connection!", Color.DarkRed);
            }
        }
    }
}
