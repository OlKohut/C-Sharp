using System;

namespace Variant4_ECommerceStorage
{
    public abstract class InventoryItem
    {
        public string Sku { get; }
        public string Title { get; }
        public decimal UnitPrice { get; }

        protected InventoryItem(string sku, string title, decimal unitPrice)
        {
            if (string.IsNullOrWhiteSpace(sku)) throw new ArgumentException("SKU cannot be empty", nameof(sku));
            if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title cannot be empty", nameof(title));
            if (unitPrice < 0) throw new ArgumentOutOfRangeException(nameof(unitPrice), "Price cannot be negative");

            Sku = sku;
            Title = title;
            UnitPrice = unitPrice;
        }
    }

    public interface IRestockable
    {
        int StockQuantity { get; }
        void Restock(int amount);
    }

    public interface IReturnableToSupplier
    {
        int StockQuantity { get; }
        void ReturnToSupplier(int amount);
    }

    public class PhysicalInventoryItem : InventoryItem, IRestockable, IReturnableToSupplier
    {
        public int StockQuantity { get; private set; }

        public PhysicalInventoryItem(string sku, string title, int stockQuantity, decimal unitPrice)
            : base(sku, title, unitPrice)
        {
            if (stockQuantity < 0)
                throw new ArgumentOutOfRangeException(nameof(stockQuantity), "Initial stock cannot be negative");
            StockQuantity = stockQuantity;
        }

        public void Restock(int amount)
        {

            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Restock amount must be positive");

            StockQuantity += amount;
            Console.WriteLine($"Restocked {amount} units of {Title}. Total stock: {StockQuantity}");
        }

        public void ReturnToSupplier(int amount)
        {

            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Return amount must be positive");

            if (amount > StockQuantity)
                throw new InvalidOperationException($"Cannot return {amount} units. Available stock: {StockQuantity}");

            StockQuantity -= amount;
            Console.WriteLine($"Returned {amount} units of {Title} to supplier.");
        }
    }

    public class DigitalLicenseItem : InventoryItem
    {
        public string LicenseKeyPattern { get; }

        public DigitalLicenseItem(string sku, string title, decimal unitPrice, string licenseKeyPattern)
            : base(sku, title, unitPrice)
        {
            if (string.IsNullOrWhiteSpace(licenseKeyPattern))
                throw new ArgumentException("License key pattern cannot be empty", nameof(licenseKeyPattern));

            LicenseKeyPattern = licenseKeyPattern;
        }
    }

    public class WarehouseManager
    {
        public void ProcessDefectiveReturn(IReturnableToSupplier item, int quantity)
        {
            ArgumentNullException.ThrowIfNull(item);
            item.ReturnToSupplier(quantity);
        }

        public void ProcessSupplierDelivery(IRestockable item, int quantity)
        {
            ArgumentNullException.ThrowIfNull(item);
            item.Restock(quantity);
        }
    }
}