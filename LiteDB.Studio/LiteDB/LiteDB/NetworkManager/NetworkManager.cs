using System;
using System.Text;
using System.Drawing;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Web;
//using Duke;
using Mono.Web;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
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
        private string API_Key = "dc6fb8cc97d449ada0231de2ed93d498";
        private float _limitConfident = .2f;
        private float _minConfident = .15f;
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
        public void LoadVector(string collection)
        {
            if (isLoaded) return;

            //   string folderPath = @"D:\tets\mod-lite-db\test\Vectorize";
            string saveDirectory = Path.GetDirectoryName(ConnectionManager.GetInstance().ConnectionString.Filename);
            saveDirectory = Path.Combine(saveDirectory, "Vectorize",collection);
          
            var listFilePath =    GetFileList(saveDirectory);
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




        public async Task Vectorizer(VectorizeDataType type, string idOrTxtdes ="", string path ="", object param = null)
        {
            VectorizerDone = false;
            float[] vtResult = new float[] { };
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);
            queryString["model-version"] = "2022-04-11";
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", API_Key);
            string endPoint = "imagelabel.cognitiveservices.azure.com";
            HttpResponseMessage response;

            //INSERT @"
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
                            string saveDirectory = Path.GetDirectoryName(ConnectionManager.GetInstance().ConnectionString.Filename);
                            saveDirectory = Path.Combine(saveDirectory, "Vectorize",(string)param);
                            if (!Directory.Exists(saveDirectory))
                            {
                                // If not, create it
                                Directory.CreateDirectory(saveDirectory);
                                Console.WriteLine("Folder created successfully.");
                            }

                            //save to file
                            string filePath = Path.Combine(saveDirectory, id + ".txt");

                            SaveFloatArrayToTextFile(vtResult, filePath);
                        }
                    }

                   


                }

                //SELECT QUERRY
            } else if (type == VectorizeDataType.Text) 
            {
                //load data

                LoadVector((string)param);

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
      

        public List<BsonDocument> ImageRetrieval(List<BsonDocument> orginalList, Query querry)
        {
           
            List<float> result=  new List<float>();   
            foreach(var kvp in ImageVector)
            {
                float confident = GetCosineSimilarity(DesVT, kvp.Value.ToArray());
                result.Add(confident);  
            }
            int numberOfResult = querry.ImageLimit;
        
            List<(float, BsonDocument)> pairedList = new List<(float, BsonDocument)>();

            for(int i = 0; i < orginalList.Count;i++)
            {              
                pairedList.Add((result[i], orginalList[i]));
            }

            pairedList.Sort((x, y) => y.Item1.CompareTo(x.Item1));

            if (numberOfResult < pairedList.Count)
            {
                pairedList = pairedList.GetRange(0, numberOfResult);
            }

            if (querry.ImageLimit == int.MaxValue)
            {
                //calculate confident threshold
                _limitConfident = CalculateAverageDistance(result.ToArray());
                float mostConfident = pairedList[0].Item1;
                //   pairedList.RemoveAll(pair => pair.Item1 < _limitConfident);
                pairedList = pairedList
                    .Where(item => mostConfident - item.Item1 < _limitConfident)
                    .ToList();
            }
          

            return pairedList.Select(x=> x.Item2).ToList();    
        }
        public  float[] LoadFloatArrayFromTextFile(string filePath)
        {
            List<float> data = new List<float>();
            string line;

            try
            {
                using (StreamReader reader = new StreamReader(filePath))
                {
                    while ((line = reader.ReadLine()) != null)
                    {
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
                                Console.WriteLine($"Error parsing value: {value}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading file: {ex.Message}");
              
            }

            return data.ToArray();
        }

        public string ExtractID(string IDString)
        {

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

        public void OnDropCollections(string collection)
        {
            string saveDirectory = Path.GetDirectoryName(ConnectionManager.GetInstance().ConnectionString.Filename);
            string vtDirectory = Path.Combine(saveDirectory, "Vectorize", collection);
            string imgDirectory = Path.Combine(saveDirectory, "Images", collection);
            if (Directory.Exists(imgDirectory))
            {
                Directory.Delete(imgDirectory,true);  

            }

            if(Directory.Exists(vtDirectory)) 
            { 
                Directory.Delete(vtDirectory, true);  
            }
        }


        // delete image and vector
        public void DeleteWithID(string rawId, string collection)
        {
            string extractedID = ExtractID(rawId);

            string saveDirectory = Path.GetDirectoryName(ConnectionManager.GetInstance().ConnectionString.Filename);
            string vtDirectory = Path.Combine(saveDirectory, "Vectorize", collection);
            string imgDirectory = Path.Combine(saveDirectory, "Images", collection);
            if (Directory.Exists(imgDirectory))
            {
                Directory.Delete(Path.Combine(imgDirectory,extractedID), true);

            }

            if (Directory.Exists(vtDirectory))
            {
                Directory.Delete(Path.Combine(vtDirectory,extractedID), true);
            }
        }

        float CalculateAverageDistance(float[] array)
        {
            float totalDistance = 0.0f;
            int count = 0;

            for (int i = 0; i < array.Length; i++)
            {
                for (int j = i + 1; j < array.Length; j++)
                {
                    totalDistance += Math.Abs(array[i] - array[j]);
                    count++;
                }
            }

            if (count > 0)
            {
                return totalDistance / count;
            }
            else
            {
                return 0.0f; 
            }
        }

    }



}
