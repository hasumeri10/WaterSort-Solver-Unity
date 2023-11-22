using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tube : MonoBehaviour
{
    public GameObject[] WaterList;
    private GameManager _gameManager;
    private LevelManager _levelManager;
    private Vector3 _initialPosition;
    private Vector3 _targetPosition;
    [SerializeField] private float _speed = 1f;
    [SerializeField] private float _turnSpeed = 1f;

    void Start()
    {
        _gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        _levelManager = GameObject.Find("LevelManager").GetComponent<LevelManager>();
        _initialPosition = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnMouseDown()
    {
        if (_gameManager.GameState == GameStates.Playing && !_levelManager.IsCoroutineRunning)
        {
            if (_levelManager.IsTubeSelected)
            {
                if (_levelManager.SelectedTube == this.gameObject)
                {
                    _levelManager.DeselectTube();
                }
                else
                {
                    _levelManager.PourWater(_levelManager.SelectedTube, this.gameObject);
                }
            }
            else
            {
                _levelManager.SelectTube(this.gameObject);
            }
        }
        
    }

    public void MoveTube(Vector2 targetPosition)
    {
        _levelManager.IsCoroutineRunning = true;
        _targetPosition = new Vector3(targetPosition.x, targetPosition.y, transform.position.z);
        StartCoroutine(MoveTubeToPosition());
    }

    public void MoveTubeBack()
    {
        _levelManager.IsCoroutineRunning = true;
        _targetPosition = _initialPosition;
        StartCoroutine(MoveTubeToPosition());
    }

    public void HandlePourAnimation(Vector2 targetPostion)
    {
        _levelManager.IsCoroutineRunning = true;
        _targetPosition = new Vector3(targetPostion.x, targetPostion.y, transform.position.z);
        StartCoroutine(Pour());
    }

    private IEnumerator MoveTubeToPosition()
    {
        while ((_targetPosition - transform.position).sqrMagnitude > 0.001)
        {
            transform.Translate((_targetPosition - transform.position) * Time.deltaTime * _speed);
            yield return new WaitForEndOfFrame();
        }
        transform.position = _targetPosition;
        _levelManager.IsCoroutineRunning = false;
    }

    private IEnumerator Pour()
    {
        while ((_targetPosition - transform.position).sqrMagnitude > 0.001)
        {
            transform.Translate((_targetPosition - transform.position) * Time.deltaTime * _speed);
            yield return new WaitForEndOfFrame();
        }
        
        while (transform.eulerAngles.z < 88)
        {
            transform.Rotate(Vector3.forward * Time.deltaTime * _turnSpeed);
            yield return new WaitForEndOfFrame();
        }

        _levelManager.UpdateTube();
        _levelManager.CurrentState = _levelManager.NewState;
        
        while (transform.eulerAngles.z > 5)
        {
            transform.Rotate(Vector3.back * Time.deltaTime * _turnSpeed);
            yield return new WaitForEndOfFrame();
        }
        transform.rotation = Quaternion.identity;

        _targetPosition = _initialPosition;
        while ((_targetPosition - transform.position).sqrMagnitude > 0.001)
        {
            transform.Translate((_targetPosition - transform.position) * Time.deltaTime * _speed);
            yield return new WaitForEndOfFrame();
        }

        _levelManager.DeselectTube();
        _levelManager.IsCoroutineRunning = false;
    }

}
