using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using FoodOrderingApp.Models;

namespace FoodOrderingApp.Controllers
{
    public class RestaurantsController : Controller
    {
        private FoodOrderingDBEntities db = new FoodOrderingDBEntities();
        private const string CartSessionKey = "Cart"; // Use "Cart" as the key to store your Cart object

        private Cart GetCartFromSession()
        {
            return Session[CartSessionKey] as Cart ?? new Cart();
        }

        private void SaveCartToSession(Cart cart)
        {
            Session[CartSessionKey] = cart;
        }

        // GET: Restaurants
        public ActionResult Index()
        {
            ViewBag.IsAdmin = IsAdminUser(); // Set ViewBag for the view
            var restaurants = db.Restaurants.Include(r => r.User);
            return View(restaurants.ToList());
        }

        // GET: Restaurants/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Restaurant restaurant = db.Restaurants.Find(id);
            if (restaurant == null)
            {
                return HttpNotFound();
            }
            return View(restaurant);
        }

        // GET: Restaurants/Create
        [Authorize] // Only logged-in users can create
        public ActionResult Create()
        {
            if (!IsAdminUser())
            {
                return new HttpUnauthorizedResult();
            }
            ViewBag.OwnerId = new SelectList(db.Users, "UserId", "FullName");
            return View();
        }

        // POST: Restaurants/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize] // Ensure user is logged in
        public ActionResult Create([Bind(Include = "RestaurantId,Name,Location,Description,OwnerId,IsActive")] Restaurant restaurant)
        {
            if (!IsAdminUser())
            {
                return new HttpUnauthorizedResult();
            }
            if (ModelState.IsValid)
            {
                db.Restaurants.Add(restaurant);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.OwnerId = new SelectList(db.Users, "UserId", "FullName", restaurant.OwnerId);
            return View(restaurant);
        }

        // GET: Restaurants/Edit/5
        [Authorize] // Ensure user is logged in
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Restaurant restaurant = db.Restaurants.Find(id);
            if (restaurant == null)
            {
                return HttpNotFound();
            }

            // Check if the current user is an Admin or the owner of the restaurant
            if (!IsAdminUser() && restaurant.OwnerId != GetLoggedInUserId())
            {
                return new HttpUnauthorizedResult(); // Or RedirectToAction("Index");
            }

            ViewBag.OwnerId = new SelectList(db.Users, "UserId", "FullName", restaurant.OwnerId);
            return View(restaurant);
        }

        // POST: Restaurants/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize] // Ensure user is logged in
        public ActionResult Edit([Bind(Include = "RestaurantId,Name,Location,Description,OwnerId,IsActive")] Restaurant restaurant)
        {
            if (ModelState.IsValid)
            {
                if (!IsAdminUser() && restaurant.OwnerId != GetLoggedInUserId())
                {
                    return new HttpUnauthorizedResult(); // Or RedirectToAction("Index");
                }
                db.Entry(restaurant).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.OwnerId = new SelectList(db.Users, "UserId", "FullName", restaurant.OwnerId);
            return View(restaurant);
        }

        // GET: Restaurants/Delete/5
        [Authorize] // Ensure user is logged in
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Restaurant restaurant = db.Restaurants.Find(id);
            if (restaurant == null)
            {
                return HttpNotFound();
            }

            // Check if the current user is an Admin
            if (!IsAdminUser())
            {
                return new HttpUnauthorizedResult(); // Or RedirectToAction("Index");
            }

            return View(restaurant);
        }

