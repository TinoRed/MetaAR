// Copyright 2022 Niantic, Inc. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine.Networking;
using System.Text;
using System.Threading.Tasks;
using Niantic.ARDK.AR;
using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.AR.HitTest;
using Niantic.ARDK.AR.WayspotAnchors;
using Niantic.ARDK.LocationService;
using Niantic.ARDK.Utilities.Input.Legacy;

using UnityEngine;
using UnityEngine.UI;

namespace Niantic.ARDKExamples.WayspotAnchors
{
  public class WayspotAnchorExampleManager: MonoBehaviour
  {
    // [Tooltip("The anchor that will be placed")]
    // [SerializeField]
    // private GameObject _anchorPrefab;

    [Tooltip("The anchors array that contains prefabs that will be placed")]
    [SerializeField]
    private GameObject[] _anchorPrefabArray;

    [SerializeField]
    private List<int> _prefabIndexesArray = new List<int>();

    [Tooltip("Camera used to place the anchors via raycasting")]
    [SerializeField]
    private Camera _camera;

    [Tooltip("Text used to display the current status of the demo")]
    [SerializeField]
    private Text _statusLog;

    [Tooltip("Text used to show the current localization state")]
    [SerializeField]
    private Text _localizationStatus;

    private WayspotAnchorService _wayspotAnchorService;
    private IARSession _arSession;
    private LocalizationState _localizationState;

    private readonly Dictionary<Guid, GameObject> _wayspotAnchorGameObjects =
      new Dictionary<Guid, GameObject>();

    private int _prefabIndex;
    private const string DataKey = "wayspot_anchor_payloads";
    private const string host = "http://192.168.1.146";
    private const string port = "5008";
    private const string post_endpoint = "mongopost";
    private const string get_endpoint = "mongoget";
    private bool _isFirstLoadDone = false; 
    [Serializable]
    private class MongoPayloads 
    {
      public string[] Payloads;
      public int[] prefabIndexes;
    }
    private void Awake()
    {
      // This is necessary for setting the user id associated with the current user. 
      // We strongly recommend generating and using User IDs. Accurate user information allows
      //  Niantic to support you in maintaining data privacy best practices and allows you to
      //  understand usage patterns of features among your users.  
      // ARDK has no strict format or length requirements for User IDs, although the User ID string
      //  must be a UTF8 string. We recommend avoiding using an ID that maps back directly to the
      //  user. So, for example, don’t use email addresses, or login IDs. Instead, you should
      //  generate a unique ID for each user. We recommend generating a GUID.
      // When the user logs out, clear ARDK's user id with ArdkGlobalConfig.ClearUserIdOnLogout

      //  Sample code:
      //  // GetCurrentUserId() is your code that gets a user ID string from your login service
      //  var userId = GetCurrentUserId(); 
      //  ArdkGlobalConfig.SetUserIdOnLogin(userId);

      _statusLog.text = "Initializing Session.";
      _prefabIndex = 0;
    }

    private void OnEnable()
    {
      ARSessionFactory.SessionInitialized += HandleSessionInitialized;
    }

