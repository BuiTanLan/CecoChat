using CecoChat.Contracts.Config;
using CecoChat.Data.Config;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace CecoChat.Server.Admin.Endpoints;

public class ConfigService : Config.ConfigBase
{
    private readonly ILogger _logger;
    private readonly ConfigDbContext _configDbContext;

    public ConfigService(
        ILogger<ConfigService> logger,
        ConfigDbContext configDbContext)
    {
        _logger = logger;
        _configDbContext = configDbContext;
    }

    public override async Task<GetConfigElementsResponse> GetConfigElements(GetConfigElementsRequest request, ServerCallContext context)
    {
        ElementEntity[] entities = await _configDbContext.Elements.Where(element => element.Name.StartsWith(request.ConfigSection)).ToArrayAsync();
        GetConfigElementsResponse response = new();

        foreach (ElementEntity entity in entities)
        {
            Contracts.Config.ConfigElement element = new()
            {
                Name = entity.Name,
                Value = entity.Value,
                Version = entity.Version.ToTimestamp()
            };
            response.Elements.Add(element);
        }

        _logger.LogTrace("Responding with {ConfigElementCount} config elements for config section {ConfigSection}", response.Elements.Count, request.ConfigSection);
        return response;
    }
}
