using System.Linq.Expressions;
using System.Reflection;
using LastHour.BuildingBlocks.Infrastructure.StronglyTypedIds;
using LastHour.BuildingBlocks.SharedKernel.Domain;
using LastHour.BuildingBlocks.SharedKernel.Domain.StronglyTypedIds;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LastHour.BuildingBlocks.Infrastructure.Persistence.Configuration;

/// <summary>
/// Cross-cutting model conventions shared by every <see cref="LastHourDbContext"/> instance:
/// automatic value conversion for strongly typed identifiers and soft-delete query filters.
/// </summary>
public static class DbContextModelConventions
{
    /// <summary>
    /// Registers strongly typed identifier value converters. EF Core lazily discovers properties
    /// typed as <see cref="IStronglyTypedId{TValue,T}"/> as navigations to a phantom entity type,
    /// so each such property is ignored and remapped as a scalar property backed by a value
    /// converter; the phantom entity types are then dropped from the model.
    /// </summary>
    /// <param name="modelBuilder">The model builder being configured.</param>
    public static void ApplyStronglyTypedIdConverters(this ModelBuilder modelBuilder)
    {
        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes().ToList())
        {
            foreach (PropertyInfo propertyInfo in entityType.ClrType.GetProperties())
            {
                Type? idInterface = GetStronglyTypedIdInterface(propertyInfo.PropertyType);
                if (idInterface is null)
                {
                    continue;
                }

                string propertyName = propertyInfo.Name;

                if (entityType.FindProperty(propertyName) is not null)
                {
                    continue;
                }

                Type valueType = idInterface.GetGenericArguments()[0];
                Type converterType = typeof(StronglyTypedIdValueConverter<,>).MakeGenericType(
                    valueType,
                    propertyInfo.PropertyType);

                object? instance = converterType.GetField(
                    nameof(StronglyTypedIdValueConverter<Guid, GuidId>.Instance),
                    BindingFlags.Public | BindingFlags.Static)?.GetValue(null);

                if (instance is not ValueConverter converter)
                {
                    continue;
                }

                EntityTypeBuilder entityTypeBuilder = modelBuilder.Entity(entityType.ClrType);
                entityTypeBuilder.Ignore(propertyName);
                entityTypeBuilder.Property(propertyName).HasConversion(converter);
            }
        }

        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes().ToList())
        {
            if (GetStronglyTypedIdInterface(entityType.ClrType) is not null)
            {
                modelBuilder.Ignore(entityType.ClrType);
            }
        }
    }

    /// <summary>
    /// Applies a query filter to every soft-deletable entity so deleted rows are hidden from all
    /// queries without any module-specific configuration.
    /// </summary>
    /// <param name="modelBuilder">The model builder being configured.</param>
    public static void ApplySoftDeleteQueryFilters(this ModelBuilder modelBuilder)
    {
        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            ParameterExpression parameter = Expression.Parameter(entityType.ClrType, "entity");
            Expression body = Expression.Not(Expression.Property(parameter, nameof(ISoftDelete.IsDeleted)));
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(Expression.Lambda(body, parameter));
        }
    }

    private static Type? GetStronglyTypedIdInterface(Type propertyType)
        => propertyType.GetInterfaces().FirstOrDefault(
            candidate => candidate.IsGenericType
                         && candidate.GetGenericTypeDefinition() == typeof(IStronglyTypedId<,>));
}