    private void Update()
    {
      if (_wayspotAnchorService == null)
      {
        return;
      }
      if (_wayspotAnchorService.LocalizationState == LocalizationState.Failed)
      {
        _wayspotAnchorService.Restart();
      }
      //Get the pose where you tap on the screen
      var success = TryGetTouchInput(out Matrix4x4 localPose);
      if (_wayspotAnchorService.LocalizationState == LocalizationState.Localized)
      {
        // After succesful localization the Wayspot anchors are loaded
        // happens just once thanks to _isFirstLoadDone flag
        if (!_isFirstLoadDone)
        {
          LoadWayspotAnchors();
          _isFirstLoadDone = true;
        }
        if (success) //Check is screen tap was a valid tap
        {
          Ray raycast = Camera.main.ScreenPointToRay(Input.GetTouch(0).position);
          RaycastHit raycastHit;

          if (Physics.Raycast(raycast, out raycastHit))
          {
            foreach (var wayspotAnchorGameObject in _wayspotAnchorGameObjects)
            {
              if (raycastHit.collider.name.Equals("Anchor " + wayspotAnchorGameObject.Key.ToString())) {
                Debug.Log("Tap on: " + wayspotAnchorGameObject.Key.ToString());
                // DO SOMETHING
                _statusLog.text = "Complimenti hai cliccato " + wayspotAnchorGameObject.Value.tag;
                // float RotationSpeed = 2.0f;
                // wayspotAnchorGameObject.Value.transform.Rotate(Vector3.up * (RotationSpeed * Time.deltaTime));
                // wayspotAnchorGameObject.Value.transform.Rotate(00.0f, 360.0f, 0.0f, Space.Self);
                wayspotAnchorGameObject.Value.GetComponent<Animation>().Play();
                break;
              }
            }
          }  
        }
      }
      else
      {
        if (success) //Check is screen tap was a valid tap
        {
          _statusLog.text = "Must localize before playing.";
        }
        if(_localizationState != _wayspotAnchorService.LocalizationState)
        {
          _wayspotAnchorGameObjects.Values.ToList().ForEach(a => a.SetActive(false));
        }
      }
      _localizationStatus.text = _wayspotAnchorService.LocalizationState.ToString();
      _localizationState = _wayspotAnchorService.LocalizationState;
    }

    private void OnDisable()
    {
      ARSessionFactory.SessionInitialized -= HandleSessionInitialized;
    }

    private void OnDestroy()
    {
      _wayspotAnchorService.Dispose();
    }

    /// Saves all of the existing wayspot anchors
    public void SaveWayspotAnchors()
    {
      if (_wayspotAnchorGameObjects.Count > 0)
      {
        var wayspotAnchors = _wayspotAnchorService.GetAllWayspotAnchors();
        var payloads = wayspotAnchors.Select(a => a.Payload);
        SaveLocalPayloadsToMongo(payloads.ToArray());
      }
      else
      {
        SaveLocalPayloadsToMongo(Array.Empty<WayspotAnchorPayload>());
      }

      _statusLog.text = "Saved Wayspot Anchors.";
    }

    /// Loads all of the saved wayspot anchors
    public void LoadWayspotAnchors()
    {
      if (_wayspotAnchorService.LocalizationState != LocalizationState.Localized)
      {
        _statusLog.text = "Must localize before loading anchors.";
        return;
      }
      _statusLog.text = "Loading Wayspot Anchors from MongoDB...";
      
      LoadLocalPayloadsFromMongo();
    }

    /// Loads all of the saved wayspot anchors
    public void ChangePrefab()
    {
      _prefabIndex = ++ _prefabIndex % _anchorPrefabArray.Length;
      _statusLog.text = "Hai selezionato: " + _anchorPrefabArray[_prefabIndex].name;
    }

    /// Clears all of the active wayspot anchors
    public void ClearAnchorGameObjects()
    {
      if (_wayspotAnchorGameObjects.Count == 0)
      {
        _statusLog.text = "No anchors to clear.";
        return;
      }

      foreach (var anchor in _wayspotAnchorGameObjects)
      {
        Destroy(anchor.Value);
      }

      _wayspotAnchorService.DestroyWayspotAnchors(_wayspotAnchorGameObjects.Keys.ToArray());
      _wayspotAnchorGameObjects.Clear();
      _prefabIndexesArray.Clear();
      _statusLog.text = "Cleared Wayspot Anchors.";
    }

    IEnumerator MongoPostRequest(WayspotAnchorPayload[] wayspotAnchorPayloads)
    {
      var wayspotAnchorsData = new WayspotAnchorsData();
      wayspotAnchorsData.Payloads = wayspotAnchorPayloads.Select(a => a.Serialize()).ToArray();
      wayspotAnchorsData.prefabIndexes = _prefabIndexesArray.ToArray();
      string wayspotAnchorsJson = JsonUtility.ToJson(wayspotAnchorsData);
      // remove the closing bracket to append more fields
      // wayspotAnchorsJson = wayspotAnchorsJson.Remove(wayspotAnchorsJson.Length-1);
      // wayspotAnchorsJson += ",\"dataKey\": \"wayspot_anchor_payloads\"}";
      Debug.Log("TEST MongoPostRequest - " + wayspotAnchorsJson);
      PlayerPrefs.SetString(DataKey, wayspotAnchorsJson);
      
      // Saving through MongoDB
      
      string url_post = host + ":" + port + "/" + post_endpoint;
      var post_request = new UnityWebRequest(url_post, "POST");
      byte[] bodyRaw = Encoding.UTF8.GetBytes(wayspotAnchorsJson);
      post_request.uploadHandler = (UploadHandler) new UploadHandlerRaw(bodyRaw);
      post_request.downloadHandler = (DownloadHandler) new DownloadHandlerBuffer();
      post_request.SetRequestHeader("Content-Type", "application/json");
      yield return post_request.SendWebRequest();
    }

