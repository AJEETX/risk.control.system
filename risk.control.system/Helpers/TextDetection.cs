using Amazon.Textract;
using Amazon.Textract.Model;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace risk.control.system.Helpers;

public class TextDetection
{
    public static async Task<List<Block>> ExtractTextDataAsync(byte[] bytes)
    {
        var awsAccessKeyId = Environment.GetEnvironmentVariable("aws_id");
        var awsSecretAccessKey = Environment.GetEnvironmentVariable("aws_secret");
        using var textractClient = new AmazonTextractClient(awsAccessKeyId, awsSecretAccessKey, Amazon.RegionEndpoint.USEast1);
        var request = new DetectDocumentTextRequest
        {
            Document = new Document { Bytes = new MemoryStream(bytes) }
        };

        var response = await textractClient.DetectDocumentTextAsync(request);
        return response.Blocks;
    }

}
