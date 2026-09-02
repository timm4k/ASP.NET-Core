using MiniBlog.Models;

namespace MiniBlog.Services;

public sealed partial class BlogRepository
{
    public async Task<IReadOnlyList<CategoryResponse>> GetCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return _data.Categories
                .OrderBy(category => category.Name)
                .Select(ToCategoryResponseUnsafe)
                .ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<CommandResult<CategoryResponse>> AddCategoryAsync(
        CategoryCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            IReadOnlyDictionary<string, string[]> errors = ValidateCategoryUnsafe(request.Name);
            if (errors.Count > 0)
            {
                return CommandResult<CategoryResponse>.Invalid(errors);
            }

            string name = request.Name!.Trim();
            Category category = new(
                NextCategoryIdUnsafe(),
                name,
                SlugGenerator.Create(name));
            _data.Categories.Add(category);
            await SaveUnsafeAsync(cancellationToken);
            return CommandResult<CategoryResponse>.Success(ToCategoryResponseUnsafe(category));
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<CommandResult<CategoryResponse>> EditCategoryAsync(
        CategoryEditRequest request,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            int index = _data.Categories.FindIndex(category => category.Id == request.Id);
            if (index < 0)
            {
                return NotFound<CategoryResponse>("Category");
            }

            IReadOnlyDictionary<string, string[]> errors = ValidateCategoryUnsafe(request.Name, request.Id);
            if (errors.Count > 0)
            {
                return CommandResult<CategoryResponse>.Invalid(errors);
            }

            Category updated = _data.Categories[index] with
            {
                Name = request.Name!.Trim(),
                Slug = SlugGenerator.Create(request.Name!)
            };
            _data.Categories[index] = updated;
            await SaveUnsafeAsync(cancellationToken);
            return CommandResult<CategoryResponse>.Success(ToCategoryResponseUnsafe(updated));
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> DeleteCategoryAsync(int id, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            int removed = _data.Categories.RemoveAll(category => category.Id == id);
            if (removed == 0)
            {
                return false;
            }

            for (int index = 0; index < _data.Posts.Count; index++)
            {
                BlogPost post = _data.Posts[index];
                if (!post.CategoryIds.Contains(id))
                {
                    continue;
                }

                _data.Posts[index] = post with
                {
                    CategoryIds = post.CategoryIds.Where(categoryId => categoryId != id).ToArray(),
                    UpdatedAt = DateTimeOffset.UtcNow
                };
            }

            await SaveUnsafeAsync(cancellationToken);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    private IReadOnlyDictionary<string, string[]> ValidateCategoryUnsafe(string? name, int? currentId = null)
    {
        Dictionary<string, string[]> errors = BlogValidator.ValidateCategory(name)
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        if (errors.Count > 0)
        {
            return errors;
        }

        string slug = SlugGenerator.Create(name!);
        bool duplicate = _data.Categories.Any(category =>
            category.Id != currentId &&
            string.Equals(category.Slug, slug, StringComparison.OrdinalIgnoreCase));
        if (duplicate)
        {
            errors["name"] = ["A category with this name already exists"];
        }
        return errors;
    }

    private int NextCategoryIdUnsafe()
    {
        return _data.Categories.Count == 0 ? 1 : _data.Categories.Max(category => category.Id) + 1;
    }

    private CategoryResponse ToCategoryResponseUnsafe(Category category)
    {
        int postCount = _data.Posts.Count(post => post.IsActive && post.CategoryIds.Contains(category.Id));
        return new CategoryResponse(
            category.Id,
            category.Name,
            category.Slug,
            postCount,
            $"/posts/category/{Uri.EscapeDataString(category.Slug)}");
    }
}
