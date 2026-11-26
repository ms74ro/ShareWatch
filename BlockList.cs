// Lista de blocare: fisierul extern blocklist.txt (langa exe). Calculatoarele din lista sunt deconectate imediat.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ShareWatch
{
    static class BlockList
    {
        public class Entry
        {
            public string[] Tokens;   // IP-uri si/sau nume de calculator
            public string Comment;
            public string Line;       // linia originala (pentru stergere)
        }

        static readonly object gate = new object();
        static List<Entry> entries = new List<Entry>();
        static DateTime stamp = DateTime.MinValue;
        static long len = -2;
        public static volatile int Version;   // creste la fiecare reincarcare (fereastra stie cand sa reafiseze)

        public static string FilePath { get { return Path.Combine(Settings.Dir, "blocklist.txt"); } }

        const string Header =
"# ShareWatch - block list\r\n" +
"# One computer per line: its IP address and/or computer name, separated by spaces.\r\n" +
"# Text after # is a comment. Blocked computers are disconnected as soon as they connect.\r\n" +
"# Example:\r\n" +
"#   192.168.1.50 PC-NAME   # reason\r\n";

        public static int Count { get { lock (gate) return entries.Count; } }

        public static List<Entry> Entries { get { lock (gate) return new List<Entry>(entries); } }

        // apelat de monitor la fiecare ciclu: reincarca fisierul doar daca s-a schimbat
        public static void ReloadIfChanged()
        {
            lock (gate)
            {
                try
                {
                    FileInfo fi = new FileInfo(FilePath);
                    DateTime t = fi.Exists ? fi.LastWriteTimeUtc : DateTime.MinValue;
                    long l = fi.Exists ? fi.Length : -1;
                    if (t == stamp && l == len) return;
                    List<Entry> list = new List<Entry>();
                    if (fi.Exists)
                    {
                        string[] lines;
                        using (FileStream fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                        using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
                            lines = sr.ReadToEnd().Replace("\r\n", "\n").Split('\n');
                        foreach (string line in lines)
                        {
                            int h = line.IndexOf('#');
                            string data = h >= 0 ? line.Substring(0, h) : line;
                            string comment = h >= 0 ? line.Substring(h + 1).Trim() : "";
                            string[] tokens = data.Split(new char[] { ' ', '\t', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                            for (int i = 0; i < tokens.Length; i++) tokens[i] = tokens[i].TrimStart('\\');
                            if (tokens.Length == 0) continue;
                            list.Add(new Entry { Tokens = tokens, Comment = comment, Line = line });
                        }
                    }
                    entries = list;
                    stamp = t; len = l;   // doar dupa o citire reusita (altfel reincercam la urmatorul ciclu)
                    Version++;
                }
                catch { }
            }
        }

        static bool Eq(string token, string value)
        {
            return !string.IsNullOrEmpty(value) && value != "?" && string.Equals(token, value, StringComparison.OrdinalIgnoreCase);
        }

        public static bool Match(string raw, Peer p)
        {
            if (!Settings.BlockEnabled) return false;
            string r = raw == null ? null : raw.TrimStart('\\');
            lock (gate)
            {
                foreach (Entry e in entries)
                    foreach (string t in e.Tokens)
                        if (Eq(t, r) || (p != null && (Eq(t, p.Ip) || Eq(t, p.Host)))) return true;
            }
            return false;
        }

        static void EnsureFile()
        {
            if (!File.Exists(FilePath)) File.WriteAllText(FilePath, Header, new UTF8Encoding(false));
        }

        public static void OpenFileInEditor()
        {
            try
            {
                lock (gate) EnsureFile();
                System.Diagnostics.Process.Start("notepad.exe", "\"" + FilePath + "\"");
            }
            catch { }
        }

        // adauga o linie: "token1 token2   # comentariu"
        public static bool Add(string tokens, string comment)
        {
            lock (gate)
            {
                try
                {
                    EnsureFile();
                    string existing = File.ReadAllText(FilePath, Encoding.UTF8);
                    string line = tokens.Trim() + (string.IsNullOrEmpty(comment) ? "" : "   # " + comment);
                    string prefix = existing.Length > 0 && !existing.EndsWith("\n") ? "\r\n" : "";
                    File.AppendAllText(FilePath, prefix + line + "\r\n", new UTF8Encoding(false));
                    stamp = DateTime.MinValue;   // fortam reincarcarea
                }
                catch { return false; }
            }
            ReloadIfChanged();
            return true;
        }

        public static bool Remove(Entry e)
        {
            lock (gate)
            {
                try
                {
                    string[] lines = File.ReadAllText(FilePath, Encoding.UTF8).Replace("\r\n", "\n").Split('\n');
                    StringBuilder sb = new StringBuilder();
                    bool removed = false;
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (!removed && lines[i] == e.Line) { removed = true; continue; }
                        if (i == lines.Length - 1 && lines[i].Length == 0) break;   // fara linie goala in plus la final
                        sb.Append(lines[i]).Append("\r\n");
                    }
                    File.WriteAllText(FilePath, sb.ToString(), new UTF8Encoding(false));
                    stamp = DateTime.MinValue;
                }
                catch { return false; }
            }
            ReloadIfChanged();
            return true;
        }
    }
}
