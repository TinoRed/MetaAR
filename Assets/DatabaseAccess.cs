using MongoDB.Driver;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MongoDB.Bson;

public class DatabaseAccess : MonoBehaviour
{
    // Start is called before the first frame update
    private const string MONGO_URI = "mongodb+srv://agost:<password>@cluster0.vhjvg.mongodb.net/?retryWrites=true&w=majority";
    private const string DATABASE_NAME = "test";
    private MongoClient client;
    private IMongoDatabase db;
    private IMongoCollection<BsonDocument> collection;

    void Start()
    {
        client = new MongoClient(MONGO_URI);
        db = client.GetDatabase(DATABASE_NAME);
        collection = db.GetCollection<BsonDocument>("test");
        //Test
        var document = new BsonDocument {{"username",100}};
        collection.InsertOne(document);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
