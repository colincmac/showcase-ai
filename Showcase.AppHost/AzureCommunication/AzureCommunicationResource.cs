using Aspire.Hosting.Azure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aspire.Hosting.ApplicationModel;
public class AzureCommunicationResource(string name, Action<AzureResourceInfrastructure> configureInfrastructure) : AzureProvisioningResource(name, configureInfrastructure), IResourceWithConnectionString
{
    public BicepSecretOutputReference ConnectionString => new("connectionString", this);
    public ReferenceExpression ConnectionStringExpression =>
    ReferenceExpression.Create($"{ConnectionString}");

}