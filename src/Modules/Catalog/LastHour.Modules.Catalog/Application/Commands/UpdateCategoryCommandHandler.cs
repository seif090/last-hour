using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.SharedKernel.Domain.Exceptions;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Catalog.Domain.Categories;
using Microsoft.Extensions.Logging;

namespace LastHour.Modules.Catalog.Application.Commands;

/// <summary>
/// Handles updating the details of an existing category.
/// </summary>
public sealed class UpdateCategoryCommandHandler : ICommandHandler<UpdateCategoryCommand>
{
    private static readonly Action<ILogger, Guid, Exception?> CategoryUpdated =
        LoggerMessage.Define<Guid>(
            LogLevel.Information,
            new EventId(4002, nameof(UpdateCategoryCommandHandler)),
            "Category {CategoryId} updated.");

    private readonly ICategoryRepository _categories;
    private readonly ILogger<UpdateCategoryCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateCategoryCommandHandler"/> class.
    /// </summary>
    /// <param name="categories">The category repository.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public UpdateCategoryCommandHandler(
        ICategoryRepository categories,
        ILogger<UpdateCategoryCommandHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(categories);
        ArgumentNullException.ThrowIfNull(logger);
        _categories = categories;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(UpdateCategoryCommand command, CancellationToken cancellationToken)
    {
        CategoryId categoryId = CategoryId.Create(command.Id);
        Category? category = await _categories.GetByIdTrackedAsync(categoryId, cancellationToken).ConfigureAwait(false);

        if (category is null)
        {
            return Result.Failure(Error.NotFound(
                "Categories.NotFound",
                "The category could not be found."));
        }

        CategoryId? parentCategoryId = null;
        if (command.ParentCategoryId.HasValue)
        {
            if (command.ParentCategoryId.Value == command.Id)
            {
                return Result.Failure(Error.Validation(
                    "Categories.SelfParentingNotAllowed",
                    "A category cannot reference itself as its parent category."));
            }

            parentCategoryId = CategoryId.Create(command.ParentCategoryId.Value);
            if (!await _categories.ExistsByIdAsync(parentCategoryId, cancellationToken).ConfigureAwait(false))
            {
                return Result.Failure(Error.NotFound(
                    "Categories.ParentNotFound",
                    "The parent category could not be found."));
            }
        }

        try
        {
            category.Update(command.Name, command.Description, parentCategoryId);
        }
        catch (InvalidCategoryNameException exception)
        {
            return Result.Failure(
                Error.Validation("InvalidCategoryName", exception.Message));
        }
        catch (BusinessRuleValidationException exception)
        {
            return Result.Failure(
                Error.Validation("Categories.SelfParentingNotAllowed", exception.Message));
        }

        CategoryUpdated(_logger, category.Id.Value, null);
        return Result.Success();
    }
}
