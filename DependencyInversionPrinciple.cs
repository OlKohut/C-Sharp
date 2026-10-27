using System;
using System.Collections.Generic;

namespace Variant4_PatientMedicalRecordService.Task5
{
    public sealed class MedicalRecord
    {
        public string RecordId { get; set; }
        public string PatientId { get; set; }
        public string Diagnosis { get; set; }
        public string DoctorNotes { get; set; }
    }

    // ===== Абстракції (належать високорівневому модулю) =====
    public interface IMedicalRecordFileStorage { void SaveEncryptedRecord(string recordId, string data); }
    public interface IMedicalRecordRepository  { void InsertRecord(MedicalRecord record); }
    public interface IMedicalEventPublisher    { void PublishMedicalEvent(string eventType, string recordId); }

    // ===== Production-реалізації =====
    public sealed class EncryptedLocalFileStorage : IMedicalRecordFileStorage
    {
        public void SaveEncryptedRecord(string recordId, string data) =>
            Console.WriteLine($"[Encrypted File Storage] Картку {recordId} зашифровано AES-256 та збережено у локальне сховище.");
    }

    public sealed class OracleHealthDatabase : IMedicalRecordRepository
    {
        public void InsertRecord(MedicalRecord record) =>
            Console.WriteLine($"[Oracle Health DB] Запис {record.RecordId} для пацієнта {record.PatientId} успішно збережено в таблицю 'PATIENT_RECORDS'.");
    }

    public sealed class HospitalEventBusPublisher : IMedicalEventPublisher
    {
        public void PublishMedicalEvent(string eventType, string recordId) =>
            Console.WriteLine($"[Hospital EventBus] Опубліковано подію '{eventType}' для медичного запису {recordId}.");
    }

    // ===== Mock-реалізації для тестів / демонстрації підміни =====
    public sealed class InMemoryRecordRepository : IMedicalRecordRepository
    {
        public List<MedicalRecord> Records { get; } = new();
        public void InsertRecord(MedicalRecord record) => Records.Add(record);
    }

    public sealed class FakeFileStorage : IMedicalRecordFileStorage
    {
        public Dictionary<string, string> Files { get; } = new();
        public void SaveEncryptedRecord(string recordId, string data) => Files[recordId] = data;
    }

    public sealed class FakeEventPublisher : IMedicalEventPublisher
    {
        public List<(string Type, string RecordId)> Events { get; } = new();
        public void PublishMedicalEvent(string eventType, string recordId) => Events.Add((eventType, recordId));
    }

    // ===== Високорівнева логіка: жодного new для інфраструктури =====
    public sealed class PatientMedicalRecordService
    {
        private readonly IMedicalRecordFileStorage _fileStorage;
        private readonly IMedicalRecordRepository _repository;
        private readonly IMedicalEventPublisher _eventPublisher;

        public PatientMedicalRecordService(
            IMedicalRecordFileStorage fileStorage,
            IMedicalRecordRepository repository,
            IMedicalEventPublisher eventPublisher)
        {
            _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        }

        public void SavePatientRecord(MedicalRecord record)
        {
            if (record is null || string.IsNullOrEmpty(record.PatientId))
                throw new ArgumentException("Некоректний медичний запис пацієнта.");

            _repository.InsertRecord(record);
            _fileStorage.SaveEncryptedRecord(record.RecordId, record.Diagnosis);
            _eventPublisher.PublishMedicalEvent("PATIENT_DIAGNOSIS_UPDATED", record.RecordId);
        }
    }

    // ===== Composition Root: єдине місце, де створюються конкретні залежності =====
    public static class CompositionRoot
    {
        public static PatientMedicalRecordService CreateProduction() =>
            new PatientMedicalRecordService(
                new EncryptedLocalFileStorage(),
                new OracleHealthDatabase(),
                new HospitalEventBusPublisher());

        public static PatientMedicalRecordService CreateForTests(
            InMemoryRecordRepository repository,
            FakeFileStorage fileStorage,
            FakeEventPublisher publisher) =>
            new PatientMedicalRecordService(fileStorage, repository, publisher);
    }

    // ===== Демонстрація підміни реалізацій =====
    public static class DipDemo
    {
        public static void Run()
        {
            var record = new MedicalRecord
            {
                RecordId = "R-001",
                PatientId = "P-100",
                Diagnosis = "Гіпертонія",
                DoctorNotes = "Контроль тиску"
            };

            // 1) Production: Oracle + шифроване сховище + EventBus
            Console.WriteLine("--- Production ---");
            CompositionRoot.CreateProduction().SavePatientRecord(record);

            // 2) Test: ті самі сервіс і логіка, але з in-memory заглушками
            Console.WriteLine("--- Mock ---");
            var repo = new InMemoryRecordRepository();
            var files = new FakeFileStorage();
            var events = new FakeEventPublisher();
            CompositionRoot.CreateForTests(repo, files, events).SavePatientRecord(record);

            Console.WriteLine($"Records: {repo.Records.Count}, Files: {files.Files.Count}, Events: {events.Events.Count}");
            Console.WriteLine($"Event: {events.Events[0].Type} / {events.Events[0].RecordId}");
        }
    }
}