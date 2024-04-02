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
using Newtonsoft.Json;
using static HDF.PInvoke.H5T;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Linq;
using IronPython.Runtime;
using Azure;
using System.Text.RegularExpressions;
namespace LiteDB
{
   
    public enum VectorizeDataType
    {
        Image,
        Text,
    }

    internal class NetworkManager
    {
        // Private static instance variable
        private static NetworkManager _instance;
        public Dictionary<string,List<float>> ImageVector = new Dictionary<string,List<float>>();
        private float[] DesVT = new float[] { };
        private bool isLoaded = false;
        public bool VectorizerDone = false;
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
            float[] vectorImg = new float[] { };
            float[] vectorTxt = new float[] { };    
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "");
            queryString["model-version"] = "2022-04-11";
            //  queryString["smartcrops-aspect-ratios"] = "{string}";
           // queryString["gender-neutral-caption"] = "False";
            string endPoint = "imagelabel.cognitiveservices.azure.com";
            var uri = $"https://{endPoint}/computervision/retrieval:vectorizeImage?api-version=2023-02-01-preview&" + queryString;

            HttpResponseMessage response;

            // Request body  Features =
           // byte[] byteData = Encoding.UTF8.GetBytes(@"D:\dog.jpg");
            byte[] byteData = ConvertImageToByteArray(@"D:\Miner.png");


            //parse image to vector

            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                response = await client.PostAsync(uri, content);

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

