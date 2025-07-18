﻿using Amazon.Textract;
using Amazon.Textract.Model;

namespace risk.control.system.Helpers;

public class TextDetection
{
    public static async Task<List<Block>> ExtractTextDataAsync(byte[] bytes)
    {
        var awsAccessKeyId = Environment.GetEnvironmentVariable("aws_id");
        var awsSecretAccessKey = Environment.GetEnvironmentVariable("aws_secret");
        using var textractClient = new AmazonTextractClient(awsAccessKeyId, awsSecretAccessKey);
        var request = new DetectDocumentTextRequest
        {
            Document = new Document { Bytes = new MemoryStream(bytes) }
        };

        var response = await textractClient.DetectDocumentTextAsync(request);
        return response.Blocks;
    }

}
