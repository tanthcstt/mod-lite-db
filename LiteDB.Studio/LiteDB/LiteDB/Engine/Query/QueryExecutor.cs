
using Microsoft.ML.OnnxRuntime;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using static LiteDB.Constants;
using Microsoft.ML.AutoML;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Drawing.Imaging;
using System.Drawing;
using System.Runtime.InteropServices;

namespace LiteDB.Engine
{
    /// <summary>
    /// Class that execute QueryPlan returing results
    /// </summary>
    internal class QueryExecutor
    {
        private readonly LiteEngine _engine;
        private readonly EngineState _state;
        private readonly TransactionMonitor _monitor;
        private readonly SortDisk _sortDisk;
        private readonly DiskService _disk;
        private readonly EnginePragmas _pragmas;
        private readonly CursorInfo _cursor;
        private readonly string _collection;
        private readonly Query _query;
        private readonly IEnumerable<BsonDocument> _source;

        public QueryExecutor(
            LiteEngine engine, 
            EngineState state,
            TransactionMonitor monitor, 
            SortDisk sortDisk, 
            DiskService disk,
            EnginePragmas pragmas, 
            string collection, 
            Query query, 
            IEnumerable<BsonDocument> source)
        {
            _engine = engine;
            _state = state;
            _monitor = monitor;
            _sortDisk = sortDisk;
            _disk = disk;
            _pragmas = pragmas;
            _collection = collection;
            _query = query;

            _cursor = new CursorInfo(collection, query);

            LOG(_query.ToSQL(_collection).Replace(Environment.NewLine, " "), "QUERY");

            // source will be != null when query will run over external data source, like system collections or files (not user collection)
            _source = source;
        }

        public BsonDataReader ExecuteQuery()
        {
            if (_query.Into == null)
            {
                return this.ExecuteQuery(_query.ExplainPlan);
            }
            else
            {
                return this.ExecuteQueryInto(_query.Into, _query.IntoAutoId);
            }
        }

        /// <summary>
        /// Run query definition into engine. Execute optimization to get query planner
        /// </summary>
        /// 
       
