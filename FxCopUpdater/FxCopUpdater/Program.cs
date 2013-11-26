using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Diagnostics;

namespace FxCopUpdater
{
    class Program
    {
        class ParsedArgs
        {
            public string TemplateFile { get; set; }
            public string TargetsDirectory { get; set; }
        }

        static void Main(string[] args)
        {
            var pa = ParseArgs(args);
            UpdateProjectFile(pa);
        }

        private static void UpdateProjectFile(ParsedArgs pa)
        {
            //load the project file
            var template = new System.Xml.XmlDocument();
            template.Load(pa.TemplateFile);

            //update it

            RemoveTargets(template);
            AddTargets(template, pa.TargetsDirectory);

            UpdateReferencePaths(template);

            //write it back to disk

            var output = new System.Xml.XmlDocument();
            foreach (var node in template.ChildNodes.Cast<XmlNode>().Where(w => w.NodeType != XmlNodeType.XmlDeclaration))
            {
                var importNode = output.ImportNode(node, true);
                output.AppendChild(importNode);
            }

            using (var xWriter = XmlWriter.Create(pa.TemplateFile, new XmlWriterSettings() { Indent = true, IndentChars = " " }))
            {
                output.WriteTo(xWriter);
            }
        }

        /// <summary>
        /// Just remove all the targets from the project file
        /// </summary>
        private static void RemoveTargets(XmlNode node)
        {
            var toRemove = new List<XmlNode>();

            foreach (var child in node.ChildNodes.Cast<XmlNode>())
            {
                if (child.Name.Equals("Target", StringComparison.OrdinalIgnoreCase)
                    && child.ParentNode != null && child.ParentNode.Name.Equals("Targets", StringComparison.OrdinalIgnoreCase)
                    && child.ParentNode.ParentNode != null && child.ParentNode.ParentNode.Name.Equals("FxCopProject", StringComparison.OrdinalIgnoreCase))
                {
                    toRemove.Add(child);
                }
            }

            foreach (var tr in toRemove)
            {
                node.RemoveChild(tr);
            }

            foreach (var child in node.ChildNodes.Cast<XmlNode>())
            {
                RemoveTargets(child);
            }
        }

        /// <summary>
        /// Any .dll in the specified directory, add it to the project as a target
        /// </summary>
        private static void AddTargets(XmlDocument template, string targetsDirectory)
        {
            var targetsNode = template.SelectSingleNode("FxCopProject/Targets");

            foreach (var dll in Directory.GetFiles(targetsDirectory).Where(w => w.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)))
            {
                string path;
                if (dll.StartsWith(".."))
                    path = "$(ProjectDir)\\" + dll;
                else if (dll.StartsWith("\\"))
                    path = "$(ProjectDir)" + dll;
                else
                    path = dll;

                var target = template.CreateElement("Target");
                target.SetAttribute("Name", path);
                target.SetAttribute("Analyze", "True");
                target.SetAttribute("AnalyzeAllChildren", "True");

                targetsNode.AppendChild(target);

                Console.WriteLine("adding target: " + path);
            }
        }

        /// <summary>
        /// Make sure all the reference folders are in the project
        /// </summary>
        private static void UpdateReferencePaths(XmlDocument template)
        {
            var refsNode = template.SelectSingleNode("FxCopProject/Targets/AssemblyReferenceDirectories");

            var toRemove = refsNode.ChildNodes.Cast<XmlNode>().Where(w => w.Name.Equals("Directory", StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var tr in toRemove) refsNode.RemoveChild(tr);

			// stuff that may be relevant to your organization/setup
            // AppendDirectoryNode(template, refsNode, "$(ProjectDir)/../../whatever/lib/");

            AppendDirectoryNode(template, refsNode, ProgramFilesx86Path() + "/Microsoft ASP.NET/ASP.NET MVC 3/Assemblies/");
            AppendDirectoryNode(template, refsNode, ProgramFilesx86Path() + "/Microsoft ASP.NET/ASP.NET MVC 4/Assemblies/");
            AppendDirectoryNode(template, refsNode, ProgramFilesx86Path() + "/Microsoft ASP.NET/ASP.NET Web Pages/v2.0/Assemblies/");
        }

        private static void AppendDirectoryNode(XmlDocument template, XmlNode refsNode, string path)
        {
            var dir = template.CreateNode(XmlNodeType.Element, "Directory", null);
            dir.InnerText = path;
            refsNode.AppendChild(dir);
        }

        private static string ProgramFilesx86Path()
        {
            if (8 == IntPtr.Size || (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"))))
            {
                return Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            }

            return Environment.GetEnvironmentVariable("ProgramFiles");
        }

        private static string RunFxCopProcess(string args)
        {
            var p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = "C:\\Program Files (x86)\\Microsoft FxCop 10.0\\fxcopcmd.exe";
            p.StartInfo.Arguments = args;

            p.Start();

            string output = p.StandardOutput.ReadToEnd();

            p.WaitForExit();
            return output;
        }

        private static ParsedArgs ParseArgs(string[] args)
        {
            var pa = new ParsedArgs();
            if (args.Length >= 2)
            {
                if (File.Exists(args[0]))
                    pa.TemplateFile = args[0];
                if (Directory.Exists(args[1]))
                    pa.TargetsDirectory = args[1];
            }

            if (pa.TemplateFile == null || pa.TargetsDirectory == null)
            {
                Console.WriteLine("Usage: FxCopUpdater [FxCop rules/exclusions file] [targets directory]");
                Environment.Exit(1);
            }

            if (pa.TargetsDirectory.EndsWith("\\")) pa.TargetsDirectory = pa.TargetsDirectory.TrimEnd('\\');

            return pa;
        }
    }
}
