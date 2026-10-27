using System;
using System.Text;

namespace Variant4_CloudStorageService.Task4
{
    // ===== Role-based інтерфейси (замість одного "жирного" ICloudStorageProvider) =====
    public interface IFileReader          { byte[] DownloadFile(string name); }
    public interface IFileWriter          { void UploadFile(string name, byte[] data); void DeleteFile(string name); }
    public interface IShareLinkProvider   { string GeneratePublicShareLink(string name); }
    public interface IArchiver            { void CompressArchive(string folderPath); }
    public interface INetworkDriveMounter { void MountNetworkDrive(string driveLetter); }

    // ===== Реалізації: кожна реалізує лише те, що справді вміє. Жодних заглушок і NotImplementedException =====

    // Публічний CDN: тільки читання + публічні посилання.
    public sealed class ReadOnlyPublicCdnProvider : IFileReader, IShareLinkProvider
    {
        public byte[] DownloadFile(string name) => Array.Empty<byte>();
        public string GeneratePublicShareLink(string name) => $"https://example.com/share/{name}";
    }

    // Локальна синхронізація: читання, запис, архівування.
    public sealed class LocalFileSyncProvider : IFileReader, IFileWriter, IArchiver
    {
        public byte[] DownloadFile(string name) => Array.Empty<byte>();
        public void UploadFile(string name, byte[] data) => Console.WriteLine($"Uploaded {name} to local disk.");
        public void DeleteFile(string name) => Console.WriteLine($"Deleted {name} from local disk.");
        public void CompressArchive(string folderPath) => Console.WriteLine($"Archived {folderPath}.");
    }

    // Мережевий диск: єдина реалізація, яка вміє монтуватися.
    public sealed class NetworkShareProvider : IFileReader, IFileWriter, INetworkDriveMounter
    {
        public byte[] DownloadFile(string name) => Array.Empty<byte>();
        public void UploadFile(string name, byte[] data) => Console.WriteLine($"Uploaded {name} to network share.");
        public void DeleteFile(string name) => Console.WriteLine($"Deleted {name} from network share.");
        public void MountNetworkDrive(string driveLetter) => Console.WriteLine($"Mounted network share as {driveLetter}:");
    }

    // ===== Клієнти залежать виключно від ролей, які реально викликають =====

    public sealed class BackupService
    {
        private readonly IFileWriter _writer;
        private readonly IArchiver _archiver;

        public BackupService(IFileWriter writer, IArchiver archiver)
        {
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _archiver = archiver ?? throw new ArgumentNullException(nameof(archiver));
        }

        public void Backup(string folder, string archiveName)
        {
            _archiver.CompressArchive(folder);
            _writer.UploadFile(archiveName, Encoding.UTF8.GetBytes(folder));
        }
    }

    public sealed class SharingService
    {
        private readonly IShareLinkProvider _links;

        public SharingService(IShareLinkProvider links) =>
            _links = links ?? throw new ArgumentNullException(nameof(links));

        public string Share(string fileName) => _links.GeneratePublicShareLink(fileName);
    }

    public sealed class DriveMountService
    {
        private readonly INetworkDriveMounter _mounter;

        public DriveMountService(INetworkDriveMounter mounter) =>
            _mounter = mounter ?? throw new ArgumentNullException(nameof(mounter));

        public void Mount(string letter) => _mounter.MountNetworkDrive(letter);
    }

    // Клієнт ролі читання (щоб кожен інтерфейс мав свого споживача)
    public sealed class DownloadService
    {
        private readonly IFileReader _reader;

        public DownloadService(IFileReader reader) =>
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));

        public int Download(string fileName) => _reader.DownloadFile(fileName).Length;
    }

    // ===== Демонстрація: один провайдер може виступати в кількох ролях =====
    public static class IspDemo
    {
        public static void Run()
        {
            var cdn = new ReadOnlyPublicCdnProvider();
            var local = new LocalFileSyncProvider();
            var network = new NetworkShareProvider();

            // CDN годиться для читання та посилань, але компілятор не дозволить
            // передати його в BackupService (немає IFileWriter / IArchiver).
            Console.WriteLine(new SharingService(cdn).Share("report.pdf"));
            Console.WriteLine($"Downloaded {new DownloadService(cdn).Download("report.pdf")} bytes.");

            new BackupService(local, local).Backup("C:/Docs", "docs.zip");
            new DriveMountService(network).Mount("Z");
        }
    }
}