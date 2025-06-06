using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Cursova
{
    [JsonDerivedType(typeof(Dish), typeDiscriminator: "Dish")]
    [JsonDerivedType(typeof(Drink), typeDiscriminator: "Drink")]
    [JsonDerivedType(typeof(Dessert), typeDiscriminator: "Dessert")]
    public abstract class MenuItemForOrder
    {
        private string name;
        private decimal price;
        private string description;
        private double weightGrams;
        private List<string> allergens = new List<string>();

        public string Name { get => name; set => name = value; }
        public decimal Price { get => price; set => price = value; }
        public string Description { get => description; set => description = value; }
        public double WeightGrams { get => weightGrams; set => weightGrams = value; }
        public List<string> Allergens { get => allergens; set => allergens = value; }
        [JsonConstructor]
        public MenuItemForOrder()
        {
            Allergens = new List<string>();
        }

        public MenuItemForOrder(string name, decimal price, string description, double weightGrams, List<string> allergens = null)
        {
            Name = name;
            Price = price;
            Description = description;
            WeightGrams = weightGrams;
            if (allergens != null)
            {
                Allergens = new List<string>(allergens);
            }
        }

        public abstract string GetCategory();

        public override string ToString()
        {
            return $"{Name} - {Price:C}";
        }
    }
}
