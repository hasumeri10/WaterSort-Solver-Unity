using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    [SerializeField] private int _targetFrameRate = 60;
    [SerializeField] private float _fpsUpdateTime = 1;
    [SerializeField] private TextMeshProUGUI _fpsText;
    private int _numberOfLevels;
    private int _levelIndex;
    private Color[] _colors;
    public GameStates GameState;

    void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = _targetFrameRate;
        GameState = GameStates.Idle;
        StartCoroutine(UpdateFps());
        InitializeSettings();
    }

    void Update()
    {
        
    }

    private IEnumerator UpdateFps()
    {
        while (true)
        {
            yield return new WaitForSeconds(_fpsUpdateTime);
            _fpsText.text = $"FPS: {(int)(1f / Time.deltaTime)}";
        }
    }

    public void StartGame(int levelIndex, GameStates gameState)
    {
        _levelIndex = levelIndex;
        GameState = gameState;
        SceneManager.LoadScene("GameScene");
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    private void InitializeSettings()
    {
        string settingsString = File.ReadAllText(Application.streamingAssetsPath + $"/Levels/settings.json");
        JObject settingsObject = JObject.Parse(settingsString);
        _numberOfLevels = (int)settingsObject["number_of_levels"];
        int numberOfColors = (int)settingsObject["number_of_colors"];

        // Initialize _colors array base on settings file
        _colors = new Color[numberOfColors];
        for (int i = 0; i < numberOfColors; i++)
        {
            string[] colorString = ((string)settingsObject[$"color{i}"]).Split(",");
            _colors[i] = new Color(float.Parse(colorString[0]), float.Parse(colorString[1]), float.Parse(colorString[2]), float.Parse(colorString[3]));
        }
    }

    public int NumberOfLevels
    {
        get { return _numberOfLevels; }
    }

    public int LevelIndex
    {
        get { return _levelIndex; }
    }

    public Color[] Colors
    {
        get { return _colors; }
    }
}

public enum GameStates
{
    Idle,
    Playing,
    Solving,
    Over
}