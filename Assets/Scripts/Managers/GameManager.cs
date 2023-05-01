using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement ;
using Fusion;
using FusionUtilsEvents;
using System.Threading.Tasks;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public FusionEvent OnPlayerLeftEvent;
    public FusionEvent OnRunnerShutDownEvent;

    public enum GameState
    {
        Lobby,
        Playing,
        Loading
    }

    public GameState State { get; private set; }

    [Space]

    public LevelManager LoadLevelManager;

    [SerializeField] private GameObject _exitCanvas;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(this.transform.parent.gameObject);
        }
        DontDestroyOnLoad(transform.parent);
    }

    private void OnEnable()
    {
        OnPlayerLeftEvent.RegisterResponse(PlayerDisconnected);
        OnRunnerShutDownEvent.RegisterResponse(DisconnectedFromSession);
    }

    private void OnDisable()
    {
        OnPlayerLeftEvent.RegisterResponse(PlayerDisconnected);
        OnRunnerShutDownEvent.RemoveResponse(DisconnectedFromSession);
    }

    public void SetGameState(GameState state)
    {
        State = state;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && State == GameState.Playing)
        {
            _exitCanvas.SetActive(!_exitCanvas.activeInHierarchy);
        }
    }

    public PlayerData GetPlayerData(PlayerRef player, NetworkRunner runner)
    {
        NetworkObject NO;
        if (runner.TryGetPlayerObject(player, out NO))
        {
            PlayerData data = NO.GetComponent<PlayerData>();
            return data;
        }
        else
        {
            Debug.LogWarning("Player not found");
            return null;
        }
    }

    public void AllowAllPlayersInputs()
    {
        foreach (PlayerBehaviour behaviour in FindObjectsOfType<PlayerBehaviour>())
        {
            behaviour.SetInputsAllowed(true);
        }
    }

    /// <summary>
    /// Start player's spectator state.
    /// </summary>
    public void SetPlayerSpectating(PlayerBehaviour playerBehaviour)
    {
        FindObjectOfType<CameraManager>().SetSpectating();
        playerBehaviour.SetInputsAllowed(false);
    }

    public void PlayerDisconnected(PlayerRef player, NetworkRunner runner)
    {
        runner.Despawn(runner.GetPlayerObject(player).GetComponent<PlayerData>().Instance);
        runner.Despawn(runner.GetPlayerObject(player));
    }

    //Called by button
    public void LeaveRoom()
    {
        _ = LeaveRoomAsync();
    }

    private async Task LeaveRoomAsync()
    {
        await ShutdownRunner();
    }

    private async Task ShutdownRunner()
    {
        await FusionHelper.LocalRunner?.Shutdown(destroyGameObject: false);
        SetGameState(GameState.Lobby);
    }

    public void DisconnectedFromSession(PlayerRef player, NetworkRunner runner)
    {
        Debug.Log("Disconnected from the session");
        ExitSession();
    }

    public void ExitSession()
    {
        _ = ShutdownRunner();
        LoadLevelManager.ResetLoadedScene();
        SceneManager.LoadScene(0);
        _exitCanvas.SetActive(false);
    }

    public void ExitGame()
    {
        _ = ShutdownRunner();
        Application.Quit();
    }
}
