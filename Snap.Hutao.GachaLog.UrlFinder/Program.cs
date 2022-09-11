using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Snap.Hutao.GachaLog.UrlFinder;

public static class Program
{
    private const string myPath = @"C:\Program Files\Genshin Impact\Genshin Impact Game";
    [STAThread]
    public static void Main(string[] args)
    {
        Console.WriteLine(@"祈愿记录Url查找 - 请输入 [YuanShen.exe] 所在文件夹路径, 否则会使用默认地址");
        Console.WriteLine(myPath);
        string path = Console.ReadLine()!.Trim('"');
        if (path.Length < 10){
            path = myPath;
        }
        if(Directory.Exists(Path.Combine(path, "YuanShen_Data")))
        {
            path = Path.Combine(path, @"YuanShen_Data\webCaches\Cache\Cache_Data\data_2");
        }
        else
        {
            path = Path.Combine(path, @"GenshinImpact_Data\webCaches\Cache\Cache_Data\data_2");
        }

        string target = Path.GetTempFileName();
        File.Copy(path, target, true);

        List<string> results = new();

        using (FileStream fileStream = new(target, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            using BinaryReader reader = new(fileStream);
            Regex urlMatch = new("(https.+?game_biz=hk4e.+?)&", RegexOptions.Compiled);

            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                try
                {
                    uint test = reader.ReadUInt32();

                    if (test == 0x2F302F31)
                    {
                        byte[] chars = reader.ReadBytesUntil(b => b == 0x00);
                        string result = Encoding.UTF8.GetString(chars, 0, chars.Length);

                        if (urlMatch.Match(result).Success)
                        {
                            results.Add(result);
                        }

                        int alignment = sizeof(int) * 4;
                        // align up
                        long offset = reader.BaseStream.Position % alignment;
                        reader.BaseStream.Position += (alignment - offset);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        File.Delete(target);
        var r = results.Last<string>()??throw new Exception("url is empty");
        Clipboard.SetText(r);
        Console.WriteLine("URL已经复制到剪贴板");
    }

    public static byte[] ReadBytesUntil(this BinaryReader binaryReader, Func<byte, bool> evaluator)
    {
        return binaryReader.ReadByteEnumerableUntil(evaluator).ToArray();
    }

    private static IEnumerable<byte> ReadByteEnumerableUntil(this BinaryReader binaryReader, Func<byte, bool> evaluator)
    {
        while (binaryReader.BaseStream.Position < binaryReader.BaseStream.Length)
        {
            byte b = binaryReader.ReadByte();
            if (evaluator(b))
            {
                yield break;
            }
            else
            {
                yield return b;
            }
        }
    }
}