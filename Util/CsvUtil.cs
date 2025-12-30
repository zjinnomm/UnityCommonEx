using System.Collections.Generic;
using System.Text;

namespace UnityCommonEx
{

    public static class CsvUtil
    {

        const char QuoteChar = '\"';
        const char DelimiterChar = ',';

        static IList<string[]> Read(string[] lines)
        {
            IList<string[]> result = new List<string[]>();

            List<string> lineSegs = new List<string>();
            StringBuilder segBuilder = new StringBuilder();
            bool quoted;
            bool quoteInQuoted;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].TrimEnd();
                lineSegs.Clear();
                segBuilder.Clear();
                quoted = false;
                quoteInQuoted = false;

                for (int j = 0; j < line.Length; j++)
                {
                    char c = line[j];
                    if (quoted)
                    {
                        if (quoteInQuoted)
                        {
                            if (c == QuoteChar)
                            {
                                segBuilder.Append(QuoteChar);
                                quoteInQuoted = false;
                            }
                            else if (c == DelimiterChar)
                            {
                                lineSegs.Add(segBuilder.ToString());
                                segBuilder.Clear();
                                quoteInQuoted = false;
                                quoted = false;
                            }
                            else
                            {
                                LogUtil.Error("unexpected char '{0}' at line {1} col {2}", c, i, j);
                            }
                        }
                        else
                        {
                            if (c == QuoteChar)
                            {
                                quoteInQuoted = true;
                            }
                            else
                            {
                                segBuilder.Append(c);
                            }
                        }
                    }
                    else
                    {
                        if (c == DelimiterChar)
                        {
                            lineSegs.Add(segBuilder.ToString());
                            segBuilder.Clear();
                        }
                        else if (c == QuoteChar)
                        {
                            if (segBuilder.Length > 0)
                            {
                                LogUtil.Error("unexpected quote at line {0} col {1}", i, j);
                            }
                            quoted = true;
                        }
                        else
                        {
                            segBuilder.Append(c);
                        }
                    }
                }

                lineSegs.Add(segBuilder.ToString());
                result.Add(lineSegs.ToArray());
            }
            return result;
        }

        public static IList<string[]> ReadCsvRaw(string content)
        {
            return Read(content.Split('\n'));
        }

        public static IList<string[]> ReadCsv(string path)
        {
            return Read(System.IO.File.ReadAllLines(path));
        }

    }

}