    IEnumerator MongoGetRequest()
    {
      string url_get = host + ":" + port + "/" + get_endpoint;
      UnityWebRequest get_request = UnityWebRequest.Get(url_get);
      yield return get_request.SendWebRequest();
      if (get_request.result != UnityWebRequest.Result.Success) {
        Debug.Log(get_request.error);
      }
      else {
        // se la risposta non è vuota, seleziona i payload
        var payloads = new List<WayspotAnchorPayload>();
        var jsonText = get_request.downloadHandler.text;
        MongoPayloads mongo_payload = JsonUtility.FromJson<MongoPayloads>(jsonText);
        foreach (var wayspotAnchorPayload in mongo_payload.Payloads)
        {
          var payload = WayspotAnchorPayload.Deserialize(wayspotAnchorPayload);
          payloads.Add(payload);
        }

        var testMongoPayloads = payloads.ToArray();
        Debug.Log("TEST - MongoGetRequest: mongoPayLoads Length = " + testMongoPayloads.Length);
        if (testMongoPayloads.Length > 0)
          {
            var wayspotAnchors = _wayspotAnchorService.RestoreWayspotAnchors(testMongoPayloads);
            CreateAnchorGameObjectsFromMongoLoad(wayspotAnchors, mongo_payload.prefabIndexes);
            _statusLog.text = "Loaded Wayspot Anchors.";
          }
          else
          {
            _statusLog.text = "No anchors to load.";
          }
          }
    }

    public void SaveLocalPayloadsToMongo(WayspotAnchorPayload[] wayspotAnchorPayloads)
    {
      StartCoroutine(MongoPostRequest(wayspotAnchorPayloads));
    }
    
    public void LoadLocalPayloadsFromMongo()
    {
      StartCoroutine(MongoGetRequest());
    }

    /// Pauses the AR Session
    public void PauseARSession()
    {
      if (_arSession.State == ARSessionState.Running)
      {
        _arSession.Pause();
        _statusLog.text = $"AR Session Paused.";
      }
      else
      {
        _statusLog.text = $"Cannot pause AR Session.";
      }
    }

    /// Resumes the AR Session
    public void ResumeARSession()
    {
      if (_arSession.State == ARSessionState.Paused)
      {
        _arSession.Run(_arSession.Configuration);
        _statusLog.text = $"AR Session Resumed.";
      }
      else
      {
        _statusLog.text = $"Cannot resume AR Session.";
      }
    }

    /// Restarts Wayspot Anchor Service
    public void RestartWayspotAnchorService()
    {
      _wayspotAnchorService.Restart();
    }

    private void HandleSessionInitialized(AnyARSessionInitializedArgs anyARSessionInitializedArgs)
    {
      _statusLog.text = "Running Session...";
      _arSession = anyARSessionInitializedArgs.Session;
      _arSession.Ran += HandleSessionRan;
    }

    private void HandleSessionRan(ARSessionRanArgs arSessionRanArgs)
    {
      _arSession.Ran -= HandleSessionRan;
      _wayspotAnchorService = CreateWayspotAnchorService();
      _statusLog.text = "Session Initialized.";
    }

    private void PlaceAnchor(Matrix4x4 localPose)
    {
      _wayspotAnchorService.CreateWayspotAnchors(CreateAnchorGameObjects, localPose);
      // Alternatively, you can make this method async and create wayspot anchors using await:
      // var wayspotAnchors = await _wayspotAnchorService.CreateWayspotAnchorsAsync(localPose);
      // CreateAnchorGameObjects(wayspotAnchors);

      // _statusLog.text = "Anchor placed.";
    }

