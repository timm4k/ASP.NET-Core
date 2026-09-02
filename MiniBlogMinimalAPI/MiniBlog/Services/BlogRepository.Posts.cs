using MiniBlog.Models;

namespace MiniBlog.Services;

public sealed partial class BlogRepository
{
    public async Task<IReadOnlyList<PostResponse>> GetPostsAsync(
        string? search = null,
        string? categorySlug = null,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            IEnumerable<BlogPost> query = _data.Posts.Where(post => post.IsActive);
            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim();
                query = query.Where(post =>
                    post.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    post.Summary.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    post.Content.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(categorySlug))
            {
                Category? category = _data.Categories.FirstOrDefault(item =>
                    string.Equals(item.Slug, categorySlug, StringComparison.OrdinalIgnoreCase));
                if (category is null)
                {
                    return [];
                }
                query = query.Where(post => post.CategoryIds.Contains(category.Id));
            }

            return query
                .OrderByDescending(post => post.CreatedAt)
                .Select(ToPostResponseUnsafe)
                .ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<PostResponse?> GetPostByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await GetPostAsync(post => post.Id == id, cancellationToken);
    }

    public async Task<PostResponse?> GetPostBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await GetPostAsync(
            post => string.Equals(post.Slug, slug, StringComparison.OrdinalIgnoreCase),
            cancellationToken);
    }

    public async Task<CommandResult<IReadOnlyList<PostResponse>>> GetOwnedPostsAsync(
        string? authorKey,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyDictionary<string, string[]> keyErrors = BlogValidator.ValidateAuthorKey(authorKey);
        if (keyErrors.Count > 0)
        {
            return CommandResult<IReadOnlyList<PostResponse>>.Invalid(keyErrors);
        }

        string authorKeyHash = HashAuthorKey(authorKey!);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            IReadOnlyList<PostResponse> posts = _data.Posts
                .Where(post => string.Equals(post.AuthorKeyHash, authorKeyHash, StringComparison.Ordinal))
                .OrderByDescending(post => post.CreatedAt)
                .Select(ToPostResponseUnsafe)
                .ToArray();
            return CommandResult<IReadOnlyList<PostResponse>>.Success(posts);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<CommandResult<PostResponse>> AddPostAsync(
        PostCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            Dictionary<string, string[]> errors = BlogValidator.ValidatePost(
                request.Title,
                request.Summary,
                request.Content,
                request.ImageUrl,
                request.ExternalUrl,
                request.ExternalLabel,
                request.Author,
                request.CategoryIds,
                _data.Categories)
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
            foreach ((string field, string[] messages) in BlogValidator.ValidateAuthorKey(request.AuthorKey))
            {
                errors[field] = messages;
            }
            if (errors.Count > 0)
            {
                return CommandResult<PostResponse>.Invalid(errors);
            }

            string title = request.Title!.Trim();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            BlogPost post = new(
                NextPostIdUnsafe(),
                title,
                CreateUniquePostSlugUnsafe(title),
                request.Summary!.Trim(),
                request.Content!.Trim(),
                request.ImageUrl!.Trim(),
                request.ExternalUrl?.Trim() ?? string.Empty,
                request.ExternalLabel?.Trim() ?? string.Empty,
                request.Author!.Trim(),
                HashAuthorKey(request.AuthorKey!),
                true,
                request.CategoryIds!.Distinct().ToArray(),
                now,
                now);
            _data.Posts.Add(post);
            await SaveUnsafeAsync(cancellationToken);
            return CommandResult<PostResponse>.Success(ToPostResponseUnsafe(post));
        }
        finally
        {
            _gate.Release();
        }
    }
    public async Task<CommandResult<PostResponse>> EditPostAsync(
        PostEditRequest request,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            int index = _data.Posts.FindIndex(post => post.Id == request.Id);
            if (index < 0)
            {
                return NotFound<PostResponse>("Post");
            }

            IReadOnlyDictionary<string, string[]> errors = BlogValidator.ValidatePost(
                request.Title,
                request.Summary,
                request.Content,
                request.ImageUrl,
                request.ExternalUrl,
                request.ExternalLabel,
                request.Author,
                request.CategoryIds,
                _data.Categories);
            if (errors.Count > 0)
            {
                return CommandResult<PostResponse>.Invalid(errors);
            }

            BlogPost current = _data.Posts[index];
            string title = request.Title!.Trim();
            BlogPost updated = current with
            {
                Title = title,
                Slug = CreateUniquePostSlugUnsafe(title, current.Id),
                Summary = request.Summary!.Trim(),
                Content = request.Content!.Trim(),
                ImageUrl = request.ImageUrl!.Trim(),
                ExternalUrl = request.ExternalUrl?.Trim() ?? string.Empty,
                ExternalLabel = request.ExternalLabel?.Trim() ?? string.Empty,
                Author = request.Author!.Trim(),
                CategoryIds = request.CategoryIds!.Distinct().ToArray(),
                UpdatedAt = DateTimeOffset.UtcNow
            };
            _data.Posts[index] = updated;
            await SaveUnsafeAsync(cancellationToken);
            return CommandResult<PostResponse>.Success(ToPostResponseUnsafe(updated));
        }
        finally
        {
            _gate.Release();
        }
    }
    public async Task<bool> DeletePostAsync(int id, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            int removed = _data.Posts.RemoveAll(post => post.Id == id);
            if (removed == 0)
            {
                return false;
            }
            await SaveUnsafeAsync(cancellationToken);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<CommandResult<PostResponse>> SetPostActiveAsync(
        int id,
        string? authorKey,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyDictionary<string, string[]> keyErrors = BlogValidator.ValidateAuthorKey(authorKey);
        if (keyErrors.Count > 0)
        {
            return CommandResult<PostResponse>.Invalid(keyErrors);
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            int index = _data.Posts.FindIndex(post => post.Id == id);
            if (index < 0)
            {
                return NotFound<PostResponse>("Post");
            }

            BlogPost current = _data.Posts[index];
            if (!AuthorKeyMatches(current.AuthorKeyHash, authorKey!))
            {
                return CommandResult<PostResponse>.Invalid(new Dictionary<string, string[]>
                {
                    ["authorKey"] = ["Only this post's author can change its visibility"]
                });
            }

            if (current.IsActive == isActive)
            {
                return CommandResult<PostResponse>.Success(ToPostResponseUnsafe(current));
            }

            BlogPost updated = current with
            {
                IsActive = isActive,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            _data.Posts[index] = updated;
            await SaveUnsafeAsync(cancellationToken);
            return CommandResult<PostResponse>.Success(ToPostResponseUnsafe(updated));
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<PostResponse?> GetPostAsync(
        Func<BlogPost, bool> predicate,
        CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            BlogPost? post = _data.Posts.FirstOrDefault(post => post.IsActive && predicate(post));
            return post is null ? null : ToPostResponseUnsafe(post);
        }
        finally
        {
            _gate.Release();
        }
    }

    private string CreateUniquePostSlugUnsafe(string title, int? currentId = null)
    {
        string baseSlug = SlugGenerator.Create(title);
        string candidate = baseSlug;
        int suffix = 2;
        while (_data.Posts.Any(post =>
            post.Id != currentId &&
            string.Equals(post.Slug, candidate, StringComparison.OrdinalIgnoreCase)))
        {
            candidate = $"{baseSlug}-{suffix++}";
        }
        return candidate;
    }

    private int NextPostIdUnsafe()
    {
        return _data.Posts.Count == 0 ? 1 : _data.Posts.Max(post => post.Id) + 1;
    }


    private PostResponse ToPostResponseUnsafe(BlogPost post)
    {
        IReadOnlyList<CategoryResponse> categories = _data.Categories
            .Where(category => post.CategoryIds.Contains(category.Id))
            .OrderBy(category => category.Name)
            .Select(ToCategoryResponseUnsafe)
            .ToArray();
        return new PostResponse(
            post.Id,
            post.Title,
            post.Slug,
            post.Summary,
            post.Content,
            post.ImageUrl,
            post.ExternalUrl,
            post.ExternalLabel,
            post.Author,
            post.IsActive,
            categories,
            post.CreatedAt,
            post.UpdatedAt,
            $"/posts/{post.Slug}");
    }
}
