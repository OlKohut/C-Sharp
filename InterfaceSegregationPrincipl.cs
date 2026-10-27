using System;

namespace Variant4_CloudStorageService
{
    // Вузькоспеціалізовані інтерфейси (Role-based Interfaces)
    public interface IFileReader
    {
        byte[] DownloadFile(string name);
    }

    public interface IFileWriter
    {
        void UploadFile(string name, byte[] data);
        void DeleteFile(string name);
    }

    public interface IPublicLinkGenerator
    {
        string GeneratePublicShareLink(string name);
    }

    public interface IArchiveCompressor
    {
        void CompressArchive(string folderPath);
    }

    public interface INetworkDriveMounter
    {
        void MountNetworkDrive(string driveLetter);
    }

    // CDN реалізує лише те, що підтримує
    public class ReadOnlyPublicCDNProvider : IFileReader, IPublicLinkGenerator
    {
        public byte[] DownloadFile(string name) => Array.Empty<byte>();
        public string GeneratePublicShareLink(string name) => $"https://example.com/share/{name}";
    }

    // Локальна синхронізація реалізує лише свої функції
    public class LocalFileSyncProvider : IFileReader, IFileWriter, IArchiveCompressor
    {
        public void UploadFile(string name, byte[] data) => Console.WriteLine($"Uploaded {name} to local disk.");
        public byte[] DownloadFile(string name) => Array.Empty<byte>();
        public void DeleteFile(string name) => Console.WriteLine($"Deleted {name} from local disk.");
        public void CompressArchive(string folderPath) => Console.WriteLine($"Archived {folderPath}.");
    }
}