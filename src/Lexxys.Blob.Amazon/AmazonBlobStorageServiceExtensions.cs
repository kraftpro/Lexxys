using Amazon.S3;
using Microsoft.Extensions.DependencyInjection;

namespace Lexxys;

public static class AmazonBlobStorageServiceExtensions
{
    /// <summary>
    /// Adds the Amazon Blob Storage Service to the service collection and registers it with the blob storage factory.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure the AmazonS3Config.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddAmazonBlobStorageService(this IServiceCollection services, string domain, string bucketName, Action<AmazonS3Config> configure)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        // Ensure the blob storage factory is registered
        //services.AddBlobStorage(domain);

        // Register the Amazon Blob Storage Service as a singleton
        services.AddSingleton<IBlobStorageService>(sp =>
        {
            var config = new AmazonS3Config();
            configure(config);
            var service = new AmazonBlobStorageService(bucketName, config);

            // Register the service with the factory
            var factory = sp.GetRequiredService<IBlobStorageFactory>();
            factory.RegisterStorage(domain, service);

            return service;
        });

        return services;
    }
}