        internal BsonDataReader ExecuteQuery(bool executionPlan)
        {
            var transaction = _monitor.GetTransaction(true, true, out var isNew);

            transaction.OpenCursors.Add(_cursor);
            var result = RunQuery();

            if (_query.ImageDescription != "")
            {
                IEnumerator<BsonDocument> enumerator = result.GetEnumerator();
                bool read = enumerator.MoveNext();
                Console.WriteLine(enumerator);
                List<BsonDocument> listDoc = new List<BsonDocument>();

                string pattern = @"Image\((.*?)\)";

                // all value - path
                Dictionary<BsonDocument, string> dic = new Dictionary<BsonDocument, string>();
                List<string> listPath = new List<string>(); 

                while (read)
                {
                    listDoc.Add(enumerator.Current);
                    read = enumerator.MoveNext();
                }


                foreach (var item in listDoc)
                {
                    foreach (var doc in item)
                    {

                        Match match = Regex.Match(doc.Value.ToString(), pattern);
                        if (match.Success)
                        {
                            string imageString = match.Groups[1].Value;
                            listPath.Add(imageString);
                            dic.Add(item, imageString);
                        }

                    }
                }

                var modelPath = @"D:\Inceptionv3.onnx";
                var imagePath = @"D:\download.jpg";
             

                // Load the model
                InferenceSession session = new InferenceSession(modelPath);
                // var imageData = Image.Load(imagePath);
                byte[] imageData = File.ReadAllBytes(imagePath);
                using (var image = Image.FromStream(new MemoryStream(imageData)))
                {
                    // Resize image manually (assuming model needs 299x299)
                    var resizedImage = new Bitmap(image, 299, 299);

                    // Convert to desired format (assuming model needs BGR)
                    var bgrImage = new Bitmap(resizedImage.Width, resizedImage.Height, PixelFormat.Format24bppRgb);
                    using (var graphics = Graphics.FromImage(bgrImage))
                    {
                        graphics.DrawImage(resizedImage, 0, 0);
                    }

                    // Lock bits and extract pixel data
                    var bgrData = bgrImage.LockBits(new Rectangle(0, 0, bgrImage.Width, bgrImage.Height),
                                                     ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                    // Calculate the size of imageBytes based on stride and height
                    int imageSize = Math.Abs(bgrData.Stride) * bgrImage.Height;
                    byte[] imageBytes = new byte[imageSize];

                    Marshal.Copy(bgrData.Scan0, imageBytes, 0, imageBytes.Length);

                    // Unlock bits
                    bgrImage.UnlockBits(bgrData);

                    // Create input tensor
                    var inputMeta = session.InputMetadata["image"];  // Assuming the input name is "input_tensor"
                    int tensorLength = 1 * 3 * 299 * 299;
                    float[] normalizedImageBytes = new float[tensorLength];
                    for (int i = 0; i < tensorLength; i++)
                    {
                        normalizedImageBytes[i] = 0f;
                    }
                    // Loop through each pixel and normalize BGR values to the expected range
                    // (assuming model expects values between 0 and 1)
                    for (int i = 0; i < imageBytes.Length; i += 3)
                    {
                        int pixelIndex = i / 3;
                        if (pixelIndex < normalizedImageBytes.Length / 3) // Divide by 3 to match the number of channels
                        {
                            // Normalize Blue, Green, and Red channels (adjust order if format is different)
                                normalizedImageBytes[pixelIndex * 3] = (float)imageBytes[i + 2] / 255.0f; // Blue
                          
                            if (i + 1 < normalizedImageBytes.Length)
                            {

                                normalizedImageBytes[pixelIndex * 3 + 1] = (float)imageBytes[i + 1] / 255.0f; // Green
                            }
                            if (i + 2 < normalizedImageBytes.Length)
                            {
                                normalizedImageBytes[pixelIndex * 3 + 2] = (float)imageBytes[i] / 255.0f;   // Red

                            }
                        }
                    }
                    Float16[] float16Array = new Float16[normalizedImageBytes.Length];
                    for (int i = 0; i < float16Array.Length; i++)
                    {
                        float16Array[i] = (Float16)normalizedImageBytes[i];
                    }

                    // Create the DenseTensor with normalized BGR data
                    var tensor = new DenseTensor<Float16>(float16Array, new int[] { 1, 3, 299, 299 });
                    var inputNames = session.InputMetadata.Keys.ToList();
                    // ... rest of the code for running inference and extracting features (same as previous example)
                    var namedInput = NamedOnnxValue.CreateFromTensor<Float16>(inputNames.FirstOrDefault(), tensor);

                    var results = session.Run(new List<NamedOnnxValue>() { { namedInput } });
                    var featuresTensor = results.FirstOrDefault(r => r.Name.StartsWith("output_features")); // Handle potential multiple feature outputs
                  
                    float[] features = featuresTensor.AsTensor<float>().ToList().ToArray();

                  
                  

                    var softmaxOutputTensor = results.FirstOrDefault(r => r.Name.StartsWith("softmax_output"));
                    if (softmaxOutputTensor == null)
                    {
                        throw new Exception("Failed to find expected softmax output tensor for label extraction.");
                    }

                    float[] softmaxProbabilities = softmaxOutputTensor.AsTensor<float>().ToList().ToArray();
                    int topIndex = softmaxProbabilities.ToList().IndexOf(softmaxProbabilities.Max()); // Get index of highest probability
                    string predictedLabel = $"Predicted label: {topIndex}"; // Assuming labels are indexed 0, 1, 2, ...
                    Console.Write(predictedLabel);

                }




                return new BsonDataReader(listDoc, _collection, _state);

            } else
            {
                return new BsonDataReader(RunQuery(), _collection, _state);
            }

            IEnumerable<BsonDocument> RunQuery()
            {
                var snapshot = transaction.CreateSnapshot(_query.ForUpdate ? LockMode.Write : LockMode.Read, _collection, false);

                // no collection, no documents
                if (snapshot.CollectionPage == null && _source == null)
                {
                    // if query use Source (*) need runs with empty data source
                    if (_query.Select.UseSource)
                    {
                        yield return _query.Select.ExecuteScalar(_pragmas.Collation).AsDocument;
                    }

                    transaction.OpenCursors.Remove(_cursor);

                    if (isNew)
                    {
                        _monitor.ReleaseTransaction(transaction);
                    }

                    yield break;
                }

                // execute optimization before run query (will fill missing _query properties instance)
                var optimizer = new QueryOptimization(snapshot, _query, _source, _pragmas.Collation);

                var queryPlan = optimizer.ProcessQuery();

                var plan = queryPlan.GetExecutionPlan();

                // if execution is just to get explan plan, return as single document result
                if (executionPlan)
                {
                    yield return queryPlan.GetExecutionPlan();

                    transaction.OpenCursors.Remove(_cursor);

                    if (isNew)
                    {
                        _monitor.ReleaseTransaction(transaction);
                    }

                    yield break;
                }

                // get node list from query - distinct by dataBlock (avoid duplicate)
                var nodes = queryPlan.Index.Run(snapshot.CollectionPage, new IndexService(snapshot, _pragmas.Collation, _disk.MAX_ITEMS_COUNT));

                // get current query pipe: normal or groupby pipe
                var pipe = queryPlan.GetPipe(transaction, snapshot, _sortDisk, _pragmas, _disk.MAX_ITEMS_COUNT);

                // start cursor elapsed timer
                _cursor.Elapsed.Start();

                using (var enumerator = pipe.Pipe(nodes, queryPlan).GetEnumerator())
                {
                    var read = false;

                    try
                    {
                        read = enumerator.MoveNext();
                    }
                    catch (Exception ex)
                    {
                        _state.Handle(ex);
                        throw ex;
                    }

                    while (read)
                    {
                        _cursor.Fetched++;
                        _cursor.Elapsed.Stop();

                        yield return enumerator.Current;

                        if (transaction.State != TransactionState.Active) throw new LiteException(0, $"There is no more active transaction for this cursor: {_cursor.Query.ToSQL(_cursor.Collection)}");

                        _cursor.Elapsed.Start();

                        try
                        {
                            read = enumerator.MoveNext();
                        }
                        catch (Exception ex)
                        {
                            _state.Handle(ex);
                            throw ex;
                        }
                    }
                }

                // stop cursor elapsed
                _cursor.Elapsed.Stop();

                transaction.OpenCursors.Remove(_cursor);

                if (isNew)
                {
                    _monitor.ReleaseTransaction(transaction);
                }
            };
        }

        /// <summary>
        /// Execute query and insert result into another collection. Support external collections
        /// </summary>
        internal BsonDataReader ExecuteQueryInto(string into, BsonAutoId autoId)
        {
            IEnumerable<BsonDocument> GetResultset()
            {
                using (var reader = this.ExecuteQuery(false))
                {
                    while (reader.Read())
                    {
                        yield return reader.Current.AsDocument;
                    }
                }
            }

            int result;

            // if collection starts with $ it's system collection
            if (into.StartsWith("$"))
            {
                SqlParser.ParseCollection(new Tokenizer(into), out var name, out var options);

                var sys = _engine.GetSystemCollection(name);

                result = sys.Output(GetResultset(), options);
            }
            // otherwise insert as normal collection
            else
            {
                result = _engine.Insert(into, GetResultset(), autoId);
            }

            return new BsonDataReader(result);
        }
    }
}