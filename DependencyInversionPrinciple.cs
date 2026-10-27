using System;

namespace Variant4_PatientMedicalRecordService
{
    public class MedicalRecord
    {
        public string RecordId { get; set; } = string.Empty;
        public string PatientId { get; set; } = string.Empty;
        public string Diagnosis { get; set; } = string.Empty;
        public string DoctorNotes { get; set; } = string.Empty;
    }

    // 1. Виділення абстракцій
    public interface IEncryptedStorage
    {
        void SaveEncryptedRecord(string recordId, string data);
    }

    public interface IHealthDatabase
    {
        void InsertRecord(MedicalRecord record);
    }

    public interface IEventPublisher
    {
        void PublishMedicalEvent(string eventType, string recordId);
    }

    // 2. Реалізації
    public class EncryptedLocalFileStorage : IEncryptedStorage
    {
        public void SaveEncryptedRecord(string recordId, string data)
        {
            Console.WriteLine($"[Encrypted File Storage] Картку {recordId} зашифровано AES-256 та збережено.");
        }
    }

    public class OracleHealthDatabase : IHealthDatabase
    {
        public void InsertRecord(MedicalRecord record)
        {
            Console.WriteLine($"[Oracle Health DB] Запис {record.RecordId} для пацієнта {record.PatientId} збережено.");
        }
    }

    public class HospitalEventBusPublisher : IEventPublisher
    {
        public void PublishMedicalEvent(string eventType, string recordId)
        {
            Console.WriteLine($"[Hospital EventBus] Опубліковано подію '{eventType}' для {recordId}.");
        }
    }

    // Mock для тестування (демонстрація підміни реалізацій)
    public class MockHealthDatabase : IHealthDatabase
    {
        public void InsertRecord(MedicalRecord record)
        {
            Console.WriteLine($"[Mock DB] Запис {record.RecordId} збережено у пам'ять для Unit-тесту.");
        }
    }

    // 3. Високорівневий клас залежить ВІКЛЮЧНО від абстракцій (Constructor Injection)
    public class PatientMedicalRecordService
    {
        private readonly IEncryptedStorage _fileStorage;
        private readonly IHealthDatabase _database;
        private readonly IEventPublisher _eventPublisher;

        public PatientMedicalRecordService(
            IEncryptedStorage fileStorage, 
            IHealthDatabase database, 
            IEventPublisher eventPublisher)
        {
            _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        }

        public void SavePatientRecord(MedicalRecord record)
        {
            if (record == null || string.IsNullOrEmpty(record.PatientId))
            {
                throw new ArgumentException("Некоректний медичний запис пацієнта.");
            }

            _database.InsertRecord(record);
            _fileStorage.SaveEncryptedRecord(record.RecordId, record.Diagnosis);
            _eventPublisher.PublishMedicalEvent("PATIENT_DIAGNOSIS_UPDATED", record.RecordId);
        }
    }

    // 4. Composition Root (Точка збірки програми)
    public static class Program
    {
        public static void Main()
        {
            // Налаштування залежностей вручну (Manual DI)
            IEncryptedStorage storage = new EncryptedLocalFileStorage();
            IHealthDatabase database = new OracleHealthDatabase(); // або new MockHealthDatabase()
            IEventPublisher publisher = new HospitalEventBusPublisher();

            var service = new PatientMedicalRecordService(storage, database, publisher);

            var record = new MedicalRecord
            {
                RecordId = "REC-1001",
                PatientId = "PAT-555",
                Diagnosis = "Грип A",
                DoctorNotes = "Призначено спокій та рясне питво."
            };

            service.SavePatientRecord(record);
        }
    }
}