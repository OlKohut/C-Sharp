using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Variant4_HotelBooking
{

    public class BookingRequest
    {
        public string GuestName { get; set; } = string.Empty;
        public string GuestPhone { get; set; } = string.Empty;
        public string RoomType { get; set; } = string.Empty; 
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public bool IsLoyaltyMember { get; set; }
        public List<string> ExtraServices { get; set; } = new();
    }

    public record BookingConfirmation(string BookingId, int Nights, decimal TotalPrice, string VoucherHtml);

    //  1. Валідація (причина зміни: правила перевірки даних)

    public class BookingValidator
    {
        public void Validate(BookingRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.GuestName) || string.IsNullOrWhiteSpace(request.GuestPhone))
                throw new ArgumentException("Вкажіть ім'я гостя та контактний номер телефону.");

            if (request.CheckInDate < DateTime.Today || request.CheckOutDate <= request.CheckInDate)
                throw new InvalidOperationException("Некоректний період проживання.");
        }
    }

    //  2. Розрахунок вартості (причина зміни: тарифи, сезони, знижки)

    public class BookingPriceCalculator
    {
        public decimal CalculateTotalPrice(BookingRequest request, int nights)
        {
            decimal basePricePerNight = request.RoomType switch
            {
                "Standard" => 1200m,
                "Deluxe" => 2500m,
                "Suite" => 4500m,
                _ => throw new ArgumentException($"Невідома категорія номера: {request.RoomType}")
            };

            if (request.CheckInDate.Month is >= 6 and <= 8)
            {
                basePricePerNight *= 1.3m;
            }

            decimal roomTotal = basePricePerNight * nights;
            decimal extrasTotal = 0m;

            if (request.ExtraServices != null)
            {
                foreach (var service in request.ExtraServices)
                {
                    extrasTotal += service switch
                    {
                        "Breakfast" => 300m * nights,
                        "Spa" => 800m,
                        "Transfer" => 500m,
                        _ => 0m
                    };
                }
            }

            decimal grandTotal = roomTotal + extrasTotal;
            return request.IsLoyaltyMember ? grandTotal * 0.90m : grandTotal;
        }
    }

    //3. Ваучер (причина зміни: формат/дизайн документа) 

    public class HtmlVoucherGenerator
    {
        public string GenerateHtmlVoucher(BookingRequest request, int nights, decimal grandTotal, string bookingId)
        {
            string guest = WebUtility.HtmlEncode(request.GuestName);
            string room = WebUtility.HtmlEncode(request.RoomType);

            string services = request.ExtraServices is { Count: > 0 }
                ? WebUtility.HtmlEncode(string.Join(", ", request.ExtraServices))
                : "Немає";

            return $@"
            <html>
            <body>
                <h2>Ваучер Бронювання №{bookingId}</h2>
                <p><b>Гість:</b> {guest}</p>
                <p><b>Тип номера:</b> {room}</p>
                <p><b>Заїзд:</b> {request.CheckInDate:dd.MM.yyyy} | <b>Виїзд:</b> {request.CheckOutDate:dd.MM.yyyy} ({nights} ночей)</p>
                <p><b>Додаткові послуги:</b> {services}</p>
                <h3>Загальна вартість: {grandTotal:F2} грн</h3>
            </body>
            </html>";
        }
    }

    //  4. Збереження (причина зміни: сховище даних) 

    public class BookingRepository
    {
        private const string LogFilePath = "hotel_bookings.log";

        public void SaveBookingRecord(BookingRequest request, BookingConfirmation confirmation)
        {
            string dbEntry =
                $"{DateTime.Now:yyyy-MM-dd}|{confirmation.BookingId}|{request.GuestName}|{request.RoomType}|{confirmation.Nights}Nights|{confirmation.TotalPrice}\n";
            File.AppendAllText(LogFilePath, dbEntry);
        }
    }

    //  5. Сповіщення (причина зміни: канал/шлюз зв'язку)

    public class GuestNotificationService
    {
        private readonly HttpClient _httpClient;

        public GuestNotificationService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task SendConfirmationAsync(BookingRequest request, BookingConfirmation confirmation)
        {
            try
            {
                string message =
                    $"Шановний(а) {request.GuestName}, ваше бронювання {request.RoomType} на {confirmation.Nights} ночей зафіксовано. " +
                    $"Сума: {confirmation.TotalPrice:F2} грн.";

                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("phone", request.GuestPhone),
                    new KeyValuePair<string, string>("text", message)
                });

                await _httpClient.PostAsync("http://viber-gateway.local/send", content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка відправки меседжу гостю: {ex.Message}");
            }
        }
    }

    // Клас-оркестратор (лише координує кроки) 

    public class HotelBookingService
    {
        private readonly BookingValidator _validator;
        private readonly BookingPriceCalculator _calculator;
        private readonly HtmlVoucherGenerator _voucherGenerator;
        private readonly BookingRepository _repository;
        private readonly GuestNotificationService _notificationService;

        public HotelBookingService(
            BookingValidator validator,
            BookingPriceCalculator calculator,
            HtmlVoucherGenerator voucherGenerator,
            BookingRepository repository,
            GuestNotificationService notificationService)
        {
            _validator = validator;
            _calculator = calculator;
            _voucherGenerator = voucherGenerator;
            _repository = repository;
            _notificationService = notificationService;
        }

        public async Task<BookingConfirmation> CreateBookingAsync(BookingRequest request)
        {
            _validator.Validate(request);

            int nights = (request.CheckOutDate - request.CheckInDate).Days;
            decimal grandTotal = _calculator.CalculateTotalPrice(request, nights);
            string bookingId = Guid.NewGuid().ToString()[..8];

            string voucherHtml = _voucherGenerator.GenerateHtmlVoucher(request, nights, grandTotal, bookingId);
            var confirmation = new BookingConfirmation(bookingId, nights, grandTotal, voucherHtml);

            _repository.SaveBookingRecord(request, confirmation);
            await _notificationService.SendConfirmationAsync(request, confirmation);

            return confirmation;
        }
    }
}