using System;
using System.IO;
using System.Linq;

namespace AngelDB {

public static class byteTools
{

    public static byte[] Combine(byte[] first, byte[] second)
    {
        byte[] ret = new byte[first.Length + second.Length];
        Buffer.BlockCopy(first, 0, ret, 0, first.Length);
        Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
        return ret;
    }

    public static byte[][] BufferSplit(byte[] buffer, int blockSize)
    {
        byte[][] blocks = new byte[(buffer.Length + blockSize - 1) / blockSize][];

        for (int i = 0, j = 0; i < blocks.Length; i++, j += blockSize)
        {
            blocks[i] = new byte[Math.Min(blockSize, buffer.Length - j)];
            Array.Copy(buffer, j, blocks[i], 0, blocks[i].Length);
        }

        return blocks;
    }

    /// <summary>
    /// Counts how many chunks of a certain size a binary file has.
    /// </summary>
    /// <param name="srcfilename"></param>
    /// <param name="bufferSize"></param>
    /// <returns></returns>
    public static long numberOfChunksInFile(string srcfilename, int bufferSize)

    {
        if (System.IO.File.Exists(srcfilename) == false)
        {
            return 0;
        }

        System.IO.Stream s1 = System.IO.File.Open(srcfilename, System.IO.FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        System.IO.BinaryReader f1 = new System.IO.BinaryReader(s1);

        long n = 0;

        while (true)

        {

            byte[] buf = new byte[bufferSize];
            int sz = f1.Read(buf, 0, bufferSize);
            ++n;

            if (sz <= 0)
                break;
            if (sz < bufferSize)
                break; // eof reached

        }

        f1.Close();
        return n;

    }


    public static string FileSplit(string srcfilename, int bufferSize, int numberOfChunks)

    {
        if (System.IO.File.Exists(srcfilename) == false)
        {
            return "Error: File not found";
        }

        string os_directory_separator = "/";

        DirectoryInfo d = new DirectoryInfo(Path.GetDirectoryName(srcfilename));
        var Files = d.GetFiles("Chunk" + Path.GetFileName(srcfilename) + "*.Chunk").OrderBy(f => f.Name).ToList(); ;

        foreach (FileInfo file in Files)
        {
            File.Delete(file.FullName);
        }

        FileInfo f = new FileInfo(srcfilename);
        long bytesTotal = f.Length;

        System.IO.Stream s1 = System.IO.File.Open(srcfilename, System.IO.FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        System.IO.BinaryReader f1 = new System.IO.BinaryReader(s1);

        long n = 0;
        long sizeOfChunk = (bytesTotal / bufferSize) / numberOfChunks;
        int chunkNumber = 0;
        byte[] newbytes = new byte[] { };
        string chunkName;

        for (int i = 1; i <= (numberOfChunks + 1); ++i)
        {
            chunkName = Path.GetDirectoryName(srcfilename) + os_directory_separator + "Chunk" + Path.GetFileName(srcfilename) + "-" + i.ToString().PadLeft(5, '0') + ".Chunk";

            if (File.Exists(chunkName))
            {
                File.Delete(chunkName);
            }
        }

        while (true)
        {
            byte[] buf = new byte[bufferSize];
            int sz = f1.Read(buf, 0, bufferSize);

            if (sz <= 0)
                break;

            if (sz < bufferSize)
            {
                ++chunkNumber;
                chunkName = Path.GetDirectoryName(srcfilename) + os_directory_separator + "Chunk" + Path.GetFileName(srcfilename) + "-" + chunkNumber.ToString().PadLeft(5, '0') + ".Chunk";
                AppendAllBytes(chunkName, buf.Take(sz).ToArray());
                break; // eof reached
            }
            else
            {
              ++n;
              chunkName = Path.GetDirectoryName(srcfilename) + os_directory_separator + "Chunk" + Path.GetFileName(srcfilename) + "-" + chunkNumber.ToString().PadLeft(5, '0') + ".Chunk";
              AppendAllBytes(chunkName, buf);
              if (n == sizeOfChunk) {
                   ++chunkNumber; 
                   n = 0;
              }
            }
        }

        f1.Close();
        return "Ok.";
    }

    public static string JoinChunks(string fileName)
    {
        DirectoryInfo d = new DirectoryInfo(Path.GetDirectoryName(fileName));
        var Files = d.GetFiles("Chunk" + Path.GetFileName(fileName) + "*.Chunk").OrderBy(f => f.Name).ToList(); ;

        string os_directory_separator = "/";

        string tempFile = Path.GetDirectoryName(fileName) + os_directory_separator + byteTools.RandomString(15, true) + ".temp";

        foreach (FileInfo file in Files)
        {
            string result = JoinTowFiles(tempFile, file.FullName);
            if (result != "Ok.") return result;

            File.Delete(file.FullName);

        }

        if (File.Exists(fileName))
        {
            File.Delete(fileName);
        }

        File.Move(tempFile, fileName);

        return "Ok.";

    }

    public static string JoinTowFiles(string firstFile, string secondFile)
    {

        if (System.IO.File.Exists(secondFile) == false)
        {
            return "Error: Second file does not exits";
        }

        System.IO.Stream s1 = System.IO.File.Open(secondFile, System.IO.FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        System.IO.BinaryReader f1 = new System.IO.BinaryReader(s1);
        int bufferSize = 80000;

        while (true)

        {
            byte[] buf = new byte[bufferSize];
            int sz = f1.Read(buf, 0, bufferSize);

            if (sz <= 0)
                break;

            if (sz < bufferSize)
            {
                AppendAllBytes(firstFile, buf.Take(sz).ToArray());
                break; // eof reached
            }
            else
            {
                AppendAllBytes(firstFile, buf);
            }

        }

        f1.Close();
        return "Ok.";

    }




    /// <summary>
    /// Returns a random string 
    /// </summary>
    /// <param name="size"></param>
    /// <param name="lowerCase"></param>
    /// <returns></returns>
    public static string RandomString(int size, bool lowerCase = false)
    {
        var builder = new System.Text.StringBuilder(size);
        Random _random = new Random();
        char offset = lowerCase ? 'a' : 'A';
        const int lettersOffset = 26;

        for (var i = 0; i < size; i++)
        {
            var @char = (char)_random.Next(offset, offset + lettersOffset);
            builder.Append(@char);
        }

        return lowerCase ? builder.ToString().ToLower() : builder.ToString();
    }

    /// <summary>
    /// Add bits to a file
    /// </summary>
    /// <param name="path">The file will be created if it doesn't exist; and add the desired bits.</param>
    /// <param name="bytes"></param>
    public static void AppendAllBytes(string path, byte[] bytes)
    {
        //argument-checking here.

        var stream = new FileStream(path, FileMode.Append);
        stream.Write(bytes, 0, bytes.Length);
        stream.Close();

    }


   }
}
