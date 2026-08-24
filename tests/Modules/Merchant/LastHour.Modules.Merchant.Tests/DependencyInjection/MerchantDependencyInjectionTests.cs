using FluentAssertions;
using FluentValidation;
using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Infrastructure.DependencyInjection;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Repositories;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Merchant.Application.Commands;
using LastHour.Modules.Merchant.Application.Models;
using LastHour.Modules.Merchant.Application.Queries;
using LastHour.Modules.Merchant.DependencyInjection;
using LastHour.Modules.Merchant.Domain.Merchants;
using LastHour.Modules.Merchant.Tests.Persistence.TestSupport;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MerchantAggregate = LastHour.Modules.Merchant.Domain.Merchants.Merchant;

namespace LastHour.Modules.Merchant.Tests.DependencyInjection;

public class MerchantDependencyInjectionTests
{
    [Fact]
    public async Task AddLastHourMerchant_RegistersRepositoryAndValidators()
    {
        using SqliteConnection connection = TestMerchantPersistence.CreateOpenConnection();
        await using LastHourDbContext context = await TestMerchantPersistence.CreateContextAsync(connection);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLastHourMerchant();

        services.AddScoped<IRepository<MerchantAggregate>>(_ => new EfRepository<MerchantAggregate>(context));
        services.AddScoped<IReadRepository<MerchantAggregate, MerchantId>>(_ => new EfReadRepository<MerchantAggregate, MerchantId>(context));
        services.AddScoped<ITrackedRepository<MerchantAggregate, MerchantId>>(_ => new EfTrackedRepository<MerchantAggregate, MerchantId>(context));

        using ServiceProvider provider = services.BuildServiceProvider();

        IMerchantRepository? repository = provider.GetService<IMerchantRepository>();
        repository.Should().NotBeNull();

        provider.GetService<IValidator<RegisterMerchantCommand>>().Should().NotBeNull();
        provider.GetService<IValidator<UpdateMerchantProfileCommand>>().Should().NotBeNull();
        provider.GetService<IValidator<VerifyMerchantCommand>>().Should().NotBeNull();
        provider.GetService<IValidator<SuspendMerchantCommand>>().Should().NotBeNull();
        provider.GetService<IValidator<ActivateMerchantCommand>>().Should().NotBeNull();
        provider.GetService<IValidator<CloseMerchantCommand>>().Should().NotBeNull();
        provider.GetService<IValidator<GetMerchantByIdQuery>>().Should().NotBeNull();
        provider.GetService<IValidator<SearchMerchantsQuery>>().Should().NotBeNull();
    }

    [Fact]
    public async Task AddCqrs_WithMerchantAssembly_ResolvesCommandAndQueryHandlers()
    {
        using SqliteConnection connection = TestMerchantPersistence.CreateOpenConnection();
        await using LastHourDbContext context = await TestMerchantPersistence.CreateContextAsync(connection);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLastHourMerchant();
        services.AddCqrs(null, typeof(RegisterMerchantCommand).Assembly);

        services.AddScoped<IRepository<MerchantAggregate>>(_ => new EfRepository<MerchantAggregate>(context));
        services.AddScoped<IReadRepository<MerchantAggregate, MerchantId>>(_ => new EfReadRepository<MerchantAggregate, MerchantId>(context));
        services.AddScoped<ITrackedRepository<MerchantAggregate, MerchantId>>(_ => new EfTrackedRepository<MerchantAggregate, MerchantId>(context));

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetService<IRequestHandler<RegisterMerchantCommand, Result<MerchantId>>>().Should().NotBeNull();
        provider.GetService<IRequestHandler<UpdateMerchantProfileCommand, Result>>().Should().NotBeNull();
        provider.GetService<IRequestHandler<VerifyMerchantCommand, Result>>().Should().NotBeNull();
        provider.GetService<IRequestHandler<SuspendMerchantCommand, Result>>().Should().NotBeNull();
        provider.GetService<IRequestHandler<ActivateMerchantCommand, Result>>().Should().NotBeNull();
        provider.GetService<IRequestHandler<CloseMerchantCommand, Result>>().Should().NotBeNull();

        provider.GetService<IRequestHandler<GetMerchantByIdQuery, Result<MerchantDto>>>().Should().NotBeNull();
        provider.GetService<IRequestHandler<SearchMerchantsQuery, Result<PagedResult<MerchantDto>>>>().Should().NotBeNull();
    }

    [Fact]
    public async Task DbContext_WithMerchantAssembly_DiscoversMerchantConfiguration()
    {
        using SqliteConnection connection = TestMerchantPersistence.CreateOpenConnection();
        await using LastHourDbContext context = await TestMerchantPersistence.CreateContextAsync(connection);

        var entityType = context.Model.FindEntityType(typeof(MerchantAggregate));
        entityType.Should().NotBeNull();
        entityType!.GetTableName().Should().Be("merchants");
    }
}
