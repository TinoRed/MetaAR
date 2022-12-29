using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    private string user;
    private const string usernameKey = "username";
    
    [Tooltip("Text used to display validation status of the user")]
    [SerializeField]
    private GameObject _userStatus;

    [Tooltip("Input text for cached Usernames")]
    [SerializeField]
    private Text _inputField;

    void Start(){
        _userStatus.SetActive(false);
        if (PlayerPrefs.HasKey(usernameKey)){
            user = PlayerPrefs.GetString(usernameKey);
            _inputField.text = user;
        }
    }

    public void PlayGame()
    {

        Debug.Log("DEBUG PLAYGAME");
        if (string.IsNullOrEmpty(user))
        {
            Debug.Log("DEBUG PLAYGAME IF");
            _userStatus.SetActive(true);
            _userStatus.GetComponent<UnityEngine.UI.Text>().text = "Username not valid!";
        }
        else 
        {
            Debug.Log("DEBUG PLAYGAME ELSE");
            PlayerPrefs.SetString(usernameKey, user);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }
    }

    public void ReadStringInput(string inputUser)
    {
        user = inputUser;
        _userStatus.SetActive(false);
        Debug.Log("DEBUG USER: " + user);
    }
}
