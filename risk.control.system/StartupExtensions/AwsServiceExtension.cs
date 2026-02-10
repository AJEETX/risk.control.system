using Amazon;
using Amazon.Polly;
using Amazon.Rekognition;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.Textract;
using Amazon.TranscribeService;
using risk.control.system.Helpers;

namespace risk.control.system.StartupExtensions;

public static class AwsServiceExtension
{
    public static IServiceCollection AddAwsServices(this IServiceCollection services, IConfiguration configuration)
    {
        var awsOptions = new Amazon.Extensions.NETCore.Setup.AWSOptions
        {
            Credentials = new BasicAWSCredentials(EnvHelper.Get("aws_id"), EnvHelper.Get("aws_secret")),
            Region = RegionEndpoint.APSoutheast2
        };

        services.AddDefaultAWSOptions(awsOptions);
        services.AddAWSService<IAmazonTranscribeService>();
        services.AddAWSService<IAmazonS3>();
        services.AddAWSService<IAmazonRekognition>();
        services.AddAWSService<IAmazonTextract>();
        services.AddAWSService<IAmazonPolly>();

        AWSConfigs.LoggingConfig.LogTo = LoggingOptions.Console;
        AWSConfigs.LoggingConfig.LogMetrics = true;
        AWSConfigs.LoggingConfig.LogResponses = ResponseLoggingOption.Always;

        return services;
    }
}