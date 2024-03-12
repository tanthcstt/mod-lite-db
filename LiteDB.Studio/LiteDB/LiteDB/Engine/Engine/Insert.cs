﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Insert all documents in collection. If document has no _id, use AutoId generation.
        /// </summary>
        public int Insert(string collection, IEnumerable<BsonDocument> docs, BsonAutoId autoId)
        {
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(collection));
            if (docs == null) throw new ArgumentNullException(nameof(docs));

            return this.AutoTransaction(transaction =>
            {
                var snapshot = transaction.CreateSnapshot(LockMode.Write, collection, true);
                var count = 0;
                var indexer = new IndexService(snapshot, _header.Pragmas.Collation, _disk.MAX_ITEMS_COUNT);
                var data = new DataService(snapshot, _disk.MAX_ITEMS_COUNT);

                LOG($"insert `{collection}`", "COMMAND");

                foreach (var doc in docs)
                {
                    //save image here

                    _state.Validate();

                    transaction.Safepoint();

                    this.InsertDocument(snapshot, doc, autoId, indexer, data);
                    
                    var x = IsImage(doc, OnInsertImage);   
                    Console.WriteLine(x);
                    count++;
                }

                return count;
            });
        }

        /// <summary>
        /// Internal implementation of insert a document
        /// </summary>
        private void InsertDocument(Snapshot snapshot, BsonDocument doc, BsonAutoId autoId, IndexService indexer, DataService data)
        {
            // if no _id, use AutoId
            if (!doc.TryGetValue("_id", out var id))
            {
                doc["_id"] = id =
                    autoId == BsonAutoId.ObjectId ? new BsonValue(ObjectId.NewObjectId()) :
                    autoId == BsonAutoId.Guid ? new BsonValue(Guid.NewGuid()) :
                    this.GetSequence(snapshot, autoId);
            }
            else if(id.IsNumber)
            {
                // update memory sequence of numeric _id
                this.SetSequence(snapshot, id);
            }

            // test if _id is a valid type
            if (id.IsNull || id.IsMinValue || id.IsMaxValue)
            {
                throw LiteException.InvalidDataType("_id", id);
            }

            // storage in data pages - returns dataBlock address
            var dataBlock = data.Insert(doc);

            IndexNode last = null;

            // for each index, insert new IndexNode
            foreach (var index in snapshot.CollectionPage.GetCollectionIndexes())
            {
                // for each index, get all keys (supports multi-key) - gets distinct values only
                // if index are unique, get single key only
                var keys = index.BsonExpr.GetIndexKeys(doc, _header.Pragmas.Collation);

                // do a loop with all keys (multi-key supported)
                foreach(var key in keys)
                {
                    // insert node
                    var node = indexer.AddNode(index, key, dataBlock, last);

                    last = node;
                }
            }
        }
        /// <summary>
        /// test insert image, save image to path , TODO: SAVE IMG BY BSON
        /// </summary>
        /// <param name="path"></param>
        private void OnInsertImage(string path)
        {
       
            string saveDirectory =Path.GetDirectoryName(ConnectionManager.GetInstance().ConnectionString.Filename);
            saveDirectory = Path.Combine(saveDirectory, "Images");
            byte[] imageBytes = File.ReadAllBytes(path);
            File.WriteAllBytes(Path.Combine(saveDirectory, Path.GetFileName(path)), imageBytes);


        }

        private bool IsImage(BsonDocument doc, Action<string> callback = null)
        {
            string pattern = @"Image\((.*?)\)";
            foreach (BsonValue value in doc.Values)
            {
                if (value.Type == BsonType.String)
                {
                    var docString = value.AsString;
                    Match match = Regex.Match(docString, pattern);
                    if (match.Success)
                    {
                        // Extract the path
                        string path = match.Groups[1].Value;
                        callback?.Invoke(path); 
                        return true;
                    }
                }
            }
           

            return false;    
        }

      
    }
}