                    JObject jsonObject = JObject.Parse(jsonResponse);
                    JArray vectorArray = (JArray)jsonObject["vector"];
                    vectorImg = vectorArray.Select(token => (float)token).ToArray();



                }
            }


            //parse text to vector

            var queryStringnew = HttpUtility.ParseQueryString(string.Empty);
               queryStringnew["model-version"] = "2023-04-15";
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "");
            var uriNew = "https://imagelabel.cognitiveservices.azure.com/computervision/retrieval:vectorizeText?api-version=2023-02-01-preview&model-version=2022-04-11";
                string p = "{\"text\": \"A man hold a kinfe\"}";
            byte[] prompt = Encoding.UTF8.GetBytes(p);
            using (var aa = new ByteArrayContent(prompt))
            {
                aa.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                response = await client.PostAsync(uriNew, aa);


                if (response.IsSuccessStatusCode) // Check for successful response
                {
                    string jsonResponse;
                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    {
                        using (var reader = new StreamReader(contentStream))
                        {
                            jsonResponse = await reader.ReadToEndAsync();
                        }

                        JObject jsonObject = JObject.Parse(jsonResponse);
                        JArray vectorArray = (JArray)jsonObject["vector"];
                        vectorTxt =  vectorArray.Select(token => (float)token).ToArray();

                    }
                }
            }
            Console.WriteLine(vectorImg.Length); 
            Console.WriteLine(vectorTxt.Length);
            float confident = GetCosineSimilarity(vectorTxt, vectorImg);
            Console.WriteLine($"{confident}");  
        }

        public  float GetCosineSimilarity(float[] vector1, float[] vector2)
        {
            float dotProduct = 0;
            int length = Math.Min(vector1.Length, vector2.Length);
            for (int i = 0; i < length; i++)
            {
                dotProduct += vector1[i] * vector2[i];
            }
            float magnitude1 = (float)Math.Sqrt(vector1.Select(x => x * x).Sum());
            float magnitude2 = (float)Math.Sqrt(vector2.Select(x => x * x).Sum());

            return dotProduct / (magnitude1 * magnitude2);
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

        //load vector file from folder
        public void LoadVector()
        {
            if (isLoaded) return;

            string folderPath = @"D:\tets\mod-lite-db\test\Vectorize";
             var listFilePath =    GetFileList(folderPath);
            for (int i = 0; i < listFilePath.Count; i++)
            {
                float[] floatArr = LoadFloatArrayFromTextFile(listFilePath[i]);
                string id = Path.GetFileName(listFilePath[i]).Replace(".txt","");
                if (!ImageVector.ContainsKey(id))
                {
                ImageVector.Add(id,floatArr.ToList());      

                }
            }


            isLoaded = true;    

        }




        public async void Vectorizer(VectorizeDataType type, string idOrTxtdes ="", string path ="", object param = null)
        {
            VectorizerDone = false;
            float[] vtResult = new float[] { };
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);
            queryString["model-version"] = "2022-04-11";
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "");
            string endPoint = "imagelabel.cognitiveservices.azure.com";
            HttpResponseMessage response;

            //INSERT 
            if (type == VectorizeDataType.Image)
            {
                var uri = $"https://{endPoint}/computervision/retrieval:vectorizeImage?api-version=2023-02-01-preview&" + queryString;
                byte[] byteData = ConvertImageToByteArray(path);

                using (var content = new ByteArrayContent(byteData))
                {
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    response = await client.PostAsync(uri, content);

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

                        JObject jsonObject = JObject.Parse(jsonResponse);
                        JArray vectorArray = (JArray)jsonObject["vector"];
                      
                        vtResult = vectorArray.Select(token => (float)token).ToArray();

                        string id = ExtractID(idOrTxtdes);
                        if (!ImageVector.ContainsKey(id))
                        {
                            ImageVector.Add(id, vtResult.ToList());
                        //    string ID = ExtractID(idOrTxtdes);  

                            //save to file
                            string filePath = Path.Combine(@"D:\tets\mod-lite-db\test\Vectorize", id + ".txt");

                            SaveFloatArrayToTextFile(vtResult, filePath);
                        }
                    }

                   


                }

                //SELECT QUERRY
            } else if (type == VectorizeDataType.Text) 
            {
                //load data

                LoadVector();

                var uri = $"https://{endPoint}/computervision/retrieval:vectorizeText?api-version=2023-02-01-preview&" + queryString;
                string des = idOrTxtdes.Replace("\"", "");
               
                string p = "{ \"text\": \"" + idOrTxtdes + "\" }";

                byte[] prompt = Encoding.UTF8.GetBytes(p);
                using (var aa = new ByteArrayContent(prompt))
                {
                    aa.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    response = await client.PostAsync(uri, aa);


                    if (response.IsSuccessStatusCode) // Check for successful response
                    {
                        string jsonResponse;
                        using (var contentStream = await response.Content.ReadAsStreamAsync())
                        {
                            using (var reader = new StreamReader(contentStream))
                            {
                                jsonResponse = await reader.ReadToEndAsync();
                            }

                            JObject jsonObject = JObject.Parse(jsonResponse);
                            JArray vectorArray = (JArray)jsonObject["vector"];
                            DesVT = vectorArray.Select(token => (float)token).ToArray();

                        }

                       
                    }
                }
            }

            VectorizerDone = true;  
        }
      

        public List<BsonDocument> ImageRetrieval(List<BsonDocument> orginalList)
        {
           
            List<float> result=  new List<float>();   
            foreach(var kvp in ImageVector)
            {
                float confident = GetCosineSimilarity(DesVT, kvp.Value.ToArray());
                result.Add(confident);  
            }
            // get the most confident

            int maxIndex = result.IndexOf(result.Max());

            //remove unmatch bson doc


            return new List<BsonDocument>() { orginalList[maxIndex] };      
        }
        public  float[] LoadFloatArrayFromTextFile(string filePath)
        {
            List<float> data = new List<float>();
            string line;

            // Use a try-catch block to handle potential file access errors
            try
            {
                using (StreamReader reader = new StreamReader(filePath))
                {
                    while ((line = reader.ReadLine()) != null)
                    {
                        // Split the line based on the comma separator (adjust if needed)
                        string[] stringValues = line.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (string value in stringValues)
                        {
                            // Convert each string value to float and add it to the list
                            float floatValue;
                            if (float.TryParse(value, out floatValue))
                            {
                                data.Add(floatValue);
                            }
                            else
                            {
                                // Handle invalid values (optional)
                                // You could throw an exception, log an error, or ignore them
                                Console.WriteLine($"Error parsing value: {value}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading file: {ex.Message}");
                // Re-throw the exception or handle it here as needed
            }

            // Convert the List<float> to a float array
            return data.ToArray();
        }

        public string ExtractID(string IDString)
        {

            // Regular expression pattern to match the object ID
            string pattern = @"\:\s*(?:""([^""]*))";  // Matches with or without quotes
            Match match1 = Regex.Match(IDString, pattern);
            if (match1.Success) 
            { 
                return match1.Groups[1].Value;  
            }
            return "";

        }
        public void SaveFloatArrayToTextFile(float[] data, string filePath)
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                for (int i = 0; i < data.Length; i++)
                {
                    writer.Write(data[i]);
                    if (i < data.Length - 1)
                    {
                        writer.Write(","); // Add comma as separator (adjust as needed)
                    }
                }
            }
        }


        public  List<string> GetFileList(string folderPath)
        {
            if (Directory.Exists(folderPath))
            {
                return Directory.GetFiles(folderPath).ToList();
            }
            else
            {
                Console.WriteLine($"Error: Folder '{folderPath}' does not exist.");
                return new List<string>();
            }
        }





    }



}
