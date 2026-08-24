using LastHour.BuildingBlocks.SharedKernel.Domain;
using LastHour.BuildingBlocks.SharedKernel.Domain.Exceptions;

namespace LastHour.Modules.Catalog.Domain.Categories;

/// <summary>
/// The category aggregate root of the catalog module. Categories organize products into
/// hierarchical taxonomies. The aggregate owns the category invariants: a valid name, an optional
/// description, and an optional parent category reference that cannot point to the category itself.
/// </summary>
public sealed class Category : AggregateRoot<CategoryId>, IAuditableEntity
{
    /// <summary>
    /// The maximum length of a category name.
    /// </summary>
    public const int MaxNameLength = 100;

    /// <summary>
    /// The maximum length of a category description.
    /// </summary>
    public const int MaxDescriptionLength = 500;

    private Category()
    {
        // EF Core materialization only.
    }

    private Category(CategoryId id, string name, string? description, CategoryId? parentCategoryId)
    {
        Id = id;
        Name = name;
        Description = description;
        ParentCategoryId = parentCategoryId;
    }

    /// <summary>
    /// Gets the name of the category.
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Gets the optional description of the category.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Gets the identifier of the parent category, or <see langword="null"/> when this is a root category.
    /// </summary>
    public CategoryId? ParentCategoryId { get; private set; }

    /// <inheritdoc/>
    public DateTime CreatedAt { get; set; }

    /// <inheritdoc/>
    public string? CreatedBy { get; set; }

    /// <inheritdoc/>
    public DateTime? UpdatedAt { get; set; }

    /// <inheritdoc/>
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Creates a new category.
    /// </summary>
    /// <param name="name">The category name.</param>
    /// <param name="description">An optional description.</param>
    /// <param name="parentCategoryId">An optional parent category identifier.</param>
    /// <returns>A new <see cref="Category"/> instance.</returns>
    /// <exception cref="InvalidCategoryNameException"><paramref name="name"/> is null, empty, or exceeds <see cref="MaxNameLength"/> characters, or description exceeds <see cref="MaxDescriptionLength"/>.</exception>
    /// <exception cref="BusinessRuleValidationException"><paramref name="parentCategoryId"/> is identical to the created category identifier.</exception>
    public static Category Create(string name, string? description = null, CategoryId? parentCategoryId = null)
    {
        CategoryId id = CategoryId.New();
        string validatedName = ValidateName(name);
        string? validatedDescription = ValidateDescription(description);

        if (parentCategoryId is not null && parentCategoryId == id)
        {
            throw new BusinessRuleValidationException(
                new CategorySelfParentRule("A category cannot reference itself as its parent category."));
        }

        return new Category(id, validatedName, validatedDescription, parentCategoryId);
    }

    /// <summary>
    /// Updates the category details.
    /// </summary>
    /// <param name="name">The updated category name.</param>
    /// <param name="description">The updated optional description.</param>
    /// <param name="parentCategoryId">The updated optional parent category identifier.</param>
    /// <exception cref="InvalidCategoryNameException"><paramref name="name"/> is null, empty, or exceeds <see cref="MaxNameLength"/> characters.</exception>
    /// <exception cref="BusinessRuleValidationException"><paramref name="parentCategoryId"/> references this category.</exception>
    public void Update(string name, string? description = null, CategoryId? parentCategoryId = null)
    {
        string validatedName = ValidateName(name);
        string? validatedDescription = ValidateDescription(description);

        if (parentCategoryId is not null && parentCategoryId == Id)
        {
            throw new BusinessRuleValidationException(
                new CategorySelfParentRule("A category cannot reference itself as its parent category."));
        }

        Name = validatedName;
        Description = validatedDescription;
        ParentCategoryId = parentCategoryId;
    }

    private static string ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidCategoryNameException("A category name is required.");
        }

        string trimmed = name.Trim();
        if (trimmed.Length > MaxNameLength)
        {
            throw new InvalidCategoryNameException($"A category name cannot exceed {MaxNameLength} characters.");
        }

        return trimmed;
    }

    private static string? ValidateDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        string trimmed = description.Trim();
        if (trimmed.Length > MaxDescriptionLength)
        {
            throw new InvalidCategoryNameException($"A category description cannot exceed {MaxDescriptionLength} characters.");
        }

        return trimmed;
    }

    private sealed class CategorySelfParentRule : IBusinessRule
    {
        public CategorySelfParentRule(string message)
        {
            Message = message;
        }

        public bool IsSatisfied => false;

        public string Message { get; }
    }
}