    private WayspotAnchorService CreateWayspotAnchorService()
    {
      var wayspotAnchorsConfiguration = WayspotAnchorsConfigurationFactory.Create();
      
      var locationService = LocationServiceFactory.Create(_arSession.RuntimeEnvironment);
      locationService.Start();

      var wayspotAnchorService = new WayspotAnchorService(_arSession, locationService, wayspotAnchorsConfiguration);
      return wayspotAnchorService;
    }


    private void CreateAnchorGameObjectsFromMongoLoad(IWayspotAnchor[] wayspotAnchors, int[] prefabMongoIndexes)
    {
      var i = 0;
      foreach (var wayspotAnchor in wayspotAnchors)
      {
        if (_wayspotAnchorGameObjects.ContainsKey(wayspotAnchor.ID))
        {
          continue;
        }
        wayspotAnchor.TrackingStateUpdated += HandleWayspotAnchorTrackingUpdated;
        var id = wayspotAnchor.ID;
        var anchor = Instantiate(_anchorPrefabArray[prefabMongoIndexes[i]]);
        _prefabIndexesArray.Add(prefabMongoIndexes[i]);
        anchor.SetActive(false);
        anchor.name = $"Anchor {id}";
        _wayspotAnchorGameObjects.Add(id, anchor);
        i++;
      }
    }
    private void CreateAnchorGameObjects(IWayspotAnchor[] wayspotAnchors)
    {
      foreach (var wayspotAnchor in wayspotAnchors)
      {
        if (_wayspotAnchorGameObjects.ContainsKey(wayspotAnchor.ID))
        {
          continue;
        }
        wayspotAnchor.TrackingStateUpdated += HandleWayspotAnchorTrackingUpdated;
        var id = wayspotAnchor.ID;
        var anchor = Instantiate(_anchorPrefabArray[_prefabIndex]);
        _prefabIndexesArray.Add(_prefabIndex);
        anchor.SetActive(false);
        anchor.name = $"Anchor {id}";
        _wayspotAnchorGameObjects.Add(id, anchor);
      }
    }

    private void HandleWayspotAnchorTrackingUpdated(WayspotAnchorResolvedArgs wayspotAnchorResolvedArgs)
    {
      var anchor = _wayspotAnchorGameObjects[wayspotAnchorResolvedArgs.ID].transform;
      anchor.position = wayspotAnchorResolvedArgs.Position;
      anchor.rotation = wayspotAnchorResolvedArgs.Rotation;
      anchor.gameObject.SetActive(true);
    }

    private bool TryGetTouchInput(out Matrix4x4 localPose)
    {
      if (_arSession == null || PlatformAgnosticInput.touchCount <= 0)
      {
        localPose = Matrix4x4.zero;
        return false;
      }

      var touch = PlatformAgnosticInput.GetTouch(0);
      if (touch.IsTouchOverUIObject())
      {
        localPose = Matrix4x4.zero;
        return false;
      }

      if (touch.phase != TouchPhase.Began)
      {
        localPose = Matrix4x4.zero;
        return false;
      }

      var currentFrame = _arSession.CurrentFrame;
      if (currentFrame == null)
      {
        localPose = Matrix4x4.zero;
        return false;
      }

      var results = currentFrame.HitTest
      (
        _camera.pixelWidth,
        _camera.pixelHeight,
        touch.position,
        ARHitTestResultType.ExistingPlane
      );

      int count = results.Count;
      if (count <= 0)
      {
        localPose = Matrix4x4.zero;
        return false;
      }

      var result = results[0];
      localPose = result.WorldTransform;
      return true;
    }
  }
  [Serializable]
    public class WayspotAnchorsData
    {
      /// The payloads to save via JsonUtility
      public string[] Payloads = Array.Empty<string>();
      public string dataKey = "wayspot_anchor_payloads";
      public int[] prefabIndexes = Array.Empty<int>();
    }
}
