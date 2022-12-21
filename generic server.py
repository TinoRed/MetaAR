import json

from flask import Flask, Response, jsonify, request
import pymongo

app = Flask(__name__)

user = "agostino"
password = ""

@app.route('/coffee', methods=['GET'])
def test():
    print("test")
    return jsonify({"code": 200})

@app.route('/mongoget', methods=['GET'])
def mongoget():
    client = pymongo.MongoClient("mongodb+srv://{}:{}@cluster0.vhjvg.mongodb.net/?retryWrites=true&w=majority".format(user, password))
    db = client.test
    collection  = db.test
    try:
        doc = collection.find_one({"dataKey": "wayspot_anchor_payloads"})
        response = {"Payloads":doc["Payloads"]}
    except Exception as e:
        response = {"Exception": e}
    return jsonify(response)


@app.route('/mongopost', methods=['POST'])
def mongopost():
    body = request.get_json()
    client = pymongo.MongoClient("mongodb+srv://{}:{}@cluster0.vhjvg.mongodb.net/?retryWrites=true&w=majority".format(user, password))
    db = client.test
    collection  = db.test
    try:
        response = collection.find_one_and_update({'dataKey': body["dataKey"]}, {'$set': {'Payloads': body["Payloads"]}},
                               upsert=True)
    except Exception as e:
        response = {"Exception": e}
    return jsonify(response)


if __name__ == "__main__":
    server_host = '0.0.0.0'
    server_port = '5008'
    app.run(host=server_host, port=server_port, debug=True)
