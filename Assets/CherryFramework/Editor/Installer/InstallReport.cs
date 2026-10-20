using System.Collections.Generic;
using System.Text;

namespace CherryFramework.Installer
{
    public sealed class InstallReport
    {
        public readonly List<string> Lines = new List<string>();
        public bool Success = true;

        public void Add(string line)
        {
            Lines.Add(line);
        }

        public void Fail(string line)
        {
            Success = false;
            Lines.Add(line);
        }

        public string Summary()
        {
            var builder = new StringBuilder();

            foreach (var line in Lines)
            {
                builder.AppendLine(line);
            }

            return builder.ToString();
        }
    }
}
