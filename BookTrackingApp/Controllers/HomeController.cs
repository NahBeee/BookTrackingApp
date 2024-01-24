// HomeController.cs
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net.Http;
using System.Collections.Generic;
using BookTrackingApp.Models;

namespace BookTrackingApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private List<Book> _userBookList = new List<Book>(); // New list to store user's books

        public HomeController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index(string searchTitle)
        {
            var httpClient = _httpClientFactory.CreateClient("UdacityBooksAPI");

            // Fetch the entire list of books from the API
            var response = await httpClient.GetStringAsync("/books");
            var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(response);

            // Filter books based on the search title
            var filteredBooks = string.IsNullOrWhiteSpace(searchTitle)
                ? apiResponse.Books
                : apiResponse.Books.Where(b => b.Title.Contains(searchTitle, StringComparison.OrdinalIgnoreCase)).ToList();

            return View(filteredBooks);
        }

        [HttpPost]
        public async Task<IActionResult> AddBook(string title)
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
                    TempData["Message"] = "This book is already in your list.";
                    return RedirectToAction("Index");
                }
            }
            else
            {
                // Book was not found in the API, display a friendly message
                TempData["Message"] = "The book was not found in the list.";
                return RedirectToAction("Index");
            }
            // Return to the book list view
            return RedirectToAction("AddedBooks");
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
