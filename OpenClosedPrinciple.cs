using System;
using System.Collections.Generic;
using System.Linq;

namespace Variant4_TaxCalculator
{
    public enum ProductCategory
    {
        Electronics,
        Food,
        LuxuryGoods,
        Pharmaceuticals,
        Alcohol 
    }

    public class ImportGoods
    {
        public string GoodsCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal DeclaredValue { get; set; }
        public decimal WeightKg { get; set; }
    }

    public record TaxResult(decimal ImportDuty, decimal ExciseTax, decimal VatAmount, decimal TotalTax);


    public interface ITaxCalculationStrategy
    {
        ProductCategory Category { get; }
        TaxResult Calculate(ImportGoods goods);
    }

    public interface ITaxLogger
    {
        void Log(string message);
    }

    public class ConsoleTaxLogger : ITaxLogger
    {
        public void Log(string message) => Console.WriteLine(message);
    }


    public abstract class TaxStrategyBase : ITaxCalculationStrategy
    {
        public abstract ProductCategory Category { get; }

        protected abstract decimal CalculateDuty(ImportGoods goods);
        protected virtual decimal CalculateExcise(ImportGoods goods) => 0m;
        protected abstract decimal VatRate { get; }

        public TaxResult Calculate(ImportGoods goods)
        {
            decimal duty = CalculateDuty(goods);
            decimal excise = CalculateExcise(goods);
            decimal vat = (goods.DeclaredValue + duty + excise) * VatRate;
            return new TaxResult(duty, excise, vat, duty + excise + vat);
        }
    }

    public class ElectronicsTaxStrategy : TaxStrategyBase
    {
        public override ProductCategory Category => ProductCategory.Electronics;
        protected override decimal VatRate => 0.20m;
        protected override decimal CalculateDuty(ImportGoods goods) => goods.DeclaredValue * 0.05m;
    }

    public class FoodTaxStrategy : TaxStrategyBase
    {
        public override ProductCategory Category => ProductCategory.Food;
        protected override decimal VatRate => 0.07m; 
        protected override decimal CalculateDuty(ImportGoods goods) =>
            goods.DeclaredValue * 0.10m + goods.WeightKg * 0.50m; 
    }

    public class LuxuryGoodsTaxStrategy : TaxStrategyBase
    {
        public override ProductCategory Category => ProductCategory.LuxuryGoods;
        protected override decimal VatRate => 0.20m;
        protected override decimal CalculateDuty(ImportGoods goods) => goods.DeclaredValue * 0.25m;
        protected override decimal CalculateExcise(ImportGoods goods) => goods.DeclaredValue * 0.15m;
    }

    public class PharmaceuticalsTaxStrategy : TaxStrategyBase
    {
        public override ProductCategory Category => ProductCategory.Pharmaceuticals;
        protected override decimal VatRate => 0.07m; 
        protected override decimal CalculateDuty(ImportGoods goods) => 0m;
    }

    public class AlcoholTaxStrategy : TaxStrategyBase
    {
        public override ProductCategory Category => ProductCategory.Alcohol;
        protected override decimal VatRate => 0.20m;
        protected override decimal CalculateDuty(ImportGoods goods) => goods.DeclaredValue * 0.20m;
        protected override decimal CalculateExcise(ImportGoods goods) => goods.WeightKg * 2.00m; 
    }

    public class TaxCalculator
    {
        private readonly Dictionary<ProductCategory, ITaxCalculationStrategy> _strategies;
        private readonly ITaxLogger _logger;

        public TaxCalculator(IEnumerable<ITaxCalculationStrategy> strategies, ITaxLogger logger)
        {
            ArgumentNullException.ThrowIfNull(strategies);
            ArgumentNullException.ThrowIfNull(logger);

            _strategies = strategies.ToDictionary(s => s.Category);
            _logger = logger;
        }

        public decimal CalculateTotalTax(ImportGoods goods, ProductCategory category)
        {
            ArgumentNullException.ThrowIfNull(goods);

            if (!_strategies.TryGetValue(category, out var strategy))
            {
                throw new ArgumentOutOfRangeException(nameof(category), $"Категорія {category} відсутня у митному реєстрі.");
            }

            var result = strategy.Calculate(goods);
            _logger.Log($"[TAX] Товар '{goods.Name}' ({category}): Мито=${result.ImportDuty}, Акциз=${result.ExciseTax}, ПДВ=${result.VatAmount} | Всього до сплати: ${result.TotalTax}");
            return result.TotalTax;
        }
    }

    public static class TaxCalculatorFactory
    {
        public static TaxCalculator Create(ITaxLogger? logger = null) =>
            new(new ITaxCalculationStrategy[]
                {
                    new ElectronicsTaxStrategy(),
                    new FoodTaxStrategy(),
                    new LuxuryGoodsTaxStrategy(),
                    new PharmaceuticalsTaxStrategy(),
                    new AlcoholTaxStrategy() 
                },
                logger ?? new ConsoleTaxLogger());
    }

    public static class OcpDemo
    {
        public static void Run()
        {
            var calculator = TaxCalculatorFactory.Create();

            var wine = new ImportGoods { GoodsCode = "2204", Name = "Wine", DeclaredValue = 1000m, WeightKg = 10m };
            calculator.CalculateTotalTax(wine, ProductCategory.Alcohol);

            var phone = new ImportGoods { GoodsCode = "8517", Name = "Phone", DeclaredValue = 500m, WeightKg = 0.2m };
            calculator.CalculateTotalTax(phone, ProductCategory.Electronics);
        }
    }
}