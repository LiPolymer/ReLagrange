using System.Text.RegularExpressions;
using Gma.QrCodeNet.Encoding;

namespace ReLagrange;

public class ReLaUtils
{
    public static void ConsoleQr(string content)
    {
        QrEncoder qrEncoder = new QrEncoder(ErrorCorrectionLevel.L);
        QrCode qrCode = qrEncoder.Encode(content);
        Console.BackgroundColor = ConsoleColor.White;
        for (int i = 0; i < qrCode.Matrix.Width + 2; i++) Console.Write("　");//中文全角的空格符
        Console.WriteLine();
        for (int j = 0; j < qrCode.Matrix.Height; j++)
        {
            for (int i = 0; i < qrCode.Matrix.Width; i++)
            {
                Console.Write(i == 0 ? "　" : "");//中文全角的空格符
                Console.BackgroundColor = qrCode.Matrix[i, j] ? ConsoleColor.Black : ConsoleColor.White;
                Console.Write('　');//中文全角的空格符
                Console.BackgroundColor = ConsoleColor.White;
                Console.Write(i == qrCode.Matrix.Width - 1 ? "　" : "");//中文全角的空格符
            }
            Console.WriteLine();
        }
        for (int i = 0; i < qrCode.Matrix.Width + 2; i++) Console.Write("　");//中文全角的空格符
        Console.BackgroundColor = ConsoleColor.Black;
        Console.WriteLine();
    }
    public static void ConsoleQrJbMode(string content)
    {
        QrEncoder qrEncoder = new QrEncoder(ErrorCorrectionLevel.M);
        QrCode qrCode = qrEncoder.Encode(content);
        Console.BackgroundColor = ConsoleColor.White;
        for (int i = 0; i < qrCode.Matrix.Width + 2; i++) Console.Write("　");//中文全角的空格符
        Console.WriteLine();
        for (int j = 0; j < qrCode.Matrix.Height; j++)
        {
            for (int i = 0; i < qrCode.Matrix.Width; i++)
            {
                Console.Write(i == 0 ? "　" : "");//中文全角的空格符
                Console.BackgroundColor = qrCode.Matrix[i, j] ? ConsoleColor.DarkGray : ConsoleColor.White;
                Console.Write('　');//中文全角的空格符
                Console.BackgroundColor = ConsoleColor.White;
                Console.Write(i == qrCode.Matrix.Width - 1 ? "　" : "");//中文全角的空格符
            }
            Console.WriteLine();
        }
        for (int i = 0; i < qrCode.Matrix.Width + 2; i++) Console.Write("　");//中文全角的空格符
        Console.BackgroundColor = ConsoleColor.Black;
        Console.WriteLine();
    }
    
    public static byte[] GenRandomMac()
    {
        Random r = new Random();
        Byte[] b = new Byte[5];
        r.NextBytes(b);
        return b;
    }

    public static string[] ResolveArgs(string st)
    {
        return Regex.Matches(st, @"""([^""]*)""|(\S+)")
            .Select(i => i.Value)
            .ToArray();
    }
    
}