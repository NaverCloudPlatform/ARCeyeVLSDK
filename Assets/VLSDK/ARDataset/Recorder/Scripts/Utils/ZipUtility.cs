using System.IO;
using System.IO.Compression;
using UnityEngine;

namespace ARCeye
{
    public class ZipUtility
    {
        public static void ZipDirectory(string sourceDirectory, string destinationZipFilePath)
        {
            if (Directory.Exists(sourceDirectory))
            {
                // 기존 ZIP 파일이 존재하면 삭제
                if (File.Exists(destinationZipFilePath))
                {
                    File.Delete(destinationZipFilePath);
                }

                // 디렉토리를 압축하여 ZIP 파일 생성
                ZipFile.CreateFromDirectory(sourceDirectory, destinationZipFilePath);
            }
            else
            {
                Debug.LogError($"Source directory '{sourceDirectory}' does not exist.");
            }
        }
    }
}