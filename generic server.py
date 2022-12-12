import json

from flask import Flask, Response, jsonify, request
import pymongo

app = Flask(__name__)

user = "agostino"
password = ""

@app.route('/test', methods=['GET'])
def test():
    print("abc")
    return jsonify(
                    {"severity": "danger"}
                )

@app.route('/mongoget', methods=['GET'])
def mongoget():
    client = pymongo.MongoClient("mongodb+srv://{}:{}@cluster0.vhjvg.mongodb.net/?retryWrites=true&w=majority".format(user, password))
    db = client.test
    collection  = db.test
    try:
        doc = collection.find_one({"dataKey": "wayspot_anchor_payloads"})
        response = {"Payloads":doc["Payloads"]}
    except:
        response = {"Payloads": []}
    return jsonify(response)


@app.route('/mongopost', methods=['POST'])
def mongopost():
    body = request.get_json()
    client = pymongo.MongoClient("mongodb+srv://{}:{}@cluster0.vhjvg.mongodb.net/?retryWrites=true&w=majority".format(user, password))
    db = client.test
    collection  = db.test
    doc = collection.insert_one(body)
    return jsonify({"status":200})


if __name__ == "__main__":
    server_host = '0.0.0.0'
    server_port = '5008'
    app.run(host=server_host, port=server_port, debug=True)
