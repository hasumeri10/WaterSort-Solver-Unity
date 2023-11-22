using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Utils;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private float _initialPositionX = -4;
    [SerializeField] private float _initialPositionY = 2;
    [SerializeField] private int _numberOfTubesPerRow = 5;
    [SerializeField] private GameObject _tubeContainer;
    [SerializeField] private GameObject _menuCanvas;
    [SerializeField] private GameObject _levelCanvas;
    [SerializeField] private GameObject _blockCanvas;
    [SerializeField] private GameObject _gameOverCanvas;
    private GameManager _gameManager;
    private int _numberOfTube;
    private int _unitPerTube;
    private int[] _initialState;
    private int[] _currentState;
    private int[] _newState;
    private List<int[]> _existedState;
    private List<Node> _path;
    private List<Tuple<int, int>> _pathIndex;
    private GameObject[] _tubeList;
    private GameObject _selectedTube;
    private bool _isTubeSelected = false;
    private int _sourceIndex;
    private int _destinationIndex;
    public bool IsCoroutineRunning = false;
    
    void Start()
    {
        _gameManager = GameManager.Instance.GetComponent<GameManager>();
        InitializeLevel();
        GenerateMenuButton();
    }

    void Update()
    {
        if (IsCoroutineRunning)
        {
            return;
        }
        if (_gameManager.GameState == GameStates.Solving)
        {
            HandleSolvingState();
        }
        if (_gameManager.GameState == GameStates.Playing && CheckIfStateIsGoal(_currentState))
        {
            HandleOverState();
        }
        
    }

    private void InitializeLevel()
    {
        // Read the config file
        string levelString = File.ReadAllText(Application.streamingAssetsPath + $"/Levels/tc_ws_{_gameManager.LevelIndex}.json");
        JObject levelObject = JObject.Parse(levelString);
        _numberOfTube = (int)levelObject["number_of_tubes"];
        _unitPerTube = (int)levelObject["unit_per_tube"];
        string[] initialStateString = levelObject["initial_state"].ToString().Replace("[", "").Replace("]", "").Split(",");
        _initialState = new int[_numberOfTube * _unitPerTube];
        for (int i = 0; i < _numberOfTube; i++)
        {
            for (int j = 0; j < _unitPerTube; j++)
            {
                _initialState[i * _unitPerTube + j] = int.Parse(initialStateString[i * _unitPerTube + j]);
            }
        }
        _currentState = new int[_numberOfTube * _unitPerTube];
        Array.Copy(_initialState, _currentState, _numberOfTube * _unitPerTube);

        CreateTubesAndWater();
    }

    private void CreateTubesAndWater()
    {
        _tubeList = new GameObject[_numberOfTube];
        for (int i = 0; i < _numberOfTube; i++)
        {
            float tubePositionX = _initialPositionX + i % _numberOfTubesPerRow * 2;
            float tubePositionY = _initialPositionY - i / _numberOfTubesPerRow * (_unitPerTube * 2 + 1);
            Vector2 tubePosition = new Vector2(tubePositionX, tubePositionY);
            GameObject tube = Instantiate(Resources.Load<GameObject>($"Prefabs/Tube"), tubePosition, Quaternion.identity);
            tube.GetComponent<SpriteRenderer>().size = new Vector2(tube.GetComponent<SpriteRenderer>().size.x, tube.GetComponent<SpriteRenderer>().size.y * _unitPerTube);
            tube.GetComponent<BoxCollider2D>().size = new Vector2(tube.GetComponent<BoxCollider2D>().size.x, tube.GetComponent<BoxCollider2D>().size.y * _unitPerTube);
            _tubeList[i] = tube;
            tube.name = $"Tube{i}";
            tube.transform.SetParent(_tubeContainer.transform);
            tube.GetComponent<Tube>().WaterList = new GameObject[_unitPerTube];
            for (int j = 0; j < _unitPerTube; j++)
            {
                float waterPositionX = tubePositionX;
                float waterPositionY = tubePositionY - (float)(_unitPerTube - 1) / 2 + j;
                Vector2 waterPosition = new Vector2(waterPositionX, waterPositionY);
                GameObject water = Instantiate(Resources.Load<GameObject>($"Prefabs/Water"), waterPosition, Quaternion.identity);
                water.GetComponent<SpriteRenderer>().color = _gameManager.Colors[_initialState[i * _unitPerTube + j]];
                tube.GetComponent<Tube>().WaterList[j] = water;
                water.name = $"Water{i}-{j}";
                water.transform.SetParent(tube.transform);
            }
        }
    }

    private void GenerateMenuButton()
    {
        GameObject homeButton = _menuCanvas.transform.Find("HomeButton").gameObject;
        homeButton.GetComponent<Button>().onClick.AddListener(() => {
            _gameManager.GameState = GameStates.Idle;
            SceneManager.LoadScene("MenuScene");
        } );

        GameObject levelButton = _menuCanvas.transform.Find("LevelButton").gameObject;
        GenerateLevels();
        levelButton.transform.Find("LevelIndex").GetComponent<TextMeshProUGUI>().text = _gameManager.LevelIndex.ToString();
        levelButton.GetComponent<Button>().onClick.AddListener(() => {
            _gameManager.GameState = GameStates.Idle;
            _levelCanvas.SetActive(true);
        } );

        GameObject restartButton = _menuCanvas.transform.Find("RestartButton").gameObject;
        restartButton.GetComponent<Button>().onClick.AddListener(() => _gameManager.StartGame(_gameManager.LevelIndex, GameStates.Playing));

        GameObject solveButton = _menuCanvas.transform.Find("SolveButton").gameObject;
        solveButton.GetComponent<Button>().onClick.AddListener(() => _gameManager.StartGame(_gameManager.LevelIndex, GameStates.Solving));

    }

    private void GenerateLevels()
    {
        for (int i = 1; i <= _gameManager.NumberOfLevels; i++)
        {
            GameObject levelIndexButton = (GameObject)Instantiate(Resources.Load("Prefabs/LevelButton"), Vector3.zero, Quaternion.identity);
            levelIndexButton.name = $"LevelButton{i}";
            levelIndexButton.transform.Find("LevelIndex").GetComponent<TextMeshProUGUI>().text = i.ToString();
            levelIndexButton.gameObject.transform.SetParent(_levelCanvas.transform.Find("Scroll View").Find("Viewport").Find("Content"));
            levelIndexButton.transform.localScale = Vector2.one;
            levelIndexButton.GetComponent<Button>().onClick.AddListener(() => _gameManager.StartGame(int.Parse(levelIndexButton.transform.Find("LevelIndex").GetComponent<TextMeshProUGUI>().text), GameStates.Playing));
        }
    }

    private void HandleSolvingState()
    {
        if (!_blockCanvas.activeSelf)
        {
            _blockCanvas.SetActive(true);
            SolveLevel();
        }
    }

    private void HandleOverState()
    {
        if (!_gameOverCanvas.activeSelf)
        {
            _gameOverCanvas.SetActive(true);
            
            GameObject previousButton = _gameOverCanvas.transform.Find("PreviousButton").gameObject;
            previousButton.GetComponent<Button>().onClick.AddListener(() => 
            {
                int levelIndex = _gameManager.LevelIndex > 1 ? _gameManager.LevelIndex - 1 : _gameManager.NumberOfLevels;
                _gameManager.StartGame(levelIndex, GameStates.Playing);
            } );

            GameObject restartButton = _gameOverCanvas.transform.Find("RestartButton").gameObject;
            restartButton.GetComponent<Button>().onClick.AddListener(() => _gameManager.StartGame(_gameManager.LevelIndex, GameStates.Playing));

            GameObject nextButton = _gameOverCanvas.transform.Find("NextButton").gameObject;
            nextButton.GetComponent<Button>().onClick.AddListener(() => 
            {
                int levelIndex = _gameManager.LevelIndex < _gameManager.NumberOfLevels ? _gameManager.LevelIndex + 1 : 1;
                _gameManager.StartGame(levelIndex, GameStates.Playing);
            } );
        }
    }

    private void SolveLevel()
    {
        _existedState = new List<int[]>();
        _path = new List<Node>();
        _pathIndex = new List<Tuple<int, int>>();
        _existedState.Add(_initialState);
        PriorityQueue<Node, int> priorityQueue = new PriorityQueue<Node, int>();
        priorityQueue.Enqueue(new Node(_currentState, null, 0), CalculateCost(_currentState));
        while (priorityQueue.Count > 0)
        {
            Node currentNode = priorityQueue.Dequeue();

            if (CheckIfStateIsGoal(currentNode.State))
            {
                _path.Add(currentNode);
                break;
            }

            List<Node> nextNodes = new List<Node>();

            for (int i = 0; i < _numberOfTube; i++)
            {
                for (int j = 0; j < _numberOfTube; j++)
                {
                    if (j != i)
                    {
                        nextNodes.Add(new Node(GetNewState(currentNode.State, i, j), currentNode, currentNode.Depth + 1));
                    }
                }
            }

            foreach(Node node in nextNodes)
            {
                if (!CheckIfStateIsEmpty(node.State) && !_existedState.Contains(node.State))
                {
                    priorityQueue.Enqueue(node, CalculateCost(node.State));
                    _existedState.Add(node.State);
                }
            }
        }

        if (_path.Count > 0)
        {
            Node temp = _path[0];
            while (temp.Depth > 0)
            {
                _path.Add(temp.Parent);
                temp = temp.Parent;
            }
        }
        _path.Reverse();

        int[] currentState = _path[0].State;
        int[] previousState;
        foreach(Node node in _path)
        {
            if (_path.IndexOf(node) > 0)
            {
                previousState = currentState;
                currentState = node.State;
                int sourceIndex = 0;
                int destinationIndex = 0;
                for (int i = 0; i < _numberOfTube; i++)
                {
                    if (GetSumOfColorIndex(currentState, i) < GetSumOfColorIndex(previousState, i))
                    {
                        sourceIndex = i;
                    }
                    else if (GetSumOfColorIndex(currentState, i) > GetSumOfColorIndex(previousState, i))
                    {
                        destinationIndex = i;
                    }
                }
                _pathIndex.Add(Tuple.Create(sourceIndex, destinationIndex));
            }
        }

        StartCoroutine(RunSolvingCoroutine());

    }

    private IEnumerator RunSolvingCoroutine()
    {
        while (true)
        {
            if (!IsCoroutineRunning)
            {
                if (_selectedTube != null)
                {
                    PourWater(_tubeList[_sourceIndex], _tubeList[_destinationIndex]);
                }
                else
                {
                    if (_pathIndex.Count > 0)
                    {
                        _sourceIndex = _pathIndex[0].Item1;
                        _destinationIndex = _pathIndex[0].Item2;
                        _pathIndex.RemoveAt(0);
                        SelectTube(_tubeList[_sourceIndex]);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            yield return new WaitForEndOfFrame();
        }
        _gameManager.GameState = GameStates.Playing;
    }

    public void SelectTube(GameObject tube)
    {
        _selectedTube = tube;
        _isTubeSelected = true;
        float targetPoitionX = _selectedTube.transform.position.x;
        float targetPositionY = _selectedTube.transform.position.y + 0.5f;
        Vector2 targetPosition = new Vector2(targetPoitionX, targetPositionY);
        _selectedTube.GetComponent<Tube>().MoveTube(targetPosition);
    }

    public void DeselectTube()
    {
        if (!IsCoroutineRunning)
        {
            _selectedTube.GetComponent<Tube>().MoveTubeBack();
        }
        _selectedTube = null;
        _isTubeSelected = false;
    }

    public void PourWater(GameObject source, GameObject destination)
    {
        _sourceIndex = GetTubeIndex(source);
        _destinationIndex = GetTubeIndex(destination);
        _newState = GetNewState(_currentState, _sourceIndex, _destinationIndex);
        if (!CheckIfStateIsEmpty(_newState))
        {
            float targetPositionX = _tubeList[_destinationIndex].transform.position.x + _tubeList[_destinationIndex].GetComponent<SpriteRenderer>().size.y / 2;
            float targetPositionY = _tubeList[_destinationIndex].transform.position.y + _tubeList[_destinationIndex].GetComponent<SpriteRenderer>().size.y + 0.5f;
            Vector2 targetPosition = new Vector2(targetPositionX, targetPositionY);
            _tubeList[_sourceIndex].GetComponent<Tube>().HandlePourAnimation(targetPosition);
        }
    }

    public void UpdateTube()
    {
        int sourceTopIndex = GetTopIndex(_currentState, _sourceIndex);
        int destinationTopIndex = GetTopIndex(_currentState, _destinationIndex);
        int topUnitCount = GetTopUnitCount(_currentState, _sourceIndex, sourceTopIndex);
        Color sourceTopColor = _tubeList[_sourceIndex].GetComponent<Tube>().WaterList[sourceTopIndex].GetComponent<SpriteRenderer>().color;
        Color emptyColor = _tubeList[_destinationIndex].GetComponent<Tube>().WaterList[destinationTopIndex + 1].GetComponent<SpriteRenderer>().color;
        for (int i = 0; i < _unitPerTube; i++)
        {
            if (i > sourceTopIndex - topUnitCount)
            {
                _tubeList[_sourceIndex].GetComponent<Tube>().WaterList[i].GetComponent<SpriteRenderer>().color = emptyColor;
            }
        }            
        for (int i = 0; i < _unitPerTube; i++)
        { 
            if (i > destinationTopIndex && i <= destinationTopIndex + topUnitCount)
            {
                _tubeList[_destinationIndex].GetComponent<Tube>().WaterList[i].GetComponent<SpriteRenderer>().color = sourceTopColor;
            }
        }
    }

    private int[] GetNewState(int[] state, int sourceIndex, int destinationIndex)
    {
        int[] newState = new int[_numberOfTube * _unitPerTube];     
        int sourceTopIndex = GetTopIndex(state, sourceIndex);
        int destinationTopIndex = GetTopIndex(state, destinationIndex);

        // Don't pour if the source tube is empty
        if (sourceTopIndex < 0)
        {
            return newState;
        }

        // Don't pour if the source tube is fully filled with only one color
        if (sourceTopIndex == _unitPerTube - 1)
        {
            int count = 1;
            for (int i = 1; i < _unitPerTube; i++)
            {
                if (state[sourceIndex * _unitPerTube + i] == state[sourceIndex * _unitPerTube + i - 1])
                {
                    count += 1;
                }
            }
            if (count == _unitPerTube)
            {
                return newState;
            }
        }

        // Don't pour if the top units of the source and the destination tubes have different colors
        if (destinationTopIndex >= 0 && state[sourceIndex * _unitPerTube + sourceTopIndex] != state[destinationIndex * _unitPerTube + destinationTopIndex])
        {
            return newState;
        }

        // Pour water if the conditions are satisfied
        int topUnitCount = GetTopUnitCount(state, sourceIndex, sourceTopIndex);    

        if (_unitPerTube - 1 - destinationTopIndex >= topUnitCount)
        {
            for (int i = 0; i < _numberOfTube; i++)
            {
                for (int j = 0; j < _unitPerTube; j++)
                {
                    if (i == sourceIndex)
                    {
                        if (j > sourceTopIndex - topUnitCount)
                        {
                            newState[i * _unitPerTube + j] = 0;
                        }
                        else
                        {
                            newState[i * _unitPerTube + j] = state[i * _unitPerTube + j];
                        }
                    }
                    else if (i == destinationIndex)
                    {
                        if (j > destinationTopIndex && j <= destinationTopIndex + topUnitCount)
                        {
                            newState[i * _unitPerTube + j] = state[sourceIndex * _unitPerTube + sourceTopIndex];
                        }
                        else
                        {
                            newState[i * _unitPerTube + j] = state[i * _unitPerTube + j];
                        }
                    }
                    else
                    {
                        newState[i * _unitPerTube + j] = state[i * _unitPerTube + j];
                    }
                }
            }
        }
        return newState;
    }

    private int GetTopIndex(int[] state, int index)
    {
        for (int i = 0; i < _unitPerTube; i++)
        {
            if (state[index * _unitPerTube + i] == 0)
            {
                return i - 1;
            }
        }

        return _unitPerTube - 1;
    }

    private int GetTopUnitCount(int[] state, int index, int topIndex)
    {
        int topUnitCount = 1;
        for (int i = topIndex - 1; i >= 0; i--)
        {
            if (state[index * _unitPerTube + i] == state[index * _unitPerTube + topIndex])
            {
                topUnitCount += 1;
            }
            else
            {
                break;
            }
        }
        return topUnitCount;
    }

    private int GetTubeIndex(GameObject tube)
    {
        for (int i = 0; i < _numberOfTube; i++)
        {
            if (_tubeList[i] == tube)
            {
                return i;
            }
        }
        return -1;
    }

    private bool CheckIfStateIsEmpty(int[] state)
    {
        int sum = 0;
        for (int i = 0; i < _numberOfTube; i++)
        {
            for (int j = 0; j < _unitPerTube; j++)
            {
                sum += state[i * _unitPerTube + j];
            }
        }
        if (sum == 0)
        {
            return true;
        }
        return false;
    }

    private bool CheckIfStateIsGoal(int[] state)
    {
        if (CheckIfStateIsEmpty(state))
        {
            return false;
        }
        for (int i = 0; i < _numberOfTube; i++)
        {
            for (int j = 1; j < _unitPerTube; j++)
            {
                if (state[i * _unitPerTube + j] != state[i * _unitPerTube + j - 1])
                {
                    return false;
                }
            }
        }
        return true;
    }

    private int CalculateCost(int[] state)
    {
        int g = 0;
        int h = 0;

        for (int i = 0; i < _numberOfTube; i++)
        {
            for (int j = 1; j < _unitPerTube; j++)
            {
                if (state[i * _unitPerTube + j] > 0 && state[i * _unitPerTube + j] == state[i * _unitPerTube + j - 1])
                {
                    g -= 1;
                }
                else
                {
                    break;
                }
            }
        }

        for (int i = 0; i < _numberOfTube; i++)
        {
            for (int j = 1; j < _unitPerTube; j++)
            {
                if (state[i * _unitPerTube + j] > 0 && state[i * _unitPerTube + j] != state[i * _unitPerTube + j - 1])
                {
                    h += 1;
                }
            }
        }

        return g + h;
    }

    private int GetSumOfColorIndex(int[] state, int tubeIndex)
    {
        int sum = 0;
        for (int i = 0; i < _unitPerTube; i++)
        {
            sum += state[tubeIndex * _unitPerTube + i];
        }
        return sum;
    }

    public GameObject SelectedTube
    {
        get { return _selectedTube; }
    }

    public bool IsTubeSelected
    {
        get { return _isTubeSelected; }
    }

    public int[] CurrentState
    {
        get { return _currentState; }
        set { _currentState = value; }
    }

    public int[] NewState
    {
        get { return _newState; }
    }
}


