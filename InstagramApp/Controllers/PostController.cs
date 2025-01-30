using InstagramApp.Data;
using InstagramApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System.Security.Claims;

namespace InstagramApp.Controllers
{
    [Authorize]
    public class PostController : Controller
    {

        private ApplicationDbContext _context;
        private IWebHostEnvironment _hostingEnvironment;
        public PostController(ApplicationDbContext ctx, IWebHostEnvironment hostingEnvironment)
        {
            _context = ctx;
            _hostingEnvironment = hostingEnvironment;
        }

        public IActionResult Index()
        {

            var userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            if (userId == null) return Unauthorized();

            var posts = _context.Posts
                .Where(post => post.UserId == userId)
                .Join(
                    _context.Users,
					post => post.UserId,
					user => user.Id,
					(post, user) => new
					{
						PostId = post.Id,
						Text = post.Text,
						ReleaseDate = post.ReleaseDate,
						ImgPath = post.ImagePath,
						hasUserLiked = _context.Posts
							.Where(p => p.Id == post.Id)
							.SelectMany(p => p.PostLikedByUsers)
							.Any(user => user.Id == userId)
					}
				)
                .OrderByDescending(post => post.ReleaseDate)
                .ToList();

            return View(posts);
        }

        public IActionResult CreatePost()
        {
            return View();
        }

