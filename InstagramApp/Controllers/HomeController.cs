using InstagramApp.Data;
using InstagramApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Security.Claims;

namespace InstagramApp.Controllers
{
	[Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext ctx)
        {
            _logger = logger;
            _context = ctx;
        }

        public IActionResult Index()
        {
			var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			
			var postsOfFollowedUsers = _context.Users
                .Where(u => u.Id == userId)
                .SelectMany(u => u.Following)
                .SelectMany(f => f.Posts)
                .Join(
                    _context.Users,
                    post => post.UserId,
                    user => user.Id,
                    (post, user) => new
                    {
                        PostId = post.Id,
                        Text = post.Text,
                        ReleaseDate = post.ReleaseDate,
                        AuthorName = user.UserName,
						ImgPath = post.ImagePath,
						hasUserLiked = _context.Posts // info o tom, zdali pøihlášený uživatel olajkoval konkrétní post
							.Where(p => p.Id == post.Id)
                            .SelectMany(p => p.PostLikedByUsers)
                            .Any(user => user.Id == userId)
                    }
                    )
                .OrderByDescending(post => post.ReleaseDate)
                .ToList();

            ViewBag.HomePage = true;

            return View(postsOfFollowedUsers);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        // Vyhledávání uživatele (pohled)
		public IActionResult SearchForUser()
		{
			return View();
		}

        public IActionResult ShowSearchResults(string userName)
        {
			var user = _context.Users.FirstOrDefault(u => u.UserName == userName);
			if (user == null)
			{
				return NotFound("Uživatel nenalezen.");
			}

			var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
			var currentUser = _context.Users.Include(u => u.Following).FirstOrDefault(u => u.Id == currentUserId);

			var userId = user.Id;

			var posts = _context.Posts
				.Where(post => post.UserId == userId)
				.OrderByDescending(post => post.ReleaseDate)
				.Select(post => new
				{
					PostId = post.Id,
					Text = post.Text,
					ReleaseDate = post.ReleaseDate,
					ImgPath = post.ImagePath,
					hasUserLiked = _context.Posts
							.Where(p => p.Id == post.Id)
							.SelectMany(p => p.PostLikedByUsers)
							.Any(user => user.Id == currentUserId)
				})
				.ToList();

			ViewBag.UserName = userName;
			ViewBag.FirstName = user.Firstname;
			ViewBag.LastName = user.Lastname;

			ViewBag.IsFollowing = currentUser != null && currentUser.Following.Any(f => f.Id == userId);
			ViewBag.IsUserFindingHimself = currentUserId == userId; // jestli uživatel hledá sám sebe
			return View(posts);
		}

        public IActionResult FollowUser(string userName)
        {
            // uživatel, který bude dávat follow
            var userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            var userWillFollow = _context.Users.FirstOrDefault(u => u.Id == userId);
			if (userWillFollow == null) return Unauthorized();

			// uživatel, který dostane follow
			var userGetFollow = _context.Users.FirstOrDefault(u => u.UserName == userName);

            if (userGetFollow == null)
            {
                return NotFound("Uživatel nenalezen.");
            }

            userWillFollow.Following.Add(userGetFollow);
			_context.SaveChanges();

			return RedirectToAction("ShowSearchResults", new { userName = userName });
		}

		public IActionResult UnfollowUser(string userName)
		{
			// uživatel, který bude odebírat follow
			var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
			var currentUser = _context.Users.Include(u => u.Following)
											 .FirstOrDefault(u => u.Id == currentUserId);
			if (currentUser == null)
			{
				return Unauthorized();
			}

			// uživatel, který pøíjde o follow
			var userToUnfollow = _context.Users.FirstOrDefault(u => u.UserName == userName);
			if (userToUnfollow == null)
			{
				return NotFound("Uživatel nenalezen.");
			}

			currentUser.Following.Remove(userToUnfollow);
			_context.SaveChanges();

			return RedirectToAction("ShowSearchResults", new { userName = userName });
		}

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
