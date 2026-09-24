using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudyPilotApp.Data;
using StudyPilotApp.Models;
using StudyPilotApp.Services;
using StudyPilotApp.ViewModels;

namespace StudyPilotApp.Controllers;

[Authorize(Roles = "Student")]
public sealed class CommunityController : Controller
{
    private const int PageSize = 10;
    private static readonly HashSet<string> AllowedSorts =
        new(StringComparer.OrdinalIgnoreCase) { "newest", "oldest", "popular", "discussed" };

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly ICommunityService _communityService;
    private readonly INotificationService _notificationService;

    public CommunityController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext,
        ICommunityService communityService,
        INotificationService notificationService)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _communityService = communityService;
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        CommunityCategory? category,
        string sort = "newest",
        bool myPostsOnly = false,
        int page = 1)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        search = NormalizeSearch(search);
        category = IsDefined(category) ? category : null;
        sort = AllowedSorts.Contains(sort) ? sort.ToLowerInvariant() : "newest";
        page = Math.Max(1, page);

        var result = await _communityService.GetPostsAsync(
            user.Id, search, category, sort, myPostsOnly, page, PageSize);
        var totalPages = Math.Max(1, (int)Math.Ceiling(result.TotalCount / (double)PageSize));
        if (page > totalPages)
        {
            page = totalPages;
            result = await _communityService.GetPostsAsync(
                user.Id, search, category, sort, myPostsOnly, page, PageSize);
        }

        var facultyIds = await GetFacultyUserIdsAsync(result.Items.Select(post => post.ApplicationUserId));
        var model = new CommunityIndexViewModel
        {
            Posts = result.Items.Select(post => ToCard(post, user.Id, facultyIds)).ToList(),
            Search = search,
            Category = category,
            Sort = sort,
            MyPostsOnly = myPostsOnly,
            Page = page,
            PageSize = PageSize,
            TotalPosts = result.TotalCount
        };
        if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var post = await _communityService.GetPostAsync(id);
        if (post is null) return NotFound();

        var facultyIds = await GetFacultyUserIdsAsync(
            post.Comments.Select(comment => comment.ApplicationUserId).Append(post.ApplicationUserId));
        var model = ToDetails(post, user.Id, facultyIds);
        if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var model = new CommunityPostFormViewModel { Category = CommunityCategory.General };
        if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CommunityPostFormViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        ValidatePostForm(model);
        if (!ModelState.IsValid)
        {
            if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
            return View(model);
        }

        var post = new CommunityPost
        {
            ApplicationUserId = user.Id,
            Title = model.Title.Trim(),
            Content = model.Content.Trim(),
            Category = model.Category!.Value
        };
        await _communityService.AddPostAsync(post);
        await _communityService.SaveChangesAsync();
        TempData["CommunitySuccess"] = "Your community post was published.";
        return RedirectToAction(nameof(Details), new { id = post.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var post = await _communityService.GetPostAsync(id);
        if (post is null || post.ApplicationUserId != user.Id) return NotFound();

        var model = new CommunityPostFormViewModel
        {
            Id = post.Id,
            Title = post.Title,
            Content = post.Content,
            Category = post.Category
        };
        if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CommunityPostFormViewModel model)
    {
        if (model.Id != id) return BadRequest();
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var post = await _communityService.GetPostAsync(id, trackChanges: true);
        if (post is null || post.ApplicationUserId != user.Id) return NotFound();

        ValidatePostForm(model);
        if (!ModelState.IsValid)
        {
            if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
            return View(model);
        }

        post.Title = model.Title.Trim();
        post.Content = model.Content.Trim();
        post.Category = model.Category!.Value;
        post.UpdatedAt = DateTimeOffset.UtcNow;
        await _communityService.SaveChangesAsync();
        TempData["CommunitySuccess"] = "Your post was updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var post = await _communityService.GetPostAsync(id);
        if (post is null || post.ApplicationUserId != user.Id) return NotFound();

        var model = new CommunityDeleteViewModel
        {
            Post = ToCard(post, user.Id, new HashSet<string>(StringComparer.Ordinal))
        };
        if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
        return View(model);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var post = await _communityService.GetPostAsync(id, trackChanges: true);
        if (post is null || post.ApplicationUserId != user.Id) return NotFound();

        _communityService.RemovePost(post);
        await _communityService.SaveChangesAsync();
        TempData["CommunitySuccess"] = "Your post was deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(int postId, string? newComment)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var post = await _communityService.GetPostAsync(postId);
        if (post is null) return NotFound();

        newComment = newComment?.Trim();
        if (string.IsNullOrWhiteSpace(newComment) || newComment.Length < 2 || newComment.Length > 2000)
        {
            TempData["CommunityError"] = "Comment must be between 2 and 2,000 characters.";
            return RedirectToAction(nameof(Details), new { id = postId });
        }

        await _communityService.AddCommentAsync(new CommunityComment
        {
            CommunityPostId = postId,
            ApplicationUserId = user.Id,
            Content = newComment
        });
        await _communityService.SaveChangesAsync();
        if (post.ApplicationUserId != user.Id)
        {
            await _notificationService.CreateAsync(
                post.ApplicationUserId,
                "New comment on your post",
                $"{DisplayName(user)} commented on “{post.Title}”.",
                NotificationType.Community,
                await DetailsUrlForUserAsync(post.ApplicationUserId, postId));
        }
        return RedirectToAction(nameof(Details), new { id = postId });
    }

    [HttpGet]
    public async Task<IActionResult> EditComment(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var comment = await _communityService.GetCommentAsync(id);
        if (comment is null || comment.ApplicationUserId != user.Id) return NotFound();

        var model = new CommunityCommentEditViewModel
        {
            Id = comment.Id,
            PostId = comment.CommunityPostId,
            Content = comment.Content
        };
        if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditComment(int id, CommunityCommentEditViewModel model)
    {
        if (model.Id != id) return BadRequest();
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var comment = await _communityService.GetCommentAsync(id, trackChanges: true);
        if (comment is null || comment.ApplicationUserId != user.Id) return NotFound();

        model.PostId = comment.CommunityPostId;
        if (!ModelState.IsValid)
        {
            if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
            return View(model);
        }

        comment.Content = model.Content.Trim();
        comment.UpdatedAt = DateTimeOffset.UtcNow;
        await _communityService.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = comment.CommunityPostId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteComment(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var comment = await _communityService.GetCommentAsync(id, trackChanges: true);
        if (comment is null || comment.ApplicationUserId != user.Id) return NotFound();

        var postId = comment.CommunityPostId;
        _communityService.RemoveComment(comment);
        await _communityService.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = postId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePostLike(int id, string? returnUrl = null)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var post = await _communityService.GetPostAsync(id);
        if (post is null) return NotFound();

        bool liked;
        try { liked = await _communityService.TogglePostLikeAsync(id, user.Id); }
        catch (KeyNotFoundException) { return NotFound(); }

        if (liked && post.ApplicationUserId != user.Id)
        {
            await _notificationService.CreateAsync(
                post.ApplicationUserId,
                "Someone liked your post",
                $"{DisplayName(user)} liked “{post.Title}”.",
                NotificationType.Community,
                await DetailsUrlForUserAsync(post.ApplicationUserId, id));
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleCommentLike(int id, int postId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        try { await _communityService.ToggleCommentLikeAsync(id, user.Id); }
        catch (KeyNotFoundException) { return NotFound(); }
        return RedirectToAction(nameof(Details), new { id = postId });
    }

    private void ValidatePostForm(CommunityPostFormViewModel model)
    {
        if (!IsDefined(model.Category))
        {
            model.Category = null;
            ModelState.AddModelError(nameof(model.Category), "Please select a valid category.");
        }
    }

    private static CommunityDetailsViewModel ToDetails(
        CommunityPost post,
        string userId,
        IReadOnlySet<string> facultyIds) => new()
    {
        Post = ToCard(post, userId, facultyIds),
        Comments = post.Comments
            .OrderBy(comment => comment.CreatedAt)
            .Select(comment => new CommunityCommentViewModel
            {
                Id = comment.Id,
                Content = comment.Content,
                AuthorName = DisplayName(comment.ApplicationUser),
                AuthorInitials = CreateInitials(DisplayName(comment.ApplicationUser)),
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt,
                LikeCount = comment.Likes.Count,
                IsLikedByCurrentUser = comment.Likes.Any(like => like.ApplicationUserId == userId),
                IsOwnedByCurrentUser = comment.ApplicationUserId == userId,
                IsFacultyAuthor = facultyIds.Contains(comment.ApplicationUserId)
            }).ToList()
    };

    private static CommunityPostCardViewModel ToCard(
        CommunityPost post,
        string userId,
        IReadOnlySet<string> facultyIds)
    {
        var author = DisplayName(post.ApplicationUser);
        return new CommunityPostCardViewModel
        {
            Id = post.Id,
            Title = post.Title,
            Content = post.Content,
            Category = post.Category,
            AuthorName = author,
            AuthorInitials = CreateInitials(author),
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt,
            LikeCount = post.Likes.Count,
            CommentCount = post.Comments.Count,
            IsLikedByCurrentUser = post.Likes.Any(like => like.ApplicationUserId == userId),
            IsOwnedByCurrentUser = post.ApplicationUserId == userId,
            IsFacultyAuthor = facultyIds.Contains(post.ApplicationUserId)
        };
    }

    private async Task<HashSet<string>> GetFacultyUserIdsAsync(IEnumerable<string> userIds)
    {
        var distinctIds = userIds.Distinct(StringComparer.Ordinal).ToList();
        if (distinctIds.Count == 0) return new HashSet<string>(StringComparer.Ordinal);
        var facultyIds = await _dbContext.FacultyProfiles.AsNoTracking()
            .Where(profile => distinctIds.Contains(profile.ApplicationUserId))
            .Select(profile => profile.ApplicationUserId)
            .ToListAsync();
        return facultyIds.ToHashSet(StringComparer.Ordinal);
    }

    private async Task<string> DetailsUrlForUserAsync(string userId, int postId)
    {
        var isFaculty = await _dbContext.FacultyProfiles.AsNoTracking()
            .AnyAsync(profile => profile.ApplicationUserId == userId);
        return isFaculty
            ? $"/FacultyCommunity/Details/{postId}"
            : $"/Community/Details/{postId}";
    }

    private async Task<bool> PopulateShellAsync(StudentShellViewModel model, ApplicationUser user)
    {
        var profile = await _dbContext.StudentProfiles.AsNoTracking()
            .Where(item => item.ApplicationUserId == user.Id)
            .Select(item => new
            {
                item.StudentId, item.Department, item.Semester,
                HasProfileImage = item.ProfileImageData != null,
                item.CreatedAt, item.UpdatedAt
            }).SingleOrDefaultAsync();
        if (profile is null) return false;

        var fullName = DisplayName(user);
        model.FullName = fullName;
        model.Email = user.Email ?? string.Empty;
        model.StudentId = profile.StudentId;
        model.Initials = CreateInitials(fullName);
        model.DepartmentLabel = profile.Department ?? "Department not set";
        model.SemesterLabel = profile.Semester.HasValue ? $"Semester {profile.Semester}" : "Semester not set";
        model.HasProfileImage = profile.HasProfileImage;
        model.ProfileImageVersion = (profile.UpdatedAt ?? profile.CreatedAt).ToUnixTimeSeconds();
        return true;
    }

    private static string DisplayName(ApplicationUser user) =>
        string.IsNullOrWhiteSpace(user.FullName) ? user.Email ?? "Student" : user.FullName;

    private static string CreateInitials(string value)
    {
        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return "ST";
        return parts.Length == 1
            ? parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant()
            : $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }

    private static string? NormalizeSearch(string? value)
    {
        value = value?.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value[..Math.Min(value.Length, 100)];
    }

    private static bool IsDefined<TEnum>(TEnum? value) where TEnum : struct, Enum =>
        value.HasValue && Enum.IsDefined(typeof(TEnum), value.Value);

    private static ObjectResult MissingStudentProfile() =>
        new(new ProblemDetails
        {
            Title = "Student profile unavailable",
            Detail = "A Student profile is not linked to this account. Please contact an administrator.",
            Status = StatusCodes.Status500InternalServerError
        }) { StatusCode = StatusCodes.Status500InternalServerError };
}
