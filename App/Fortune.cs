/*
 * Copyright (c) 2019 Leonardo Pessoa
 * https://lmpessoa.com
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Lmpessoa.Fortune {

   struct Fortune {
      private const string DATX = ".datx";

      internal string Text { get; }
      internal string Author { get; }

      private Fortune(string text, string author) {
         this.Text = text ?? "No fortune for you today.";
         this.Author = author ?? "";
      }

      internal static Fortune Get(string path) {
         Dictionary<string, int> sources = GetUpdatedFortuneSources(path);
         int index = GetRandomFortuneIndexFrom(sources);
         return GetFortuneTextFrom(sources, index);
      }

      private static Dictionary<string, int> GetUpdatedFortuneSources(string path) {
         Dictionary<string, int> result = new Dictionary<string, int>();
         string[] files = Directory.GetFiles(path);
         foreach (string file in files) {
            if (!file.EndsWith(DATX)) {
               if (!File.Exists(file + DATX) || File.GetLastWriteTime(file) > File.GetLastWriteTime(file + DATX)) {
                  using (FileStream source = new FileStream(file, FileMode.Open)) {
                     using (BinaryWriter data = new BinaryWriter(new FileStream(file + DATX, FileMode.Create))) {
                        long prevStart = 0;
                        if (IsBomfStart(source)) {
                           prevStart = 3;
                        }
                        while (prevStart > -1) {
                           var pos = FindNextFortuneBreak(source);
                           long len = pos.PrevEnd - prevStart;
                           if (len <= short.MaxValue) {
                              data.Write(prevStart);
                              data.Write((short) len);
                           }
                           prevStart = pos.NextStart;
                        }
                        result.Add(file, (int) data.BaseStream.Length / 10);
                     }
                  }
               } else {
                  long entries = new FileInfo(file + DATX).Length / 10;
                  result.Add(file, (int) entries);
               }
            }
         }
         return result;
      }

      private static bool IsBomfStart(Stream source) {
         byte[] bomf = new byte[3];
         if (source.Read(bomf, 0, 3) == 3) {
            return bomf[0] == 0xEF && bomf[1] == 0xBB && bomf[2] == 0xBF;
         }
         return false;
      }

      private struct BreakPoint {
         internal readonly long PrevEnd, NextStart;

         internal BreakPoint(long prevEnd, long nextStart) {
            PrevEnd = prevEnd;
            NextStart = nextStart;
         }
      }

      private static BreakPoint FindNextFortuneBreak(Stream source) {
         byte[] prev = new byte[2];
         long end, start;
         while (source.Position < source.Length) {
            int b = source.ReadByte();
            try {
               if (b == '%') {
                  if (prev[0] == '\r' && prev[1] == '\n') {
                     end = source.Position - 3;
                  } else if (prev[1] == '\r' || prev[1] == '\n') {
                     end = source.Position - 2;
                  } else {
                     continue;
                  }
                  b = source.ReadByte();
                  if (b == '\r') {
                     b = source.ReadByte();
                     if (b == '\n') {
                        start = source.Position;
                     } else {
                        start = source.Position -= 1;
                     }
                  } else if (b == '\n') {
                     start = source.Position;
                  } else {
                     continue;
                  }
                  return new BreakPoint(end, start);
               }
            } finally {
               prev[0] = prev[1];
               prev[1] = (byte) b;
            }
         }
         return new BreakPoint(source.Length - 1, -1);
      }

      private static int GetRandomFortuneIndexFrom(Dictionary<string, int> sources) {
         int maxValue = 0;
         foreach (KeyValuePair<string, int> file in sources) {
            maxValue += file.Value;
         }
         return new Random().Next(maxValue);
      }

      private static Fortune GetFortuneTextFrom(Dictionary<string, int> sources, int index) {
         foreach (KeyValuePair<string, int> file in sources) {
            if (index < file.Value) {
               long pos;
               short len;
               using (BinaryReader data = new BinaryReader(new FileStream(file.Key + DATX, FileMode.Open))) {
                  data.BaseStream.Position = index * 10;
                  pos = data.ReadInt64();
                  len = data.ReadInt16();
               }
               using (Stream data = new FileStream(file.Key, FileMode.Open)) {
                  byte[] buff = new byte[len];
                  data.Position = pos;
                  data.Read(buff, 0, len);
                  string text = System.Text.Encoding.UTF8.GetString(buff);
                  text = Regex.Replace(text, "\r(?!\n)", "\r\n");
                  string[] lines = text.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                  string author = "";
                  if (lines[lines.Length - 1].Trim().StartsWith("--")) {
                     author = lines[lines.Length - 1].Trim().Substring(2).Trim();
                     do {
                        Array.Resize(ref lines, lines.Length - 1);
                     } while (lines[lines.Length - 1].Trim() == "");
                     text = string.Join("\r\n", lines);
                  }
                  return new Fortune(text, author);
               }
            } else {
               index -= file.Value;
            }
         }
         return new Fortune(null, null);
      }
   }
}
