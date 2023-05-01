using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Fusion;
using System.Linq;
using System.Threading.Tasks;
using System;
using FusionUtilsEvents;

public class LevelBehaviour : NetworkBehaviour
{
    public FusionEvent OnPlayerDisconnectEvent;
    [SerializeField] private float _levelTime = 300f;
    [Networked]
    private TickTimer StartTimer { get; set; }
    [Networked]
    private TickTimer LevelTimer { get; set; }

    [SerializeField] private Text _timerText;
    [SerializeField] private Text _levelTimerText;

    [SerializeField]
    private int _playersAlreadyFinish = 0;

    [Networked, Capacity(3)]
    private NetworkArray<int> _winners => default;
    public NetworkArray<int> Winners { get => _winners; }

    private FinishRaceScreen _finishRace;

    [Space()]
    [Header("Music")]
    public SoundSO LevelMusic;
    public SoundChannelSO MusicChannel;
    [SerializeField] public bool Mute = false;

    public override void Spawned()
    {
        FindObjectOfType<PlayerSpawner>().RespawnPlayers(Runner);
        FindObjectOfType<ScorptionSpawner>().RespawnScorptions(Runner);
        _finishRace = FindObjectOfType<FinishRaceScreen>();
        StartLevel();
    }

    void OnEnable()
    {
        if (FusionHelper.LocalRunner.IsServer)
        {
            OnPlayerDisconnectEvent.RegisterResponse(CheckWinnersOnPlayerDisconnect);
        }
    }

    void OnDisable()
    {
        if (FusionHelper.LocalRunner.IsServer)
        {
            OnPlayerDisconnectEvent.RemoveResponse(CheckWinnersOnPlayerDisconnect);
        }
    }

    public void StartLevel()
    {
        SetLevelStartValues();
        StartLevelMusic();
        LoadingManager.Instance.FinishLoadingScreen();
        GameManager.Instance.SetGameState(GameManager.GameState.Playing);
    }

    private void StartLevelMusic()
    {
        if (!Mute)
        {
            MusicChannel.CallMusicEvent(LevelMusic);
        }
    }

    public override void FixedUpdateNetwork()
    {

        if (StartTimer.IsRunning && _timerText.gameObject.activeInHierarchy)
        {
            _timerText.text = ((int?)StartTimer.RemainingTime(Runner)).ToString();
        }

        if (StartTimer.Expired(Runner))
        {

            _timerText.gameObject.SetActive(false);
            LevelTimer = TickTimer.CreateFromSeconds(Runner, _levelTime);
            _levelTimerText.gameObject.SetActive(true);

            GameManager.Instance.AllowAllPlayersInputs();
        }

        if (LevelTimer.IsRunning)
        {
            if (Object.HasStateAuthority && LevelTimer.Expired(Runner) && (_playersAlreadyFinish < 3 || _playersAlreadyFinish < Runner.ActivePlayers.Count()))
            {
                RPC_FinishLevel();
                LevelTimer = TickTimer.None;
            }
            _levelTimerText.text = ((int?)LevelTimer.RemainingTime(Runner)).ToString();
        }
    }

    /// <summary>
    /// Register player as winner.
    /// </summary>
    /// <param name="player"></param>
    public void PlayerOnFinishLine(PlayerRef player, PlayerBehaviour playerBehaviour)
    {
        if (_playersAlreadyFinish >= 3 || Winners.Contains(player)) { return; }

        Winners.Set(_playersAlreadyFinish, player.PlayerId);

        _playersAlreadyFinish++;

        playerBehaviour.SetInputsAllowed(false);

        if (_playersAlreadyFinish >= 3 || _playersAlreadyFinish >= Runner.ActivePlayers.Count())
        {
            RPC_FinishLevel();
            return;
        }
    }


    private void CheckWinnersOnPlayerDisconnect(PlayerRef player, NetworkRunner runner)
    {
        Debug.Log(runner.ActivePlayers.Count());
        if (_playersAlreadyFinish >= 3 || _playersAlreadyFinish >= runner.ActivePlayers.Count())
        {
            RPC_FinishLevel();
        }
    }

    /// <summary>
    /// Call finish level logic, result screen and start a random level after.
    /// </summary>
    [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
    private void RPC_FinishLevel()
    {
        int i = 0;
        foreach (var player in Winners)
        {
            PlayerData data = GameManager.Instance.GetPlayerData(player, Runner);
            if (data != null)
            {
                _finishRace.SetWinner(data.Nick.ToString(), data.Instance.GetComponent<PlayerBehaviour>().PlayerColor, i);
            }
            i++;
        }

        _finishRace.FadeIn();

        _finishRace.Invoke("FadeOut", 5f);

        Invoke("NextLevel", 5f);
    }

    private void RandomLevel()
    {
        if (FusionHelper.LocalRunner.IsClient) return;
        LoadingManager.Instance.LoadRandomLevel(Runner);
    }

    //Called by Invoke.
    private void NextLevel()
    {
        if (FusionHelper.LocalRunner.IsClient) return;
        LoadingManager.Instance.LoadNextLevel(FusionHelper.LocalRunner);
    }

    /// <summary>
    /// Start initial wall and set level control values ​​to default
    /// </summary>
    private void SetLevelStartValues()
    {
        _playersAlreadyFinish = 0;
        StartTimer = TickTimer.CreateFromSeconds(Runner, 5);
        _timerText.gameObject.SetActive(true);

        for (int i = 0; i < 3; i++)
        {
            Winners.Set(i, -1);
        }
    }
}
