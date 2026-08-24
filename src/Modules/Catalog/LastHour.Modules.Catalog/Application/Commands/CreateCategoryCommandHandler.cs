using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.SharedKernel.Domain.Exceptions;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Catalog.Domain.Categories;
using Microsoft.Extensions.Logging;

namespace LastHour.Modules.Catalog.Application.Commands;

/// <summary>
/// Handles the creation of a new category aggregate root.
/// </summary>
public sealed class CreateCategoryCommandHandler : ICommandHandler<CreateCategoryCommand, CategoryId>
{
    private static readonly Action<ILogger, Guid, string, Exception?> CategoryCreated =
        LoggerMessage.Define<Guid, string>(
            LogLevel.Information,
            new EventId(4001, nameof(CreateCategoryCommandHandler)),
            "Category {CategoryId} created with name {Name}.");

    private readonly ICategoryRepository _categories;
    private readonly ILogger<CreateCategoryCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateCategoryCommandHandler"/> class.
    /// </summary>
    /// <param name="categories">The category repository.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public CreateCategoryCommandHandler(
        ICategoryRepository categories,
        ILogger<CreateCategoryCommandHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(categories);
        ArgumentNullException.ThrowIfNull(logger);
        _categories = categories;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Result<CategoryId>> Handle(CreateCategoryCommand command, CancellationToken cancellationToken)
    {
        CategoryId? parentCategoryId = null;
        if (command.ParentCategoryId.HasValue)
        {
            parentCategoryId = CategoryId.Create(command.ParentCategoryId.Value);
            if (!await _categories.ExistsByIdAsync(parentCategoryId, cancellationToken).ConfigureAwait(false))
            {
                return Result<CategoryId>.Failure(Error.NotFound(
                    "Categories.ParentNotFound",
                    "The parent category could not be found."));
            }
        }

        Category category;
        try
        {
            category = Category.Create(command.Name, command.Description, parentCategoryId);
        }
        catch (InvalidCategoryNameException exception)
        {
            return Result<CategoryId>.Failure(
                Error.Validation("InvalidCategoryName", exception.Message));
        }
        catch (BusinessRuleValidationException exception)
        {
            return Result<CategoryId>.Failure(
                Error.Validation("Categories.SelfParentingNotAllowed", exception.Message));
        }

        _categories.Add(category);

        CategoryCreated(_logger, category.Id.Value, category.Name, null);
        return Result<CategoryId>.Success(category.Id);
    }
}
