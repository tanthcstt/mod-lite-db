﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LiteDB.Engine;
using static LiteDB.Constants;

namespace LiteDB
{
    internal partial class SqlParser
    {
        /// <summary>
        /// INSERT INTO {collection} VALUES {doc0} [, {docN}] [ WITH ID={type} ] ]
        /// </summary>
        private BsonDataReader ParseInsert()
        {
            _tokenizer.ReadToken().Expect("INSERT");
            _tokenizer.ReadToken().Expect("INTO");

            var collection = _tokenizer.ReadToken().Expect(TokenType.Word).Value;

            var autoId = this.ParseWithAutoId();

            _tokenizer.ReadToken().Expect("VALUES");

            // get list of documents (return an IEnumerable)
            // will validate EOF or ;
            var docs = this.ParseListOfDocuments();

            var result = _engine.Insert(collection, docs, autoId);
            Console.WriteLine(result);  

            return new BsonDataReader(result);
        }

        private BsonDataReader ParseInsertImage()
        {
            _tokenizer.ReadToken().Expect("INSERT_IMG");
            _tokenizer.ReadToken().Expect("INTO");

            var collection = _tokenizer.ReadToken().Expect(TokenType.Word).Value;

            var autoId = this.ParseWithAutoId();

            _tokenizer.ReadToken().Expect("VALUES");

            // get list of documents (return an IEnumerable)
            // will validate EOF or ;
            var docs = this.ParseListOfDocuments();

      
            var result = _engine.Insert(collection, docs, autoId);


         /*   string pattern = @"Image\((.*?)\)";

            if (result == 1)
            {
                IEnumerator<BsonDocument> enumerator = docs.GetEnumerator();
                bool read = enumerator.Current;
                List<BsonDocument> listDoc = new List<BsonDocument>();
                string path = "";
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
                            path = imageString;
                            break;
                        }

                    }
                }

            }*/

            return new BsonDataReader(result);
        }

        /// <summary>
        /// Parse :[type] for AutoId (just after collection name)
        /// </summary>
        private BsonAutoId ParseWithAutoId()
        {
            var with = _tokenizer.LookAhead();

            if (with.Type == TokenType.Colon)
            {
                _tokenizer.ReadToken();

                var type = _tokenizer.ReadToken().Expect(TokenType.Word);

                switch(type.Value.ToUpper())
                {
                    case "GUID": return BsonAutoId.Guid;
                    case "INT": return BsonAutoId.Int32;
                    case "LONG": return BsonAutoId.Int64;
                    case "OBJECTID": return BsonAutoId.ObjectId;
                    default: throw LiteException.UnexpectedToken(type, "DATE, GUID, INT, LONG, OBJECTID");
                }
            }

            return BsonAutoId.ObjectId;
        }
    }
}