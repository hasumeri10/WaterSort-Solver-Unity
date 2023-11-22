using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    private GameObject _menuCanvas;
    private GameManager _gameManager;

    void Start()
    {
        _menuCanvas = GameObject.Find("MenuCanvas");
        _gameManager = GameManager.Instance.GetComponent<GameManager>();
        GameObject playButton = _menuCanvas.transform.Find("PlayButton").gameObject;
        playButton.GetComponent<Button>().onClick.AddListener(() => _gameManager.StartGame(1, GameStates.Playing));
        GameObject quitButton = _menuCanvas.transform.Find("QuitButton").gameObject;
        quitButton.GetComponent<Button>().onClick.AddListener(() => _gameManager.QuitGame());
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
