using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Cursova
{
    [JsonDerivedType(typeof(Dish), typeDiscriminator: "Dish")]
    [JsonDerivedType(typeof(Drink), typeDiscriminator: "Drink")]
    [JsonDerivedType(typeof(Dessert), typeDiscriminator: "Dessert")]
    public abstract class MenuItemForOrder
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }
        public double WeightGrams { get; set; }
        public List<string> Allergens { get; set; } = new List<string>();

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