        // POST: Restaurants/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize] // Ensure user is logged in
        public ActionResult DeleteConfirmed(int id)
        {
            Restaurant restaurant = db.Restaurants.Find(id);
            if (!IsAdminUser())
            {
                return new HttpUnauthorizedResult(); // Or RedirectToAction("Index");
            }
            db.Restaurants.Remove(restaurant);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        public ActionResult Cart()
        {
            var cart = GetCartFromSession();
            return View(cart.Items.ToList()); // Pass the list of CartItems to your view
        }

        public ActionResult AddToCart(int itemId, int quantity = 1)
        {
            var menuItem = db.MenuItems.Find(itemId);
            if (menuItem != null)
            {
                var cart = GetCartFromSession();
                cart.AddItem(new MenuItem { ItemId = itemId, Name = menuItem.Name, Price = menuItem.Price, RestaurantId = menuItem.RestaurantId }, quantity);
                SaveCartToSession(cart);
            }
            return RedirectToAction("Cart");
        }

        public ActionResult RemoveFromCart(int itemId)
        {
            var cart = GetCartFromSession();
            cart.RemoveItem(itemId);
            SaveCartToSession(cart);
            return RedirectToAction("Cart");
        }
        [HttpPost]
        public ActionResult Checkout(string deliveryAddress, string paymentMethod)
        {
            var cart = GetCartFromSession();
            if (cart != null && cart.Items.Any())
            {
                var groupedCart = cart.Items.GroupBy(item => item.RestaurantId);

                using (var db = new FoodOrderingDBEntities())
                {
                    try
                    {
                        foreach (var restaurantGroup in groupedCart)
                        {
                            int restaurantId = restaurantGroup.Key;
                            decimal totalAmount = restaurantGroup.Sum(item => (item.MenuItem.Price ?? 0) * item.Quantity);

                            var userEmail = User.Identity.Name;
                            var user = db.Users.FirstOrDefault(u => u.Email == userEmail);
                            if (user == null)
                            {
                                // Handle unauthorized user
                                return new HttpStatusCodeResult(HttpStatusCode.Unauthorized);
                            }

                            var order = new Order
                            {
                                UserId = user.UserId,
                                RestaurantId = restaurantId,
                                OrderDate = DateTime.Now,
                                TotalAmount = totalAmount,
                                DeliveryAddress = deliveryAddress,
                                Status = "Pending",
                                PaymentMethod = paymentMethod,
                                PaymentStatus = "Pending",
                                OrderItems = new List<OrderItem>()
                            };

                            db.Orders.Add(order);
                            db.SaveChanges();

                            foreach (var cartItem in restaurantGroup)
                            {
                                var orderItem = new OrderItem
                                {
                                    OrderId = order.OrderId,
                                    ItemId = cartItem.MenuItem.ItemId,
                                    Quantity = cartItem.Quantity,
                                    UnitPrice = cartItem.MenuItem.Price ?? 0
                                };
                                db.OrderItems.Add(orderItem);
                            }
                            db.SaveChanges();

                        }
                        // Clear the cart after creating all orders
                        cart.Clear();
                        SaveCartToSession(cart);

                        // Optionally, redirect to a general order confirmation page
                        return RedirectToAction("OrderConfirmationOverview");
                    }
                    catch (Exception ex)
                    {
                        // Log the error (important for debugging)
                        System.Diagnostics.Debug.WriteLine($"Checkout Error: {ex.Message}");
                        // Optionally, return an error view or a specific error message to the user
                        return View("Error", new HandleErrorInfo(ex, "Restaurants", "Checkout"));
                    }

                }
            }
            else
            {
                return View("Cart", cart?.Items.ToList() ?? new List<CartItem>());

            }
        }
        public ActionResult OrderConfirmationOverview()
        {
            // You might want to display a summary of all the orders placed
            return View();
        }

        public ActionResult OrderFood(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var restaurant = db.Restaurants.Include(r => r.MenuItems).FirstOrDefault(r => r.RestaurantId == id);

            if (restaurant == null)
            {
                return HttpNotFound();
            }

            return View(restaurant); // Pass the restaurant (which includes MenuItems) to the view
        }

        [HttpPost]
        public JsonResult AddToCartAjax(int itemId, string itemName, decimal price, int restaurantId)
        {
            var menuItem = db.MenuItems.Include(m => m.Restaurant).FirstOrDefault(m => m.ItemId == itemId); // Include Restaurant
            if (menuItem != null && menuItem.Name == itemName && menuItem.Price == price && menuItem.RestaurantId == restaurantId)
            {
                var cart = GetCartFromSession();
                cart.AddItem(new MenuItem { ItemId = itemId, Name = itemName, Price = price, RestaurantId = restaurantId, Restaurant = menuItem.Restaurant }, 1); // Pass the loaded Restaurant
                SaveCartToSession(cart);
                return Json(new { success = true, message = $"{itemName} added to cart." });
            }
            else
            {
                return Json(new { success = false, message = "Error adding item to cart." });
            }
        }

        [HttpPost]
        public JsonResult UpdateCartItemQuantity(int itemId, int quantity)
        {
            var cart = GetCartFromSession();
            var itemToUpdate = cart.Items.FirstOrDefault(i => i.MenuItem.ItemId == itemId);

            if (itemToUpdate != null)
            {
                if (quantity > 0)
                {
                    itemToUpdate.Quantity = quantity;
                    SaveCartToSession(cart);

                    // Calculate the new grand total
                    decimal grandTotal = cart.Items.Sum(item => (item.MenuItem.Price ?? 0) * item.Quantity);
                    return Json(new { success = true, grandTotal = grandTotal.ToString("C") });
                }
                else
                {
                    // If quantity is 0 or less, you might want to remove the item
                    cart.RemoveItem(itemId);
                    SaveCartToSession(cart);
                    decimal grandTotal = cart.Items.Sum(item => (item.MenuItem.Price ?? 0) * item.Quantity);
                    return Json(new { success = true, grandTotal = grandTotal.ToString("C") });
                }
            }

            return Json(new { success = false, message = "Item not found in cart." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult PlaceOrder(int restaurantId, List<int> selectedItemIds)
        {
            if (selectedItemIds == null || !selectedItemIds.Any())
            {
                ModelState.AddModelError("", "Please select at least one menu item.");
                return RedirectToAction("OrderFood", new { id = restaurantId });
            }

            var userEmail = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Email == userEmail);

            if (user == null)
                return new HttpStatusCodeResult(HttpStatusCode.Unauthorized);

            var order = new Order
            {
                RestaurantId = restaurantId,
                UserId = user.UserId,
                OrderDate = DateTime.Now,
                DeliveryAddress = "Dummy Address",
                Status = "Pending",
                PaymentMethod = "Cash on Delivery",
                PaymentStatus = "Pending",
                TotalAmount = 0,
                OrderItems = new List<OrderItem>()
            };

            foreach (var itemId in selectedItemIds)
            {
                var menuItem = db.MenuItems.Find(itemId);
                if (menuItem != null)
                {
                    var orderItem = new OrderItem
                    {
                        OrderId = order.OrderId,
                        ItemId = menuItem.ItemId,
                        Quantity = 1, // Assuming default quantity is 1 in this action
                        UnitPrice = menuItem.Price ?? 0 // Handle potential null price
                    };
                    order.TotalAmount += menuItem.Price ?? 0;
                    order.OrderItems.Add(orderItem);
                }
            }

            db.Orders.Add(order);
            db.SaveChanges();

            return RedirectToAction("Index"); // Or perhaps an order confirmation page
        }

        // Helper methods to check user role and ID
        private bool IsAdminUser()
        {
            if (User.Identity.IsAuthenticated)
            {
                var userEmail = User.Identity.Name;
                var user = db.Users.FirstOrDefault(u => u.Email == userEmail);
                return user != null && user.Role == "Admin";
            }
            return false;
        }

        private int GetLoggedInUserId()
        {
            if (User.Identity.IsAuthenticated)
            {
                var userEmail = User.Identity.Name;
                var user = db.Users.FirstOrDefault(u => u.Email == userEmail);
                return user?.UserId ?? 0; // Return 0 or some default if user not found
            }
            return 0;
        }
    }
}
