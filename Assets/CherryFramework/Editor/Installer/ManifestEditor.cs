using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CherryFramework.Installer
{
    /// <summary>
    /// Reads and patches the "dependencies" object of a Unity package manifest.
    /// Only that object is touched; every other section is copied byte for byte.
    /// </summary>
    public static class ManifestEditor
    {
        public const string ManifestRelativePath = "Packages/manifest.json";

        public static string Read(string projectRoot)
        {
            var path = Path.Combine(projectRoot, ManifestRelativePath.Replace('/', Path.DirectorySeparatorChar));

            return File.Exists(path) ? File.ReadAllText(path) : "{ \"dependencies\": {} }";
        }

        public static void Write(string projectRoot, string manifestJson)
        {
            var path = Path.Combine(projectRoot, ManifestRelativePath.Replace('/', Path.DirectorySeparatorChar));
            var directory = Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, manifestJson);
        }

        public static IReadOnlyDictionary<string, string> ReadPackages(string manifestJson)
        {
            var result = new Dictionary<string, string>();
            var block = FindDependenciesBlock(manifestJson, out _);

            if (block == null)
            {
                return result;
            }

            foreach (var entry in ParseEntries(block))
            {
                result[entry.Key] = entry.Value;
            }

            return result;
        }

        public static string SetPackages(string manifestJson, IReadOnlyList<PackageEntry> packages)
        {
            if (packages == null || packages.Count == 0)
            {
                return manifestJson;
            }

            var wanted = new Dictionary<string, string>();

            foreach (var package in packages)
            {
                if (package == null || string.IsNullOrEmpty(package.id))
                {
                    continue;
                }

                wanted[package.id] = package.version;
            }

            if (wanted.Count == 0)
            {
                return manifestJson;
            }

            var block = FindDependenciesBlock(manifestJson, out var blockStart);
            var body = block ?? "{}";

            var kept = new List<string>();

            foreach (var entry in ParseEntries(body))
            {
                if (wanted.ContainsKey(entry.Key))
                {
                    continue;
                }

                kept.Add(entry.Raw);
            }

            foreach (var pair in wanted)
            {
                kept.Add("\"" + pair.Key + "\": \"" + pair.Value + "\"");
            }

            var rebuilt = BuildBlock(kept, body);

            if (block == null)
            {
                return InsertDependenciesBlock(manifestJson, rebuilt);
            }

            return manifestJson.Substring(0, blockStart) + rebuilt +
                   manifestJson.Substring(blockStart + body.Length);
        }

        private struct Entry
        {
            public string Key;
            public string Value;
            public string Raw;
        }

        private static string BuildBlock(List<string> entries, string originalBody)
        {
            var builder = new StringBuilder();

            if (entries.Count == 0)
            {
                return "{}";
            }

            var indentation = GuessIndentation(originalBody);

            builder.Append("{\n");

            for (var index = 0; index < entries.Count; index++)
            {
                builder.Append(indentation).Append(entries[index]);

                if (index < entries.Count - 1)
                {
                    builder.Append(",");
                }

                builder.Append("\n");
            }

            builder.Append(GuessClosingIndentation(originalBody)).Append("}");

            return builder.ToString();
        }

        private static List<Entry> ParseEntries(string body)
        {
            var entries = new List<Entry>();
            var inner = body.Trim().TrimStart('{').TrimEnd('}');
            var index = 0;

            while (index < inner.Length)
            {
                while (index < inner.Length && (char.IsWhiteSpace(inner[index]) || inner[index] == ','))
                {
                    index++;
                }

                if (index >= inner.Length || inner[index] == '}')
                {
                    break;
                }

                var keyStart = index;
                var key = ReadString(inner, ref index);

                if (key == null)
                {
                    break;
                }

                while (index < inner.Length && char.IsWhiteSpace(inner[index]))
                {
                    index++;
                }

                if (index >= inner.Length || inner[index] != ':')
                {
                    break;
                }

                index++;

                while (index < inner.Length && char.IsWhiteSpace(inner[index]))
                {
                    index++;
                }

                var value = ReadString(inner, ref index);

                if (value == null)
                {
                    break;
                }

                var rawEnd = index;

                while (rawEnd < inner.Length && char.IsWhiteSpace(inner[rawEnd]))
                {
                    rawEnd++;
                }

                entries.Add(new Entry
                {
                    Key = key,
                    Value = value,
                    Raw = inner.Substring(keyStart, rawEnd - keyStart)
                });

                index = rawEnd;
            }

            return entries;
        }

        private static string ReadString(string text, ref int index)
        {
            if (index >= text.Length || text[index] != '"')
            {
                return null;
            }

            var builder = new StringBuilder();
            index++;

            while (index < text.Length)
            {
                var character = text[index];

                if (character == '\\' && index + 1 < text.Length)
                {
                    builder.Append(text[index + 1]);
                    index += 2;
                    continue;
                }

                if (character == '"')
                {
                    index++;
                    return builder.ToString();
                }

                builder.Append(character);
                index++;
            }

            return null;
        }

        private static string FindDependenciesBlock(string manifestJson, out int start)
        {
            start = -1;

            var keyIndex = manifestJson.IndexOf("\"dependencies\"", StringComparison.Ordinal);

            if (keyIndex < 0)
            {
                return null;
            }

            var cursor = manifestJson.IndexOf(':', keyIndex);

            if (cursor < 0)
            {
                return null;
            }

            cursor++;

            while (cursor < manifestJson.Length && char.IsWhiteSpace(manifestJson[cursor]))
            {
                cursor++;
            }

            if (cursor >= manifestJson.Length || manifestJson[cursor] != '{')
            {
                return null;
            }

            var depth = 0;
            var inString = false;

            for (var index = cursor; index < manifestJson.Length; index++)
            {
                var character = manifestJson[index];

                if (inString)
                {
                    if (character == '\\')
                    {
                        index++;
                    }
                    else if (character == '"')
                    {
                        inString = false;
                    }

                    continue;
                }

                if (character == '"')
                {
                    inString = true;
                }
                else if (character == '{')
                {
                    depth++;
                }
                else if (character == '}')
                {
                    depth--;

                    if (depth == 0)
                    {
                        start = cursor;
                        return manifestJson.Substring(cursor, index - cursor + 1);
                    }
                }
            }

            return null;
        }

        private static string InsertDependenciesBlock(string manifestJson, string block)
        {
            var closing = manifestJson.LastIndexOf('}');

            if (closing < 0)
            {
                return "{\n  \"dependencies\": " + block + "\n}";
            }

            var head = manifestJson.Substring(0, closing).TrimEnd();

            if (!head.EndsWith("{", StringComparison.Ordinal))
            {
                head += ",";
            }

            return head + "\n  \"dependencies\": " + block + "\n" + manifestJson.Substring(closing);
        }

        private static string GuessIndentation(string body)
        {
            return GuessClosingIndentation(body) + "  ";
        }

        private static string GuessClosingIndentation(string body)
        {
            foreach (var line in body.Split('\n'))
            {
                if (line.Trim() == "}")
                {
                    return line.Substring(0, line.Length - line.TrimStart().Length);
                }
            }

            return "  ";
        }
    }
}
