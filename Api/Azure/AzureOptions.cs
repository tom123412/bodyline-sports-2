using System;

namespace Api.Azure;

public class AzureOptions
{
    public required Uri AppConfigurationEndpoint { get; set; }
    public string? AppConfigurationConnectionString { get; set; }
}
