using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Snap.Hutao.GachaLog.UrlFinder;

public static class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine(@"祈愿记录Url查找 - 请输入 [YuanShen.exe] 所在文件夹路径");
        Console.WriteLine(@"如(D:\Game\Genshin Impact\Genshin Impact Game)");
        string path = Console.ReadLine()!.Trim('"');
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
            using (BinaryReader reader = new(fileStream))
            {
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    try
                    {
                        uint test = reader.ReadUInt32();

                        if (test == 0x2F302F31)
                        {
                            byte[] chars = reader.ReadBytesUntil(b => b == 0x00);
                            string result = Encoding.UTF8.GetString(chars, 0, chars.Length);

                            if (result.StartsWith("https://webstatic.mihoyo.com/hk4e/event/e20190909gacha-v2/index.html")
                                || result.StartsWith("https://hk4e-api.mihoyo.com/event/gacha_info/api/getGachaLog"))
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
        }

        File.Delete(target);
        results.Reverse();

        JsonSerializerOptions options = new() 
        {
            WriteIndented = true, 
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        string text = JsonSerializer.Serialize(results, new JsonSerializerOptions() { WriteIndented = true });
        text = text.Replace("\\u0026", "&");

        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string output = Path.Combine(desktop, "GachaLogUrls.json");
        File.WriteAllText(output, text);

        Console.WriteLine(@"全部结果已经复制到桌面上的 [GachaLogUrls.json] 文件，靠前的为最新的Url。");
        _ = Console.ReadLine();
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