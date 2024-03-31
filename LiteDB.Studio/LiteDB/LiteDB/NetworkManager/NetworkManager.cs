using System;
using System.Text;
using System.Drawing;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Web;
//using Duke;
using Mono.Web;
using System.Threading.Tasks;
using Tensorflow;
using System.IO;
namespace LiteDB
{
    internal class NetworkManager
    {
        // Private static instance variable
        private static NetworkManager _instance;

        // Private constructor to prevent instantiation
        private NetworkManager()
        {
            // Initialization logic if needed
        }

        // Public static method to get the instance
        public static NetworkManager GetInstance()
        {
            // If the instance is null, create a new instance
            if (_instance == null)
            {
                _instance = new NetworkManager();
            }
            return _instance;
        }
        public  async void MakeRequest()
        {
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "");

            // Request parameters
            queryString["features"] = "tags";
           // queryString["model-name"] = "{string}";
            queryString["language"] = "en";
          //  queryString["smartcrops-aspect-ratios"] = "{string}";
            queryString["gender-neutral-caption"] = "False";
            string endPoint = "imagelabel.cognitiveservices.azure.com";
            var uri = $"https://{endPoint}/computervision/imageanalysis:analyze?api-version=2023-02-01-preview&" + queryString;

            HttpResponseMessage response;

            // Request body
            byte[] byteData = ConvertImageToByteArray(@"D:\dog.jpg");

            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                response = await client.PostAsync(uri, content);

                Console.WriteLine(response.Content);

                Console.WriteLine(response);

                if (response.IsSuccessStatusCode) // Check for successful response
                {
                    string jsonResponse;
                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    {
                        using (var reader = new StreamReader(contentStream))
                        {
                            jsonResponse = await reader.ReadToEndAsync();
                        }
                    }

                    // Write JSON response to a file (handle potential errors)
                    try
                    {
                        string outputFilePath = @"D:\image_analysis_response.json"; // Customize this path
                        File.WriteAllText(outputFilePath, jsonResponse);
                        Console.WriteLine($"JSON response written to '{outputFilePath}'.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error writing JSON response to file: {ex.Message}");
                    }
                }
            }

        }

        public  byte[] ConvertImageToByteArray(string imagePath)
        {
            using (var image = Image.FromFile(imagePath))
            {
                using (var ms = new MemoryStream())
                {
                    image.Save(ms, image.RawFormat); // Use the image's original format
                    return ms.ToArray();
                }
            }
        }

    }
}
