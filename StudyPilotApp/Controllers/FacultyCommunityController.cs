using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudyPilotApp.Data;
using StudyPilotApp.Models;
using StudyPilotApp.Services;
using StudyPilotApp.ViewModels;

namespace StudyPilotApp.Controllers;

[Authorize(Roles = "Faculty")]
public sealed class FacultyCommunityController : Controller
{
    private const int PageSize = 10;
    private static readonly HashSet<string> AllowedSorts =
        new(StringComparer.OrdinalIgnoreCase) { "newest", "oldest", "popular", "discussed" };

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly ICommunityService _communityService;
    private readonly INotificationService _notificationService;

    public FacultyCommunityController(
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
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var context = await GetFacultyContextAsync(cancellationToken);
        if (context is null) return MissingFacultyProfile();

        search = NormalizeSearch(search);
        category = IsDefined(category) ? category : null;
        sort = AllowedSorts.Contains(sort) ? sort.ToLowerInvariant() : "newest";
        page = Math.Max(1, page);

        var result = await _communityService.GetPostsAsync(
            context.Value.User.Id, search, category, sort, myPostsOnly, page, PageSize);
        var totalPages = Math.Max(1, (int)Math.Ceiling(result.TotalCount / (double)PageSize));
        if (page > totalPages)
        {
            page = totalPages;
            result = await _communityService.GetPostsAsync(
                context.Value.User.Id, search, category, sort, myPostsOnly, page, PageSize);
        }

        var facultyIds = await GetFacultyUserIdsAsync(
            result.Items.Select(item => item.ApplicationUserId), cancellationToken);
        var model = new FacultyCommunityIndexViewModel
        {
            Posts = result.Items.Select(item => ToCard(item, context.Value.User.Id, facultyIds)).ToList(),
            Search = search,
            Category = category,
            Sort = sort,
            MyPostsOnly = myPostsOnly,
            Page = page,
            PageSize = PageSize,
            TotalPosts = result.TotalCount
        };
        PopulateShell(model, context.Value.User, context.Value.Profile);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var context = await GetFacultyContextAsync(cancellationToken);
        if (context is null) return MissingFacultyProfile();
        var post = await _communityService.GetPostAsync(id);
        if (post is null) return NotFound();

        var authorIds = post.Comments.Select(item => item.ApplicationUserId)
            .Append(post.ApplicationUserId);
        var facultyIds = await GetFacultyUserIdsAsync(authorIds, cancellationToken);
        var model = ToDetails(post, context.Value.User.Id, facultyIds);
        PopulateShell(model, context.Value.User, context.Value.Profile);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var context = await GetFacultyContextAsync(cancellationToken);
        if (context is null) return MissingFacultyProfile();
        var model = new FacultyCommunityPostFormViewModel { Category = CommunityCategory.Announcements };
        PopulateShell(model, context.Value.User, context.Value.Profile);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        FacultyCommunityPostFormViewModel model,
        CancellationToken cancellationToken)
    {
        var context = await GetFacultyContextAsync(cancellationToken);
        if (context is null) return MissingFacultyProfile();
        ValidatePostForm(model);
        if (!ModelState.IsValid)
        {
            PopulateShell(model, context.Value.User, context.Value.Profile);
            return View(model);
        }

        var post = new CommunityPost
        {
            ApplicationUserId = context.Value.User.Id,
            Title = model.Title.Trim(),
            Content = model.Content.Trim(),
            Category = model.Category!.Value
        };
        await _communityService.AddPostAsync(post);
        await _communityService.SaveChangesAsync();
        TempData["FacultySuccess"] = "Your community post was published.";
        return RedirectToAction(nameof(Details), new { id = post.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var context = await GetFacultyContextAsync(cancellationToken);
        if (context is null) return MissingFacultyProfile();
        var post = await _communityService.GetPostAsync(id);
        if (post is null || post.ApplicationUserId != context.Value.User.Id) return NotFound();

        var model = new FacultyCommunityPostFormViewModel
        {
            Id = post.Id,
            Title = post.Title,
            Content = post.Content,
            Category = post.Category
        };
        PopulateShell(model, context.Value.User, context.Value.Profile);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        FacultyCommunityPostFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (model.Id != id) return BadRequest();
        var context = await GetFacultyContextAsync(cancellationToken);
        if (context is null) return MissingFacultyProfile();
        var post = await _communityService.GetPostAsync(id, trackChanges: true);
        if (post is null || post.ApplicationUserId != context.Value.User.Id) return NotFound();

        ValidatePostForm(model);
        if (!ModelState.IsValid)
        {
            PopulateShell(model, context.Value.User, context.Value.Profile);
            return View(model);
        }

        post.Title = model.Title.Trim();
        post.Content = model.Content.Trim();
        post.Category = model.Category!.Value;
        post.UpdatedAt = DateTimeOffset.UtcNow;
        await _communityService.SaveChangesAsync();
        TempData["FacultySuccess"] = "Your community post was updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var context = await GetFacultyContextAsync(cancellationToken);
        if (context is null) return MissingFacultyProfile();
        var post = await _communityService.GetPostAsync(id);
        if (post is null || post.ApplicationUserId != context.Value.User.Id) return NotFound();

        var facultyIds = new HashSet<string>(StringComparer.Ordinal) { context.Value.User.Id };
        var model = new FacultyCommunityDeleteViewModel
        {
            Post = ToCard(post, context.Value.User.Id, facultyIds)
        };
        PopulateShell(model, context.Value.User, context.Value.Profile);
        return View(model);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var context = await GetFacultyContextAsync(cancellationToken);
        if (context is null) return MissingFacultyProfile();
        var post = await _communityService.GetPostAsync(id, trackChanges: true);
        if (post is null || post.ApplicationUserId != context.Value.User.Id) return NotFound();

        _communityService.RemovePost(post);
        await _communityService.SaveChangesAsync();
        TempData["FacultySuccess"] = "Your community post was deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(
        int postId,
        string? newComment,
        CancellationToken cancellationToken)
    {
        var context = await GetFacultyContextAsync(cancellationToken);
        if (context is null) return MissingFacultyProfile();
        var post = await _communityService.GetPostAsync(postId);
        if (post is null) return NotFound();

        newComment = newComment?.Trim();
        if (string.IsNullOrWhiteSpace(newComment) || newComment.Length is < 2 or > 2000)
        {
            TempData["FacultyError"] = "Comment must be between 2 and 2,000 characters.";
            return RedirectToAction(nameof(Details), new { id = postId });
        }

        await _communityService.AddCommentAsync(new CommunityComment
        {
            CommunityPostId = postId,
            ApplicationUserId = context.Value.User.Id,
            Content = newComment
        });
        await _communityService.SaveChangesAsync();

        if (post.ApplicationUserId != context.Value.User.Id)
        {
            await _notificationService.CreateAsync(
                post.ApplicationUserId,
                "New Faculty reply on your post",
                $"{DisplayName(context.Value.User)} replied to “{post.Title}”.",
                NotificationType.Community,
                await DetailsUrlForUserAsync(post.ApplicationUserId, postId, cancellationToken));
        }
        return RedirectToAction(nameof(Details), new { id = postId });
    }

    [HttpGet]
    public async Task<IActionResult> EditComment(int id, CancellationToken cancellationToken)
    {
        var context = await GetFacultyContextAsync(cancellationToken);
        if (context is null) return MissingFacultyProfile();
        var comment = await _communityService.GetCommentAsync(id);
        if (comment is null || comment.ApplicationUserId != context.Value.User.Id) return NotFound();

        var model = new FacultyCommunityCommentEditViewModel
        {
            Id = comment.Id,
            PostId = comment.CommunityPostId,
            Content = comment.Content
        };
        PopulateShell(model, context.Value.User, context.Value.Profile);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditComment(
        int id,
        FacultyCommunityCommentEditViewModel model,
        CancellationToken cancellationToken)
    {
        if (model.Id != id) return BadRequest();
        var context = await GetFacultyContextAsync(cancellationToken);
        if (context is null) return MissingFacultyProfile();
        var comment = await _communityService.GetCommentAsync(id, trackChanges: true);
        if (comment is null || comment.ApplicationUserId != context.Value.User.Id) return NotFound();

        model.PostId = comment.CommunityPostId;
        if (!ModelState.IsValid)
        {
            PopulateShell(model, context.Value.User, context.Value.Profile);
            return View(model);
        }

        comment.Content = model.Content.Trim();
        comment.UpdatedAt = DateTimeOffset.UtcNow;
        await _communityService.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = comment.CommunityPostId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteComment(int id, CancellationToken cancellationToken)
    {
        var context = await GetFacultyContextAsync(cancellationToken);
        if (context is null) return MissingFacultyProfile();
        var comment = await _communityService.GetCommentAsync(id, trackChanges: true);
        if (comment is null || comment.ApplicationUserId != context.Value.User.Id) return NotFound();

        var postId = comment.CommunityPostId;
        _communityService.RemoveComment(comment);
        await _communityService.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = postId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePostLike(
        int id,
        string? returnUrl = null,
        CancellationToken cancellationToken = default)
    {
        var context = await GetFacultyContextAsync(cancellationToken);
        if (context is null) return MissingFacultyProfile();
        var post = await _communityService.GetPostAsync(id);
        if (post is null) return NotFound();

        bool liked;
        try { liked = await _communityService.TogglePostLikeAsync(id, context.Value.User.Id); }
        catch (KeyNotFoundException) { return NotFound(); }

        if (liked && post.ApplicationUserId != context.Value.User.Id)
        {
            await _notificationService.CreateAsync(
                post.ApplicationUserId,
                "Faculty liked your post",
                $"{DisplayName(context.Value.User)} liked “{post.Title}”.",
                NotificationType.Community,
                await DetailsUrlForUserAsync(post.ApplicationUserId, id, cancellationToken));
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleCommentLike(
        int id,
        int postId,
        CancellationToken cancellationToken)
    {
        var context = await GetFacultyContextAsync(cancellationToken);
        if (context is null) return MissingFacultyProfile();
        try { await _communityService.ToggleCommentLikeAsync(id, context.Value.User.Id); }
        catch (KeyNotFoundException) { return NotFound(); }
        return RedirectToAction(nameof(Details), new { id = postId });
    }

    private void ValidatePostForm(FacultyCommunityPostFormViewModel model)
    {
        if (!IsDefined(model.Category))
        {
            model.Category = null;
            ModelState.AddModelError(nameof(model.Category), "Please select a valid category.");
        }
    }

    private async Task<(ApplicationUser User, FacultyProfile Profile)?> GetFacultyContextAsync(
        CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return null;
        var profile = await _dbContext.FacultyProfiles.AsNoTracking()
            .Include(item => item.Department)
            .SingleOrDefaultAsync(item => item.ApplicationUserId == user.Id, cancellationToken);
        return profile is null ? null : (user, profile);
    }

    private async Task<HashSet<string>> GetFacultyUserIdsAsync(
        IEnumerable<string> userIds,
        CancellationToken cancellationToken)
    {
        var distinctIds = userIds.Distinct(StringComparer.Ordinal).ToList();
        if (distinctIds.Count == 0) return new HashSet<string>(StringComparer.Ordinal);
        var facultyIds = await _dbContext.FacultyProfiles.AsNoTracking()
            .Where(item => distinctIds.Contains(item.ApplicationUserId))
            .Select(item => item.ApplicationUserId)
            .ToListAsync(cancellationToken);
        return facultyIds.ToHashSet(StringComparer.Ordinal);
    }

    private async Task<string> DetailsUrlForUserAsync(
        string userId,
        int postId,
        CancellationToken cancellationToken)
    {
        var isFaculty = await _dbContext.FacultyProfiles.AsNoTracking()
            .AnyAsync(item => item.ApplicationUserId == userId, cancellationToken);
        return isFaculty
            ? $"/FacultyCommunity/Details/{postId}"
            : $"/Community/Details/{postId}";
    }

    private static FacultyCommunityDetailsViewModel ToDetails(
        CommunityPost post,
        string userId,
        IReadOnlySet<string> facultyIds) => new()
    {
        Post = ToCard(post, userId, facultyIds),
        Comments = post.Comments.OrderBy(item => item.CreatedAt).Select(item =>
        {
            var author = DisplayName(item.ApplicationUser);
            return new CommunityCommentViewModel
            {
                Id = item.Id,
                Content = item.Content,
                AuthorName = author,
                AuthorInitials = CreateInitials(author),
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt,
                LikeCount = item.Likes.Count,
                IsLikedByCurrentUser = item.Likes.Any(like => like.ApplicationUserId == userId),
                IsOwnedByCurrentUser = item.ApplicationUserId == userId,
                IsFacultyAuthor = facultyIds.Contains(item.ApplicationUserId)
            };
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

    private static void PopulateShell(
        FacultyShellViewModel model,
        ApplicationUser user,
        FacultyProfile profile)
    {
        var name = DisplayName(user);
        model.FullName = name;
        model.Email = user.Email ?? string.Empty;
        model.FacultyId = profile.FacultyId;
        model.DepartmentLabel = profile.Department?.Name ?? "Department not assigned";
        model.DesignationLabel = profile.Designation ?? "Faculty member";
        model.HasProfileImage = profile.ProfileImageData is not null;
        model.ProfileImageVersion = (profile.UpdatedAt ?? profile.CreatedAt).ToUnixTimeSeconds();
        model.Initials = CreateInitials(name);
    }

    private static string DisplayName(ApplicationUser user) =>
        string.IsNullOrWhiteSpace(user.FullName) ? user.Email ?? "Community member" : user.FullName;

    private static string CreateInitials(string value)
    {
        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return "CM";
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

    private static ObjectResult MissingFacultyProfile() =>
        new(new ProblemDetails
        {
            Title = "Faculty profile unavailable",
            Detail = "A Faculty profile is not linked to this account. Please contact an administrator.",
            Status = StatusCodes.Status500InternalServerError
        }) { StatusCode = StatusCodes.Status500InternalServerError };
}
