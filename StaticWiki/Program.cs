﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Markdig;

namespace StaticWiki
{
    class FileInfo
    {
        public string baseName, text;
    };

    class Program
    {
        private static string MarkdownStrippedString(string markdownString, MarkdownPipeline pipeline)
        {
            return Markdown.ToHtml(markdownString.Replace("_", " "), pipeline).Replace("<p>", "").Replace("</p>", "");
        }

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("StaticWiki started with no options. The options available are:");
                Console.WriteLine("StaticWiki -from FromDirectory -to ToDirectory -theme themefile -title title");
                Console.WriteLine();
                Console.WriteLine("From Directory should contain multiple .txt files that contain Markdown code");
                Console.WriteLine("Theme file should specify a text file file with special section keywords to replace with page contents.");
                Console.WriteLine("Processed pages will have the same extension as the theme file.");
                Console.WriteLine("Title is the base page title - It will become be \"Title - Current Page Title\"");
                Console.WriteLine("Current Page Title will have \"_\"'s removed");
                Console.WriteLine("Special sections are:");
                Console.WriteLine("\t\t{TITLE} - should be placed on the <title> tag");
                Console.WriteLine("\t\t{CONTENT} - should be placed where you want the page content to show");
                Console.WriteLine("\t\t{CATEGORIES} - should be placed where you want the category listings to show");
                Console.WriteLine("\t\t{SEARCHNAMES} - A list of javascript strings containing the page names");
                Console.WriteLine("\t\t{SEARCHADDRESSES} - A list of javascript strings containing the page addresses");
            }

            var fromDirectory = ".";
            var toDirectory = "./Out/";
            var themeFileName = "";
            var basePageTitle = "TEMPLATE";

            var fileCache = new Dictionary<string, FileInfo>();

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-from" && i + 1 < args.Length)
                {
                    fromDirectory = args[i + 1];
                }
                else if (args[i] == "-to" && i + 1 < args.Length)
                {
                    toDirectory = args[i + 1];
                }
                else if (args[i] == "-theme" && i + 1 < args.Length)
                {
                    themeFileName = args[i + 1];
                }
                else if (args[i] == "-title" && i + 1 < args.Length)
                {
                    basePageTitle = args[i + 1];
                }
            }

            var pipeline = new MarkdownPipelineBuilder().Build();

            Console.WriteLine("StaticWiki starting up with values:");
            Console.WriteLine(string.Format("From Directory: \"{0}\"", fromDirectory));
            Console.WriteLine(string.Format("To Directory: \"{0}\"", toDirectory));
            Console.WriteLine(string.Format("Theme File: \"{0}\"", themeFileName));
            Console.WriteLine(string.Format("Base Page Title: \"{0}\"", basePageTitle));

            string[] files = new string[0];

            try
            {
                files = Directory.GetFiles(fromDirectory, "*.txt");
            }
            catch(Exception)
            {
                Console.WriteLine(string.Format("StaticWiki failed to find files at the directory '{0}'", fromDirectory));
            }

            var themeText = "";

            try
            {
                StreamReader In = new StreamReader(themeFileName);

                themeText = In.ReadToEnd();
            }
            catch (Exception e)
            {
                Console.WriteLine(string.Format("Failed to read theme file \"{0}\": {1}", themeFileName, e.Message));

                return;
            }

            var pageExtension = Path.GetExtension(themeFileName);

            try
            {
                if(!Directory.Exists(toDirectory))
                {
                    Directory.CreateDirectory(toDirectory);
                }
            }
            catch(Exception)
            {
            }

            Console.WriteLine(string.Format("Processing {0} files", files.Length));

            var categoriesText = "";

            for (int i = 0; i < files.Length; i++)
            {
                var baseName = files[i].Substring(fromDirectory.Length + 1);
                baseName = baseName.Substring(0, baseName.LastIndexOf("."));

                var outName = toDirectory + "/" + baseName + pageExtension;

                Console.WriteLine("... " + files[i] + "(as " + outName + ")");

                try
                {
                    var inReader = new StreamReader(files[i]);

                    var content = inReader.ReadToEnd();

                    if (baseName.Equals("CATEGORIES", StringComparison.InvariantCultureIgnoreCase))
                    {
                        categoriesText = Markdown.ToHtml(content, pipeline);
                    }
                    else
                    {
                        var fileInfo = new FileInfo();
                        fileInfo.baseName = baseName;
                        fileInfo.text = content;

                        fileCache.Add(baseName, fileInfo);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(string.Format("Failed to process file '{0}': {1}", files[i], e.Message));

                    continue;
                }
            }

            var searchNames = new StringBuilder();
            var searchURLs = new StringBuilder();

            foreach (var pair in fileCache)
            {
                searchNames.Append((searchNames.Length > 0 ? ", " : "") + "\"" + MarkdownStrippedString(pair.Key, pipeline).Replace("\n", "") + "\"");
                searchURLs.Append((searchURLs.Length > 0 ? ", " : "") + "\"" + pair.Value.baseName + "\"");
            }

            for (int i = 0; i < files.Length; i++)
            {
                var baseName = files[i].Substring(fromDirectory.Length + 1);
                baseName = baseName.Substring(0, baseName.LastIndexOf("."));

                var outName = Path.GetFullPath(toDirectory + "/" + baseName + pageExtension);

                if (baseName.Equals("CATEGORIES", StringComparison.InvariantCultureIgnoreCase) || !fileCache.ContainsKey(baseName))
                    continue;

                var fileInfo = fileCache[baseName];
                var outText = Markdown.ToHtml(fileInfo.text, pipeline);

                int index = 0;

                var processedTitle = Markdown.ToHtml(basePageTitle + ": " + baseName.Replace("_", " "), pipeline).Replace("<p>", "").Replace("</p>", "");

                try
                {
                    var outWriter = new StreamWriter(outName);

                    var finalText = (string)themeText.Clone();

                    index = finalText.IndexOf("{TITLE}");

                    while (index != -1)
                    {
                        finalText = finalText.Substring(0, index) +  processedTitle + finalText.Substring(index + "{TITLE}".Length);

                        index = finalText.IndexOf("{TITLE}");
                    };

                    index = finalText.IndexOf("{SEARCHNAMES}");

                    while (index != -1)
                    {
                        finalText = finalText.Substring(0, index) + searchNames + finalText.Substring(index + "{SEARCHNAMES}".Length);

                        index = finalText.IndexOf("{SEARCHNAMES}");
                    };

                    index = finalText.IndexOf("{SEARCHURLS}");

                    while (index != -1)
                    {
                        finalText = finalText.Substring(0, index) + searchURLs + finalText.Substring(index + "{SEARCHURLS}".Length);

                        index = finalText.IndexOf("{SEARCHURLS}");
                    };

                    index = finalText.IndexOf("{CATEGORIES}");

                    if (index != -1)
                    {
                        finalText = finalText.Substring(0, index) + categoriesText + finalText.Substring(index + "{CATEGORIES}".Length);
                    };

                    index = finalText.IndexOf("{CONTENT}");

                    if (index != -1)
                    {
                        finalText = finalText.Substring(0, index) + outText + finalText.Substring(index + "{CONTENT}".Length);
                    };

                    outWriter.Write(finalText);
                    outWriter.Flush();
                    outWriter.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine(string.Format("Failed to process file '{0}': {1}", files[i], e.Message));

                    continue;
                }
            }

            Console.WriteLine("OK!");
        }
    }
}
