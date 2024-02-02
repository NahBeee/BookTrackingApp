// HomeController.cs
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net.Http;
using System.Collections.Generic;
using BookTrackingApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace BookTrackingApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private List<Book> _userBookList = new List<Book>(); // New list to store user's books
        private static readonly List<User> Users = new List<User>
        {
            new User { Username = "user1", Password = "password1" },
            new User { Username = "user2", Password = "password2" }
        };
        public IActionResult Login(string username, string password)
        {
            var user = Users.FirstOrDefault(u => u.Username == username && u.Password == password);

            if (user != null)
            {
                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username),
        };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
                {
                    IsPersistent = true, // Ensure the cookie is persisted
                    ExpiresUtc = DateTimeOffset.UtcNow.Add(TimeSpan.FromDays(30)) // Set the expiration time
                });

                return RedirectToAction("Index");
            }
            else
            {
                TempData["Message2"] = "Invalid username or password";
                return RedirectToAction("LoginView");
            }
        }

        [AllowAnonymous]
        public IActionResult LoginView()
        {
            // Check if the user is already authenticated
            if (User?.Identity?.IsAuthenticated ?? false)
            {
                // Redirect to the Index action if authenticated
                return RedirectToAction("Index");
            }

            return View();
        }
        public IActionResult Logout()
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index");
        }
        public HomeController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }
        [AllowAnonymous]
        public async Task<IActionResult> Index(string searchTitle)
        {
            if (User?.Identity?.IsAuthenticated ?? false)
            {
                var httpClient = _httpClientFactory.CreateClient("UdacityBooksAPI");

                // Fetch the entire list of books from the API
                var response = await httpClient.GetStringAsync("/books");
                var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(response);

                // Filter books based on the search title
                var filteredBooks = string.IsNullOrWhiteSpace(searchTitle)
                    ? apiResponse.Books
                    : apiResponse.Books.Where(b => b.Title.Contains(searchTitle, StringComparison.OrdinalIgnoreCase)).ToList();

                return View(new List<Book>());
            }
            else
            {
                return RedirectToAction("LoginView");
            }
            

        }

        [HttpPost]
        public async Task<IActionResult> AddBook(string title)
        {
            if (User?.Identity?.IsAuthenticated ?? false)
            {
                var httpClient = _httpClientFactory.CreateClient("UdacityBooksAPI");

                // Fetch the entire list of books from the API
                var response = await httpClient.GetStringAsync("/books");
                var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(response);

                // Find the book by title in the API response
                var bookToAdd = apiResponse.Books.FirstOrDefault(b => b.Title == title);

                if (bookToAdd != null)
                {
                    // Get or create the user's book list from session
                    var userBookListJson = HttpContext.Session.GetString("UserBookList");
                    var userBookList = string.IsNullOrEmpty(userBookListJson)
                        ? new List<Book>()
                        : JsonConvert.DeserializeObject<List<Book>>(userBookListJson);

                    // Check if the book is already in the user's list
                    if (!userBookList.Any(b => b.Id == bookToAdd.Id))
                    {
                        // Add the book to the user's book list
                        userBookList.Add(bookToAdd);

                        // Save the updated book list to session
                        HttpContext.Session.SetString("UserBookList", JsonConvert.SerializeObject(userBookList));

                        // Redirect to the AddedBooks action
                        return RedirectToAction("AddedBooks");
                    }
                    else
                    {
                        // Book is already in the user's list, display a friendly message
                        TempData["Message1"] = "This book is already in your list.";
                        return RedirectToAction("Index");
                    }
                }
                else
                {
                    // Book was not found in the API, display a friendly message
                    TempData["Message1"] = "The book was not found in the list.";
                    return RedirectToAction("Index");
                }
            }
            else
            {
                // User is not authenticated, redirect to login view
                return RedirectToAction("LoginView");
            }
        }

        public IActionResult AddedBooks()
        {
            // Get the user's book list from session
            var userBookListJson = HttpContext.Session.GetString("UserBookList");
            var userBookList = string.IsNullOrEmpty(userBookListJson)
                ? new List<Book>()
                : JsonConvert.DeserializeObject<List<Book>>(userBookListJson);

            // Return the user's book list to the view
            return View(userBookList);
        }
        [HttpPost]
        public IActionResult RemoveBook(string id)
        {
            // Get or create the user's book list from session
            var userBookListJson = HttpContext.Session.GetString("UserBookList");
            var userBookList = string.IsNullOrEmpty(userBookListJson)
                ? new List<Book>()
                : JsonConvert.DeserializeObject<List<Book>>(userBookListJson);

            // Find the book to remove by its ID
            var bookToRemove = userBookList.FirstOrDefault(b => b.Id == id);

            if (bookToRemove != null)
            {
                // Remove the book from the user's book list
                userBookList.Remove(bookToRemove);

                // Save the updated book list to session
                HttpContext.Session.SetString("UserBookList", JsonConvert.SerializeObject(userBookList));
            }

            // Redirect back to the AddedBooks action
            return RedirectToAction("AddedBooks");
        }
        

    }
}
