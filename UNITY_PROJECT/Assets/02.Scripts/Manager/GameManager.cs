/// <summary>
/// GameManager.cs
/// 
/// ＃역할
/// 게임에서 발생하는 이벤트(버튼, 플래그)를 처리 합니다.
/// PanelManager에게 해당된 레벨에 따른 패턴을 지시합니다.
/// 오리지널 또는 커스텀 노래 조회 버튼을 눌렀을 때 라이브러리 내 음악을 조회 후 각 정보들을 Element들에게 전달합니다.
/// 
/// ＃스크립팅 기술
/// 
/// </summary>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("[UI]")]
    public GameObject musicSelect;      // UI Lobby
    public GameObject musicStart;       // UI Ingame
    public GameObject musicPaused;      // UI Pause
    public GameObject result;           // UI Result

    public GameObject originalScrollView; // UI Original View
    public GameObject customScrollView;   // UI Custom View

    public GameObject contentOriginal;    // 오리지널 리소스 프리팹 생성 위치(부모)
    public GameObject contentCustom;      // 커스텀   리소스 프리팹 생성 위치(부모)
    public GameObject contentResult;      // 결과     리소스 프리팹 생성 위치(부모)

    public GameObject infoTitle;          // Panel Music Info - Music Title

    public GameObject btnPlay;            // Game Start

    [Header("[Environment Objects]")]
    public GameObject inGameEnvironment;
    public GameObject baseGround;
    public GameObject vizualizationObjects;

    [Header("[Audio Source]")]
    public AudioSource musicBackGround; // BGM
    public AudioSource musicSelected;   // 선택된 노래
    public AudioSource musicPlayed;     // 플레이 할 노래

    [Header("[Prefabs]")]
    public GameObject musicElement;
    public GameObject resultElement;

    [Header("[Origin Controller]")]
    public GameObject layLeftController;
    public GameObject layRightController;
    public GameObject handLeftController;
    public GameObject handRightController;

    [Header("[Music Info]")]
    Stopwatch stopwatch = new Stopwatch();
    public float playTime;
    public int bpm;
    public float secPerBeat;
    public float timer; // BPM 계산 타이머

    // [Header("[플래그 변수]")]
    [HideInInspector] public bool isOriginal;    // [Button] Original MusicList Selected
    [HideInInspector] public bool isCustom;      // [Button] Custom MusicList Selected

    [HideInInspector] public bool isLevelEasy;   // [Button] Level Easy Selected
    [HideInInspector] public bool isLevelNormal; // [Button] Level Normal Selected
    [HideInInspector] public bool isLevelHard;   // [Button] Level Hard Selected

    [HideInInspector] public bool isHandChange;  // True : Hand Controller | False : Lay Controller

    [HideInInspector] public bool isStart;       // Game Start
    [HideInInspector] public bool isPause;       // Game Pause

    [HideInInspector] public bool isSensorLeft;  // 패널 접촉 유/무 왼쪽
    [HideInInspector] public bool isSensorRight; // 패널 접촉 유/무 오른쪽
    
    public static GameManager instance;
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);

        OriginalListRenewal();
    }

    private void Update()
    {
        if (isStart && !isPause)
        {
            stopwatch.Start();
            UnityEngine.Debug.Log(stopwatch.Elapsed);
            if (playTime <= stopwatch.Elapsed.Seconds)
            {
                InGameEnd();
                return;
            }
        }
    }
    // https://codingmania.tistory.com/205
    // 노래 끝나면 종료 이벤트 코루틴 사용
    IEnumerator Playing()
    {
        if (!musicPlayed.isPlaying)
        {

        }
    }

    public static string TimeFormatter(float seconds, bool forceHHMMSS = false) // 시간 변환 함수
    {
        float secondsRemainder = Mathf.Floor((seconds % 60) * 100) / 100.0f;
        int minutes = ((int)(seconds / 60)) % 60;
        int hours = (int)(seconds / 3600);

        if (!forceHHMMSS)
        {
            if (hours == 0)
            {
                return System.String.Format("{0:00}:{1:00.00}", minutes, secondsRemainder);
            }
        }
        return System.String.Format("{0}:{1:00}:{2:00}", hours, minutes, secondsRemainder);
    }

    // [Button] Original MusicList Selected
    public void BtnOriginalSelected()
    {
        if (!isOriginal && isCustom)
        {
            originalScrollView.SetActive(true);
            customScrollView.SetActive(false);
            OriginalListRenewal();

            musicSelected.Stop();
            musicBackGround.UnPause();
        }
    }

    // [Button] Custom MusicList Selected
    public void BtnCustomSelected()
    {
        if (isOriginal && !isCustom)
        {
            originalScrollView.SetActive(false);
            customScrollView.SetActive(true);
            CustomListRenewal();

            musicSelected.Stop();
            musicBackGround.UnPause();
        }
    }


    // [Button] Easy
    public void BtnLvEasy()
    {
        secPerBeat = 300f / bpm;
        btnPlay.GetComponent<Button>().interactable = true; // 노래 재생(Play) 버튼 활성화
    }

    // [Button] Normal
    public void BtnLvNormal()
    {
        secPerBeat = 240f / bpm;
        btnPlay.GetComponent<Button>().interactable = true; // 노래 재생(Play) 버튼 활성화
    }

    // [Button] Hard
    public void BtnLvHard()
    {
        secPerBeat = 180f / bpm;
        btnPlay.GetComponent<Button>().interactable = true; // 노래 재생(Play) 버튼 활성화
    }


    // [Button] Play
    public void BtnInGameStart()
    {
        isStart = true;
        isHandChange = true;

        musicSelect.SetActive(false); // Lobby UI OFF
        musicStart.SetActive(true); // Ingame UI ON
        baseGround.SetActive(false);
        vizualizationObjects.SetActive(true); // VizualizationObj ON
        inGameEnvironment.SetActive(true); // 인게임 환경 요소 ON

        musicBackGround.Pause(); // BGM Pause
        musicSelected.Stop(); // Selected Music OFF
        musicPlayed.Play(); // Played Music ON

        ControllerModeChange();
    }

    // [Button] Pause
    public void BtnInGamePause()
    {
        if (isStart)
        {
            isPause = true;
            isHandChange = false;

            musicPaused.SetActive(true); // Music Paused UI On

            Time.timeScale = 0;
            musicPlayed.Pause(); // 플레이 중 노래 일시 정지

            ControllerModeChange();
        }
    }

    // [Button] UnPause
    public void BtnInGameUnPause()
    {
        if (isStart && isPause)
        {
            isPause = false;
            isHandChange = true;

            musicPaused.SetActive(false); // Music Paused UI Off

            Time.timeScale = 1;
            musicPlayed.UnPause(); // 플레이 중 노래 일시 정지 해제

            ControllerModeChange();
        }
    }

    // End
    public void InGameEnd()
    {
        isStart = false;
        isPause = false;
        isHandChange = false;

        result.SetActive(true); // Result UI On

        musicBackGround.UnPause();
        musicPlayed.Stop(); // Played Song Reset

        timer = 0;
        secPerBeat = 0;
        btnPlay.GetComponent<Button>().interactable = false;
        PanelManager.instance.lastIndex = 0;
        PanelManager.instance.safeQuiz = false;
        for (int i = 0; i < PanelManager.instance.panelSpawnPoint.transform.childCount; i++)
        {
            Destroy(PanelManager.instance.panelSpawnPoint.transform.GetChild(i).gameObject);
        }

        ControllerModeChange();
    }

    // [Button] Pause to Back to the Lobby
    public void BtnPauseBackLobby()
    {
        if (isStart && isPause)
        {
            isStart = false;
            isPause = false;
            isHandChange = false;

            musicSelect.SetActive(true); // Lobby UI On
            baseGround.SetActive(true);
            musicStart.SetActive(false); // Ingame UI Off
            musicPaused.SetActive(false); // MusicPaused UI Off
            result.SetActive(false); // Result UI Off
            vizualizationObjects.SetActive(false); // VizualizationObj Off
            inGameEnvironment.SetActive(false); // Ingame Env Obj Off

            Time.timeScale = 1;
            musicBackGround.UnPause();
            musicPlayed.Stop(); // 플레이 중 노래 정지

            timer = 0;
            secPerBeat = 0;
            btnPlay.GetComponent<Button>().interactable = false;
            PanelManager.instance.lastIndex = 0;
            PanelManager.instance.safeQuiz = false;
            for (int i = 0; i < PanelManager.instance.panelSpawnPoint.transform.childCount; i++)
            {
                Destroy(PanelManager.instance.panelSpawnPoint.transform.GetChild(i).gameObject);
            }

            ControllerModeChange();
        }
    }

    // [Button] End to Back to the Lobby
    public void BtnEndBackLobby()
    {

    }

    object[] originalMusics;
    object[] customMusics;

    GameObject originalMusicElementPrefab = null;
    GameObject customMusicElementPrefab   = null;

    public void OriginalListRenewal()
    {
        isOriginal = true;
        isCustom = false;

        // Custom Music 폴더의 AudioClip 속성 파일 조회
        originalMusics = Resources.LoadAll<AudioClip>("Original Music");

        for (int i = 0; i < originalMusics.Length; i++)
        {
            originalMusicElementPrefab = originalMusics[i] as GameObject; // AudioClip to GameObject
            originalMusicElementPrefab = Instantiate(musicElement);
            originalMusicElementPrefab.name = $"Original Music Element_{i}";
            originalMusicElementPrefab.transform.parent = contentOriginal.transform;
            originalMusicElementPrefab.transform.localScale = Vector3.one; 
            originalMusicElementPrefab.transform.position = contentOriginal.transform.position;

            //originalMusicElementPrefab.transform.GetChild(3).GetComponent<AudioSource>().playOnAwake = false; // Off 'Play On Awake'

            // AudioSource.clip ← Resources-Custom Musics.AudioClip
            originalMusicElementPrefab.transform.GetChild(3).gameObject.GetComponent<AudioSource>().clip = (AudioClip)originalMusics[i];

            // (float)MusicLength to (string)PlayTime
            originalMusicElementPrefab.transform.GetChild(2).gameObject.GetComponent<Text>().text =
                TimeFormatter(originalMusicElementPrefab.transform.GetChild(3).gameObject.GetComponent<AudioSource>().clip.length, false);

            // textTitle.text ← customMusicElements.AudioSource.text
            originalMusicElementPrefab.transform.GetChild(1).gameObject.GetComponent<Text>().text =
                originalMusicElementPrefab.transform.GetChild(3).gameObject.GetComponent<AudioSource>().clip.name;
        }
    }

    public void CustomListRenewal()
    {
        isOriginal = false;
        isCustom = true;

        // Custom Music 폴더의 AudioClip 속성 파일 조회
        customMusics = Resources.LoadAll<AudioClip>("Custom Music");

        for (int i = 0; i < customMusics.Length; i++)
        {
            customMusicElementPrefab = customMusics[i] as GameObject; // AudioClip to GameObject
            customMusicElementPrefab = Instantiate(musicElement);
            customMusicElementPrefab.name = $"Custom Music Element_{i}";
            customMusicElementPrefab.transform.parent = contentCustom.transform;
            customMusicElementPrefab.transform.localScale = Vector3.one;
            customMusicElementPrefab.transform.position = contentCustom.transform.position;

            //customMusicElementPrefab.transform.GetChild(3).GetComponent<AudioSource>().playOnAwake = false; // Off 'Play On Awake'

            // AudioSource.clip ← Resources-Custom Musics.AudioClip
            customMusicElementPrefab.transform.GetChild(3).gameObject.GetComponent<AudioSource>().clip = (AudioClip)customMusics[i];

            // (float)MusicLength to (string)PlayTime
            customMusicElementPrefab.transform.GetChild(2).gameObject.GetComponent<Text>().text = 
                TimeFormatter(customMusicElementPrefab.transform.GetChild(3).gameObject.GetComponent<AudioSource>().clip.length, false);

            // textTitle.text ← customMusicElements.AudioSource.text
            customMusicElementPrefab.transform.GetChild(1).gameObject.GetComponent<Text>().text = 
                customMusicElementPrefab.transform.GetChild(3).gameObject.GetComponent<AudioSource>().clip.name;
        }
    }

    void ControllerModeChange()
    {
        if /*Hand Controller*/ (isHandChange) 
        {
            layLeftController .SetActive(false);
            layRightController.SetActive(false);

            handLeftController .SetActive(true);
            handRightController.SetActive(true);

            isHandChange = false;
        }
        else /*Lay Controller*/
        {
            handLeftController .SetActive(false);
            handRightController.SetActive(false);

            layLeftController .SetActive(true);
            layRightController.SetActive(true);

            isHandChange = true;
        }
    }
}