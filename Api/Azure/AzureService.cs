using System;
using Azure;
using Azure.Data.AppConfiguration;

namespace Api.Azure;

public interface IAzureService
{
    public Task<Response<ConfigurationSetting>> SaveConfigurationSettingsAsync(string key, string value);
}

public class AzureService(ConfigurationClient client) : IAzureService
{
    async Task<Response<ConfigurationSetting>> IAzureService.SaveConfigurationSettingsAsync(string key, string value)
    {
        var response = await client.SetConfigurationSettingAsync(new ConfigurationSetting(key, value));
        return response;
    }
}
