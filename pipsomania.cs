using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace pipsomania
{
    class Program
    {
        static void Main(string[] args)
        {
            string filename = (args?.Length ?? 0) == 0 ? "main.php" : args[0];
            if (!File.Exists(filename))
            {
                Console.WriteLine("ファイル({0})が見つかりませんでした", filename);
                Console.ReadKey();
                return;
            }
            string original = File.ReadAllText(filename);
            //phase1
            string source0_5 = Regex.Replace(original, @"\\x([0-9a-fA-F][0-9a-fA-F])", f => Convert.ToChar(Convert.ToInt32(f.Groups[1].Value, 16)).ToString());
            string source1 = Regex.Replace(source0_5, @"\\([0-9a-fA-F][0-9a-fA-F])", f => Convert.ToChar(Convert.ToInt32(f.Groups[1].Value, 8)).ToString());
            //phase2
            string source2 = new string(source1.ToCharArray());
            foreach (var m in Regex.Matches(source1, @"(?<dummy>\${""GLOBALS""}\[""[a-z]+""\])=""(?<name>[_a-zA-Z][_0-9a-zA-Z]*)"";").Cast<Match>())
                source2 = source2.Replace(m.Value, "").Replace(m.Groups["dummy"].Value, m.Groups["name"].Value).Replace("${" + m.Groups["name"].Value + "}", "$" + m.Groups["name"].Value);
            //phase3
            string source3 = new string(source2.ToCharArray());
            foreach(var m in Regex.Matches(source2, @"(?<dummy>\$[a-z]+)=""(?<name>[a-zA-Z][0-9a-zA-Z]*)"";").Cast<Match>())
                source3 = source3.Replace(m.Value, "").Replace($"${{{m.Groups["dummy"].Value}}}", $"${m.Groups["name"].Value}");
            //final
            string final = SimpleFormat(source3);
            File.WriteAllText(new FileInfo(filename).DirectoryName + "\\RESULT.PHP", final);
            Console.WriteLine(final);
            Console.ReadKey();
        }

        static string SimpleFormat(string source)
        {
            string tmp = source.Replace("; ", ";").Replace(";", ";\n");
            tmp = tmp.Replace("{ ", "{").Replace("{", "{\n");
            tmp = tmp.Replace("} ", "}").Replace("}", "}\n");
            tmp = tmp.Replace("class ", "\nclass ");
            tmp = Regex.Replace(tmp, " (namespace [a-zA-Z0-9_]*;)", "\n$1\n");
            tmp = Regex.Replace(tmp, "(case[ ]*'([^'\\\\]|\\\\\\\\|\\\\')*?'):[ ]*", "$1:\n");
            tmp = Regex.Replace(tmp, "(case[ ]*\"([^\"\\\\]|\\\\\\\\|\\\\')*?\"):[ ]*", "$1:\n");
            int indent = 0;
            return string.Join("\n", tmp.Split('\n').Select(line =>
            {
                indent -= line.Count(c => c == '}');
                string result = new string('\t', indent) + line;
                indent += line.Count(c => c == '{');
                indent += Regex.IsMatch(result, "case[ ]*'([^'\\\\]|\\\\\\\\|\\\\')*?':") ? 1 : 0;
                indent += Regex.IsMatch(result, "case[ ]*\"([^\"\\\\]|\\\\\\\\|\\\\')*?\":") ? 1 : 0;
                indent -= line.Contains("break;") ? 1 : 0;
                return result;
            }));
        }
    }
}
