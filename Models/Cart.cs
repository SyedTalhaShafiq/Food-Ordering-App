using System;
using System.Collections.Generic;
using System.Linq;

namespace FoodOrderingApp.Models
{
    public class Cart
    {
        public List<CartItem> Items { get; set; } = new List<CartItem>();

        // In Cart.cs
        public void AddItem(MenuItem menuItem, int quantity)
        {
            var existingItem = Items.FirstOrDefault(i => i.MenuItem.ItemId == menuItem.ItemId);
            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                Items.Add(new CartItem { MenuItem = menuItem, Quantity = quantity, RestaurantId = menuItem.RestaurantId ?? 0 });
            }
        }

        public void RemoveItem(int itemId)
        {
            var item = Items.FirstOrDefault(i => i.MenuItem.ItemId == itemId);
            if (item != null)
            {
                Items.Remove(item);
            }
        }

        public decimal GetTotal()
        {
            return Items.Sum(i => (i.MenuItem.Price ?? 0) * i.Quantity);
        }

        public void Clear()
        {
            Items.Clear();
        }
    }

    public class CartItem
    {
        public MenuItem MenuItem { get; set; }
        public int Quantity { get; set; }

        public int RestaurantId { get; set; }

        public decimal TotalPrice
        {
            get
            {
                return (MenuItem.Price ?? 0) * Quantity;
            }
        }
    }
}