        [HttpPost]
        public IActionResult CreatePostForm(Post newPost)
        {
            var user = User.FindFirst(ClaimTypes.NameIdentifier);

            newPost.UserId = user.Value;
            newPost.ReleaseDate = DateTime.Now;

            // obrázek se uloží do složky images
            if (newPost.ImageFile != null)
            {
                string uploadFolder = Path.Combine(_hostingEnvironment.WebRootPath, "images"); // cesta k složce images
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + newPost.ImageFile.FileName; // unikátní název souboru
                string filePath = Path.Combine(uploadFolder, uniqueFileName); // cesta k souboru
                using (var fileStream = new FileStream(filePath, FileMode.Create)) // uložení obrázku do složky images
                {
                    newPost.ImageFile.CopyTo(fileStream);
                }
                newPost.ImagePath = uniqueFileName;
            }

            _context.Posts.Add(newPost);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        public IActionResult EditPost(int id)
        {
            var post = _context.Posts.FirstOrDefault(p => p.Id == id);
            if (post != null) {
                return View(post);
            }
            return NotFound();
        }

        public IActionResult EditePostForm(Post existingPost)
        {
            // edit příspěvku
            var editedPost = _context.Posts.FirstOrDefault(p => p.Id == existingPost.Id);
            if (editedPost != null)
            {
                if (existingPost.ImageFile != null) // stejné jako při vytváření nového příspěvku
                {
                    string uploadFolder = Path.Combine(_hostingEnvironment.WebRootPath, "images");
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + existingPost.ImageFile.FileName;
                    string filePath = Path.Combine(uploadFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        existingPost.ImageFile.CopyTo(fileStream);
                    }
                    editedPost.ImagePath = uniqueFileName;
                }

                editedPost.Text = existingPost.Text;
                editedPost.ImageFile = existingPost.ImageFile;
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        public IActionResult DeletePost(int id)
        {
            var post = _context.Posts.FirstOrDefault(p => p.Id == id);
            if (post != null)
            {
                _context.Posts.Remove(post);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        public IActionResult LikePost(int Id, string userName)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            var currentUser = _context.Users.FirstOrDefault(u => u.Id == currentUserId);

			var post = _context.Posts
                .Include(p => p.PostLikedByUsers)
                .FirstOrDefault(p => p.Id == Id);

            var hasUserLiked = _context.Posts
                .Where(p => p.Id == post.Id)
                .SelectMany(p => p.PostLikedByUsers)
                .Any(user => user.Id == currentUserId);

            // kontrola liku
            if (hasUserLiked)
            {
                var userToUnlike = post.PostLikedByUsers.FirstOrDefault(user => user.Id == currentUserId);
                post.PostLikedByUsers.Remove(userToUnlike);
            }
            else
            {
                post.PostLikedByUsers.Add(currentUser);
            }
            _context.SaveChanges();

            // v případě, že je userName null, like se přidává buď na hlavní stránce nebo na stránce s příspěvky přihlášeného uživatele
            // v případě, že userName není null, like se přídává na stránce po vyhledání konkrétního uživatele
            // informace o userName slouží pro správné přesměrování (na stránku, kde se like přidal)
            if (userName != null)
            {
                return RedirectToAction("ShowSearchResults", "Home", new { userName = userName });
			}

			// přesměrování na původní stránku
			var refererUrl = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(refererUrl))
            {
                return Redirect(refererUrl);
            }

			// pokud není refererUrl, přesměrování na hlavní stránku
			return RedirectToAction("Index", "Post");
        }

        public IActionResult Comment(int id, string userName)
        {
            ViewData["PostId"] = id;
			var refererUrl = Request.Headers["Referer"].ToString();
            ViewData["prefUrl"] = refererUrl;
            ViewData["userName"] = userName;
            return View();
        }

        public IActionResult CommentPost(int postId, string text, string prevUrl, string userName)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; // získání ID přihlášeného uživatele
            var post = _context.Posts.FirstOrDefault(p => p.Id == postId); // získání příspěvku, ke kterému se bude přidávat komentář

            if (post == null)
            {
                return NotFound("Příspěvek nenalezen.");
            }

            if (string.IsNullOrEmpty(text))
            {
                return BadRequest("Text komentáře nesmí být prázdný.");
            }

            var comment = new Comment
            {
                Text = text,
                UserId = currentUserId,
                PostId = postId
            };

            _context.Comments.Add(comment);
            _context.SaveChanges();

            // přesměrování na původní stránku, stejný princip jako u metody LikePost
            if (userName != null)
            {
                return RedirectToAction("ShowSearchResults", "Home", new { userName = userName });
            }

            if (!string.IsNullOrEmpty(prevUrl))
			{
				return Redirect(prevUrl);
			}

			return RedirectToAction("Index", "Post");
        }
        public IActionResult LikeInfo(int id, string userName)
        {
            var likes = _context.Posts
                .Where(p => p.Id == id)
				.Include(p => p.PostLikedByUsers)
				.SelectMany(p => p.PostLikedByUsers)
                .ToList();

			
			var refererUrl = Request.Headers["Referer"].ToString();
            if (string.IsNullOrEmpty(refererUrl))
            {
                refererUrl = Url.Action("Index", "Home");
            }

			// podobný princip jako u metody LikePost
			if (userName != null)
			{
				refererUrl = Url.Action("ShowSearchResults", "Home", new { userName = userName });
			}

			ViewBag.PreviousUrl = refererUrl;

            return View(likes);
        }

        public IActionResult CommentInfo(int id, string userName)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var comments = _context.Comments
                .Where(c => c.PostId == id)
                .Include(c => c.User)
                .Select(c => new
                {
                    Id = c.Id,
                    Text = c.Text,
                    User = c.User,
                    IsCurrentUserAuthor = c.UserId == currentUserId // přidání informace, zda je uživatel autorem komentáře
				})
                .ToList();

            var refererUrl = Request.Headers["Referer"].ToString();
            if (string.IsNullOrEmpty(refererUrl))
            {
                refererUrl = Url.Action("Index", "Home");
            }

			// opět, princip jako u metody LikePost
			if (userName != null)
			{
				refererUrl = Url.Action("ShowSearchResults", "Home", new { userName = userName });
			}

			ViewBag.PreviousUrl = refererUrl;

            return View(comments);
        }

		public IActionResult DeleteComment(int id, string prevUrl)
		{
			var comment = _context.Comments.FirstOrDefault(c => c.Id == id);
			if (comment != null)
			{
				_context.Comments.Remove(comment);
				_context.SaveChanges();
			}

			// přesměrování na púvodní stránku stránku
			if (!string.IsNullOrEmpty(prevUrl))
			{
				return Redirect(prevUrl);
			}

			return RedirectToAction("Index", "Home");
		}
	}
}
