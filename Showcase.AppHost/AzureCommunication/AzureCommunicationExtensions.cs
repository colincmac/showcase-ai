using Aspire.Hosting.Azure;
using Azure.Provisioning.Expressions;
using Azure.Provisioning;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Provisioning.Communication;
using Azure.Provisioning.KeyVault;
using Azure.Provisioning.CosmosDB;
using Microsoft.Extensions.Hosting;
namespace Showcase.AppHost.Calling;
public static class AzureCommunicationExtensions
{
    public static IResourceBuilder<AzureCommunicationResource> AddAzureCommunication(this IDistributedApplicationBuilder builder, [ResourceName] string name)
    {
        builder.AddAzureProvisioning();

        AzureCommunicationResource resource = new(name, ConfigureCommunication);
        return builder.AddResource(resource)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }

    private static void ConfigureCommunication(AzureResourceInfrastructure infrastructure)
    {
        var kvNameParam = new ProvisioningParameter(AzureBicepResource.KnownParameters.KeyVaultName, typeof(string));
        infrastructure.Add(kvNameParam);
        var keyVault = KeyVaultService.FromExisting("keyVault");
        keyVault.Name = kvNameParam;
        infrastructure.Add(keyVault);

        var locationParameter = new ProvisioningParameter(AzureBicepResource.KnownParameters.Location, typeof(string))
        {
            Value = "global"
        };
        infrastructure.Add(locationParameter);

        var dataLocationParameter = new ProvisioningParameter("dataLocation", typeof(string))
        {
            Value = "unitedstates"
        };
        infrastructure.Add(dataLocationParameter);

        var service = new CommunicationService(infrastructure.AspireResource.GetBicepIdentifier())
        {
            Location = locationParameter,
            DataLocation = dataLocationParameter,
            Tags = { { "aspire-resource-name", infrastructure.AspireResource.Name } }
        };

        infrastructure.Add(service);

        var secret = new KeyVaultSecret("connectionString")
        {
            Parent = keyVault,
            Name = "connectionString",
            Properties = new SecretProperties
            {
                Value = BicepFunction.Interpolate($"endpoint={service.HostName};accesskey={service.GetKeys().PrimaryKey};")
            }
        };
        infrastructure.Add(secret);
    }

    //public static void AddAzureCommunication(this IHostApplicationBuilder builder, string connectionName)
    //{
    //    builder.Services.AddSingleton(Configurae);
    //}
}
