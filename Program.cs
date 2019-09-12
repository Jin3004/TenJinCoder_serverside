using System;
using System.IO;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Collections;
using System.Web;
using System.Diagnostics;

namespace serverside
{
    class Program
    {
        const string document_path = "C:/Users/asano/Desktop/Progress/TenJinCoder";
        //Declare constants.

        static void Main(string[] args)
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:3000/");
            listener.Start();
            Environment.CurrentDirectory = document_path;
            Console.WriteLine("Get started.");

            for (; ; )
            {
                try
                {
                    HttpListenerContext context = listener.GetContext();
                    HttpListenerRequest req = context.Request;
                    HttpListenerResponse res = context.Response;
                    if (req.HttpMethod == "GET")
                    {
                        ReturnFile(req, res);
                        res.Close();
                    }
                    else if (req.HttpMethod == "POST")
                    {
                        var read_stream = new StreamReader(req.InputStream, Encoding.UTF8);
                        Console.WriteLine(HttpUtility.UrlDecode(read_stream.ReadToEnd()));
                        var received = ConvertToDictionary(HttpUtility.UrlDecode(read_stream.ReadToEnd()));
                        if (received.ContainsKey("type"))//If this dictionary has "type" key, call various functions.
                        {
                            switch (int.Parse(received["type"]))
                            {
                                case 0:
                                    JudgeSubmittedCode(req, res, received);
                                    break;
                                default:
                                    Console.WriteLine("No match functions.");
                                    break;
                            }

                        }

                        res.Close();
                    }
                }
                catch (Exception ec)
                {
                    Console.WriteLine("Error: " + ec.Message);
                    break;
                }
            }
        }

        static Dictionary<string, string> ConvertToDictionary(string str)//convert string to dictionary.
        {
            var res = new Dictionary<string, string>();
            Console.WriteLine("{");
            string[] separatedByAnd = str.Split('&');
            foreach (var s in separatedByAnd)
            {
                Console.WriteLine(s);
                string[] separatedByEqual = s.Split("=");
                res.Add(separatedByEqual[0], separatedByEqual[1]);
            }
            Console.WriteLine("}");
            return res;
        }

        static void ReturnFile(HttpListenerRequest req, HttpListenerResponse res)//Return the specified file.
        {

            var mime_type = new Dictionary<string, string>();// [extension, mime-type]
            mime_type.Add("txt", "text/plain");
            mime_type.Add("html", "text/html");
            mime_type.Add("css", "text/css");
            mime_type.Add("js", "text/javascript");
            mime_type.Add("json", "application/json");

            string url = "";
            {
                string[] tmp = req.RawUrl.Split("?");
                if (tmp[tmp.Length - 1] == "/") tmp[0] = "/html/index.html";
                url += document_path + tmp[0];
            }
            try
            {
                res.StatusCode = 200;
                {
                    string[] tmp = url.Split('.');
                    string ext = tmp[tmp.Length - 1];
                    res.ContentType = mime_type[ext];
                }
                byte[] content = File.ReadAllBytes(url);
                res.OutputStream.Write(content, 0, content.Length);
            }
            catch (Exception ec)
            {
                res.StatusCode = 404;
                byte[] content = Encoding.UTF8.GetBytes("404 Not Found: " + ec.Message + ".");
                res.OutputStream.Write(content, 0, content.Length);
            }
        }

        static void JudgeSubmittedCode(HttpListenerRequest req, HttpListenerResponse res, Dictionary<string, string> received)
        {
            string codename = "";
            {
                int max = -1;
                var files = Directory.EnumerateFiles("users/" + received["user"] + "/submission", "*.cpp");
                foreach (var f in files)
                {
                    string filename = "";
                    {
                        var tmp = f.Split("/");
                        filename = tmp[tmp.Length - 1].Split(".")[0];
                    }
                    max = Math.Max(max, int.Parse(filename));
                }
                if (max <= 8) codename = "0" + (max + 1).ToString();
                else codename = (max + 1).ToString();
            }//Calculate the next codename.

            {
                Environment.CurrentDirectory = "users/" + received["user"] + "/submission";
                File.WriteAllText(codename + ".cpp", received["code"]);//Create source file.
                var compile = new Process();
                compile.StartInfo.FileName = "g++";
                compile.StartInfo.Arguments = codename + ".cpp -o " + codename;
                compile.Start();
                compile.WaitForExit();
                if (compile.ExitCode != 0)
                {
                    Console.WriteLine("Compile error occurred.");
                    return;
                }
            }//Compile the submitted code.

        }

    }
}