﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using TMPro;

public class MainMenuManager : MonoBehaviour, ILobbyCallbacks, IInRoomCallbacks, IOnEventCallback, IConnectionCallbacks, IMatchmakingCallbacks {
    public static MainMenuManager Instance; 
    Color defaultColor = Color.white;
    AudioSource music, sfx;
    public GameObject lobbiesContent, lobbyPrefab;
    public AudioClip buhBye, musicStart, musicLoop; 
    bool quit, validName;
    public GameObject connecting;
    public GameObject mainMenu, optionsMenu, lobbyMenu, createLobbyPrompt, inLobbyPrompt, creditsPage;
    public GameObject[] levelCameraPositions;
    public GameObject sliderText, lobbyText;
    public TMP_Dropdown levelDropdown;
    public RoomIcon selectedRoom;
    public Button joinRoomBtn, createRoomBtn, startGameBtn, changeCharacterBtn;
    public Toggle ndsResolutionToggle, fullscreenToggle;
    public GameObject playersContent, playersPrefab, chatContent, chatPrefab; 
    public TMP_InputField nicknameField, lobbyNameField;
    public Slider musicSlider, sfxSlider, masterSlider;
    public Slider starSlider;
    public TMP_Text starsText;
    public GameObject mainMenuSelected, optionsSelected, lobbySelected, currentLobbySelected, createLobbySelected, creditsSelected;
    private int prevWidth = 1280, prevHeight = 720;
    public GameObject errorBox;
    public TMP_Text errorText;

    // LOBBY CALLBACKS
    public void OnJoinedLobby() {
        ExitGames.Client.Photon.Hashtable prop = new ExitGames.Client.Photon.Hashtable();
        prop.Add("character", 0);
        prop.Add("ping", PhotonNetwork.GetPing());
        PhotonNetwork.LocalPlayer.SetCustomProperties(prop);

        StartCoroutine(UpdatePing());
    }
    public void OnLeftLobby() {}
    public void OnLobbyStatisticsUpdate(List<TypedLobbyInfo> lobbies) {}
    public void OnRoomListUpdate(List<RoomInfo> roomList) {
        //clear existing
        for (int i = 0; i < lobbiesContent.transform.childCount; i++) {
            GameObject roomObj = lobbiesContent.transform.GetChild(i).gameObject;
            if (!roomObj.activeSelf) continue;
            GameObject.Destroy(roomObj);
        }
        //add new rooms
        //TODO refactor??
        int count = 0;
        foreach (RoomInfo room in roomList) {
            if (!room.IsVisible || !room.IsOpen || room.MaxPlayers <= 0) {
                continue;
            } 
            GameObject newLobby = GameObject.Instantiate(lobbyPrefab, Vector3.zero, Quaternion.identity, lobbiesContent.transform);
            newLobby.SetActive(true);
            RectTransform rect = newLobby.GetComponent<RectTransform>();
            rect.offsetMin = new Vector2(0, (count-1) * 55f);
            rect.offsetMax = new Vector2(0, (count) * 55f);
            SetText(newLobby.transform.Find("LobbyName").gameObject, "Name: " + room.Name);
            SetText(newLobby.transform.Find("LobbyPlayers").gameObject, "Players: " + room.PlayerCount + "/" + room.MaxPlayers);
            newLobby.GetComponent<RoomIcon>().room = room;
            count--;
        }
    }

    // ROOM CALLBACKS
    public void OnPlayerPropertiesUpdate(Player player, ExitGames.Client.Photon.Hashtable playerProperties) {
        UpdatePlayerList(player);
    }
    public void OnMasterClientSwitched(Player newMaster) {
        LocalChatMessage(newMaster.NickName + " has become the Host", ColorToVector(Color.red));
    }
    public void OnJoinedRoom() {
        LocalChatMessage(PhotonNetwork.LocalPlayer.NickName + " joined the lobby", ColorToVector(Color.red));
        EnterRoom();
    }
    public void OnPlayerEnteredRoom(Player newPlayer) {
        LocalChatMessage(newPlayer.NickName + " joined the lobby", MainMenuManager.ColorToVector(Color.red));
        PopulatePlayerList();
    }
    public void OnPlayerLeftRoom(Player otherPlayer) {
        LocalChatMessage(otherPlayer.NickName + " left the lobby", MainMenuManager.ColorToVector(Color.red));
        PopulatePlayerList();
    }
    public void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable properties) {
        if (properties == null)
            return;

        if (properties[Enums.NetRoomProperties.Level] != null)
            ChangeLevel((int) properties[Enums.NetRoomProperties.Level]);
        if (properties[Enums.NetRoomProperties.StarRequirement] != null)
            ChangeStarRequirement((int) properties[Enums.NetRoomProperties.StarRequirement]);
    }
    // CONNECTION CALLBACKS
    public void OnConnected() {}
    public void OnDisconnected(DisconnectCause cause) {
        OpenErrorBox("Disconnected: " + cause.ToString());
        //TODO reconnect + offline option?
    }
    public void OnRegionListReceived(RegionHandler handler) {
        //TODO changing server regions?
    }
    public void OnCustomAuthenticationResponse(Dictionary<string, object> response) {}
    public void OnCustomAuthenticationFailed(string failure) {}
    public void OnConnectedToMaster() {
        Debug.Log("Connected to Master");
        PhotonNetwork.JoinLobby(new TypedLobby(Application.version, LobbyType.Default));
    }
    // MATCHMAKING CALLBACKS
    public void OnFriendListUpdate(List<FriendInfo> friendList) {}
    public void OnLeftRoom() {
        OpenLobbyMenu();
        ClearChat();
    }
    public void OnJoinRandomFailed(short reasonId, string reasonMessage) {
        OnJoinRoomFailed(reasonId, reasonMessage);
    }
    public void OnJoinRoomFailed(short reasonId, string reasonMessage) {
        //TODO: error messages
        Debug.LogError("join room failed, " + reasonId + ": " + reasonMessage);
        OpenErrorBox(reasonMessage);
    }
    public void OnCreateRoomFailed(short reasonId, string reasonMessage) {
        //TOOD: error message
        Debug.LogError("create room failed, " + reasonId + ": " + reasonMessage);
        OpenErrorBox(reasonMessage);
    }
    public void OnCreatedRoom() {
        Debug.Log("Created Room: " + PhotonNetwork.CurrentRoom.Name);

        ExitGames.Client.Photon.Hashtable table = new ExitGames.Client.Photon.Hashtable();
        table[Enums.NetRoomProperties.Level] = 0;
        table[Enums.NetRoomProperties.StarRequirement] = 10;
        PhotonNetwork.CurrentRoom.SetCustomProperties(table);
    }
    // CUSTOM EVENT CALLBACKS
    public void OnEvent(EventData e) {
        switch (e.Code) {
        case (byte) Enums.NetEventIds.StartGame: {
            PhotonNetwork.IsMessageQueueRunning = false;
            SceneManager.LoadSceneAsync(1, LoadSceneMode.Single);
            SceneManager.LoadSceneAsync(levelDropdown.value + 2, LoadSceneMode.Additive);
            break;
        }
        case (byte) Enums.NetEventIds.ChatMessage: {
            object[] data = (object[]) e.CustomData;
            string message = (string) data[0];
            Vector3 color = (Vector3) data[1];
            LocalChatMessage(message, color);
            break;
        }
        }
    }

    // CALLBACK REGISTERING
    void OnEnable() {
        PhotonNetwork.AddCallbackTarget(this);
    }
    void OnDisable() {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    // Unity Stuff
    void Start() {
        Instance = this;
        music = GetComponents<AudioSource>()[0];
        sfx = GetComponents<AudioSource>()[1];
        HorizontalCamera.OFFSET_TARGET = 0; 
        
        PhotonNetwork.SerializationRate = 30;

        AudioMixer mixer = music.outputAudioMixerGroup.audioMixer;
        mixer.SetFloat("MusicSpeed", 1f);
        mixer.SetFloat("MusicPitch", 1f);

        if (PhotonNetwork.InRoom) {
            OnJoinedRoom();
        }

        music.clip = musicStart;
        music.Play();
        lobbyPrefab = lobbiesContent.transform.Find("Template").gameObject; 

        PhotonNetwork.NickName = PlayerPrefs.GetString("Nickname");
        if (PhotonNetwork.NickName == null || PhotonNetwork.NickName == "")
            PhotonNetwork.NickName = "Player" + Random.Range(1000, 10000);
        Camera.main.transform.position = levelCameraPositions[Random.Range(0,levelCameraPositions.Length-1)].transform.position;
        
        nicknameField.text = PhotonNetwork.NickName;
        musicSlider.value = Settings.Instance.VolumeMusic;
        sfxSlider.value = Settings.Instance.VolumeSFX;
        masterSlider.value = Settings.Instance.VolumeMaster;

        ndsResolutionToggle.isOn = Settings.Instance.ndsResolution;
        fullscreenToggle.isOn = Screen.fullScreenMode == FullScreenMode.FullScreenWindow;
    }

    void Update() {
        bool connected = PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InLobby;
        connecting.SetActive(!connected);
        if (!music.isPlaying) {
            music.clip = musicLoop;
            music.loop = true;
            music.Play();
        }

        joinRoomBtn.interactable = connected && selectedRoom != null && validName;
        createRoomBtn.interactable = connected && validName;
    }

    IEnumerator UpdatePing() {
        while (true) {
            yield return new WaitForSeconds(2);
            if (PhotonNetwork.InRoom) {
                ExitGames.Client.Photon.Hashtable prop = new ExitGames.Client.Photon.Hashtable();
                prop.Add("ping", PhotonNetwork.GetPing());
                PhotonNetwork.LocalPlayer.SetCustomProperties(prop);
            }
        }
    }

    void EnterRoom() {
        RoomInfo room = PhotonNetwork.CurrentRoom;
        OpenInLobbyMenu(room);
        PopulatePlayerList();
        changeCharacterBtn.image.sprite = Utils.GetCharacterData(PhotonNetwork.LocalPlayer).buttonSprite;

        OnRoomPropertiesUpdate(room.CustomProperties);
    }

    public void OpenMainMenu() {
        mainMenu.SetActive(true);
        optionsMenu.SetActive(false);
        lobbyMenu.SetActive(false);
        createLobbyPrompt.SetActive(false);
        inLobbyPrompt.SetActive(false);
        creditsPage.SetActive(false);

        EventSystem.current.SetSelectedGameObject(mainMenuSelected);
    }
    public void OpenLobbyMenu() {
        if (!PhotonNetwork.IsConnected) {
            PhotonNetwork.ConnectUsingSettings();
        }
        mainMenu.SetActive(false);
        optionsMenu.SetActive(false);
        lobbyMenu.SetActive(true);
        createLobbyPrompt.SetActive(false);
        inLobbyPrompt.SetActive(false);
        creditsPage.SetActive(false);

        nicknameField.interactable = true;
        EventSystem.current.SetSelectedGameObject(lobbySelected);
    }
    public void OpenCreateLobby() {
        mainMenu.SetActive(false);
        optionsMenu.SetActive(false);
        lobbyMenu.SetActive(true);
        createLobbyPrompt.SetActive(true);
        inLobbyPrompt.SetActive(false);
        creditsPage.SetActive(false);

        nicknameField.interactable = false;
        bool endswithS = nicknameField.text.EndsWith("s");
        lobbyNameField.text = nicknameField.text + "'" + (endswithS ? "" : "s") + " Lobby";
        EventSystem.current.SetSelectedGameObject(createLobbySelected);
    }
    public void OpenOptions() {
        mainMenu.SetActive(false);
        optionsMenu.SetActive(true);
        lobbyMenu.SetActive(false);
        createLobbyPrompt.SetActive(false);
        inLobbyPrompt.SetActive(false);
        creditsPage.SetActive(false);

        EventSystem.current.SetSelectedGameObject(optionsSelected);
    }
    public void OpenCredits() {
        mainMenu.SetActive(false);
        optionsMenu.SetActive(false);
        lobbyMenu.SetActive(false);
        createLobbyPrompt.SetActive(false);
        inLobbyPrompt.SetActive(false);
        creditsPage.SetActive(true);

        EventSystem.current.SetSelectedGameObject(creditsSelected);
    }
    public void OpenInLobbyMenu(RoomInfo room) {
        mainMenu.SetActive(false);
        optionsMenu.SetActive(false);
        lobbyMenu.SetActive(false);
        createLobbyPrompt.SetActive(false);
        inLobbyPrompt.SetActive(true);
        creditsPage.SetActive(false);

        lobbyText.GetComponent<TextMeshProUGUI>().text = room.Name;
        EventSystem.current.SetSelectedGameObject(currentLobbySelected);
    }
    public void OpenErrorBox(string text) {
        errorBox.SetActive(true);
        errorText.text = text;
    }

    public void QuitRoom() {
        PhotonNetwork.LeaveRoom();
    }
    public void StartGame() {
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;

        //start game with all players
        RaiseEventOptions options = new RaiseEventOptions{Receivers = ReceiverGroup.All};
        PhotonNetwork.RaiseEvent((byte) Enums.NetEventIds.StartGame, null, options, SendOptions.SendReliable);
    }
    public void ChangeLevel(int index) {
        levelDropdown.value = index;
        levelDropdown.RefreshShownValue();
        Camera.main.transform.position = levelCameraPositions[index].transform.position;
    }
    public void SetLevelIndex() {
        if (!PhotonNetwork.IsMasterClient) return;
        int newLevelIndex = levelDropdown.value;
        if (newLevelIndex == (int) PhotonNetwork.CurrentRoom.CustomProperties[Enums.NetRoomProperties.Level]) return;

        GlobalChatMessage("Map set to: " + levelDropdown.captionText.text, ColorToVector(Color.red));

        ExitGames.Client.Photon.Hashtable table = new ExitGames.Client.Photon.Hashtable();
        table[Enums.NetRoomProperties.Level] = levelDropdown.value;
        PhotonNetwork.CurrentRoom.SetCustomProperties(table);
    }
    public void SelectRoom(GameObject room) {
        if (selectedRoom) {
            selectedRoom.Unselect();
        }
        selectedRoom = room.GetComponent<RoomIcon>();
        selectedRoom.Select();
        joinRoomBtn.interactable = room != null && PhotonNetwork.NickName.Length >= 3;
    }
    public void JoinSelectedRoom() {
        if (selectedRoom == null) {
            return;
        }
        PhotonNetwork.JoinRoom(selectedRoom.room.Name);
    }
    public void CreateRoom(GameObject lobbyInfo) {
        string room = lobbyNameField.text;
        byte players = (byte) lobbyInfo.transform.Find("MaxPlayers").Find("Slider").gameObject.GetComponent<Slider>().value;
        if (room == null || room == "" || players < 2) {
            return;
        }
        PhotonNetwork.CreateRoom(room, new RoomOptions{MaxPlayers=players, IsVisible=true, PublishUserId=true}, TypedLobby.Default);
        createLobbyPrompt.SetActive(false);
    }
    public void SetMaxPlayersText(Slider slider) {
        sliderText.GetComponent<TextMeshProUGUI>().text = "" + slider.value;
    }
    public void ClearChat() {
        for (int i = 0; i < chatContent.transform.childCount; i++) {
            GameObject chatMsg = chatContent.transform.GetChild(i).gameObject;
            if (!chatMsg.activeSelf) continue;
            GameObject.Destroy(chatMsg);
        }
    }
    public void PopulatePlayerList() {
        for (int i = 0; i < playersContent.transform.childCount; i++) {
            GameObject pl = playersContent.transform.GetChild(i).gameObject;
            if (!pl.activeSelf) continue;
            GameObject.Destroy(pl);
        }

        int count = 0;
        SortedDictionary<int, Photon.Realtime.Player> sortedPlayers = new SortedDictionary<int,Photon.Realtime.Player>(PhotonNetwork.CurrentRoom.Players);
        foreach (KeyValuePair<int, Photon.Realtime.Player> player in sortedPlayers) {
            Player pl = player.Value;

            GameObject newPl = GameObject.Instantiate(playersPrefab, Vector3.zero, Quaternion.identity);
            newPl.name = pl.UserId;
            newPl.transform.SetParent(playersContent.transform);
            newPl.transform.localPosition = new Vector3(0, -(count-- * 40f), 0);
            newPl.transform.localScale = Vector3.one;
            newPl.SetActive(true);
            RectTransform tf = newPl.GetComponent<RectTransform>();
            tf.offsetMax = new Vector2(330, tf.offsetMax.y);
            UpdatePlayerList(pl, newPl.transform);
        }

        startGameBtn.interactable = PhotonNetwork.IsMasterClient;
        levelDropdown.interactable = PhotonNetwork.IsMasterClient;
        starSlider.interactable = PhotonNetwork.IsMasterClient;
    }
    public void UpdatePlayerList(Player pl, Transform nameObject = null) {
        string characterString = Utils.GetCharacterData(pl).uistring;
        object ping;
        pl.CustomProperties.TryGetValue("ping", out ping);
        if (ping == null) ping = -1;
        string pingColor;
        if ((int) ping < 0) {
            pingColor = "black";    
        } else if ((int) ping < 80) {
            pingColor = "#00b900";
        } else if ((int) ping < 120) {
            pingColor = "orange";
        } else {
            pingColor = "red";
        }

        if (nameObject == null) nameObject = playersContent.transform.Find(pl.UserId);
        if (nameObject == null) return;
        SetText(nameObject.Find("NameText").gameObject, (pl.IsMasterClient ? "<sprite=5>" : "") + characterString + pl.NickName);
        SetText(nameObject.Find("PingText").gameObject, "<color=" + pingColor + ">" + (int) ping);
    }
    
    public void GlobalChatMessage(string message, Vector3 color) {
        RaiseEventOptions options = new RaiseEventOptions {Receivers = ReceiverGroup.All};
        object[] parameters = new object[]{ message, color };
        PhotonNetwork.RaiseEvent((byte) Enums.NetEventIds.ChatMessage, parameters, options, SendOptions.SendReliable);
    }
    public void LocalChatMessage(string message, Vector3 color) {
        float y = 0;
        for (int i = 0; i < chatContent.transform.childCount; i++) {
            GameObject child = chatContent.transform.GetChild(i).gameObject;
            if (!child.activeSelf) continue;
            y -= (child.GetComponent<RectTransform>().rect.height + 20);
        }

        GameObject chat = GameObject.Instantiate(chatPrefab, Vector3.zero, Quaternion.identity);
        chat.transform.SetParent(chatContent.transform);
        chat.transform.localPosition = new Vector3(0, y, 0);
        chat.transform.localScale = Vector3.one;
        chat.SetActive(true);
        GameObject txtObject = chat.transform.Find("Text").gameObject;
        SetText(txtObject, message, new Color(color.x, color.y, color.z));
        Canvas.ForceUpdateCanvases();
        RectTransform tf = txtObject.GetComponent<RectTransform>();
        Rect rect = tf.rect;
        Bounds bounds = txtObject.GetComponent<TextMeshProUGUI>().textBounds;
        tf.sizeDelta = new Vector2(tf.sizeDelta.x, (bounds.max.y - bounds.min.y) - 15f);
    }
    public void SendChat(TMP_InputField input) {
        string text = input.text;
        if (input.text == null || input.text == "")
            return;
        
        GlobalChatMessage(PhotonNetwork.NickName + ": " + text, ColorToVector(Color.black));
        input.text = "";
        // EventSystem.current.SetSelectedGameObject(input.gameObject);
    }
    public static Vector3 ColorToVector(Color color) {
        return new Vector3(color.r, color.g, color.b);
    }
    public void SwapCharacter() {
        int character = (int) PhotonNetwork.LocalPlayer.CustomProperties["character"];
        character = (character+1) % GlobalController.Instance.characters.Length;

        ExitGames.Client.Photon.Hashtable prop = new ExitGames.Client.Photon.Hashtable();
        prop.Add("character", character);
        PhotonNetwork.LocalPlayer.SetCustomProperties(prop);

        PlayerData data = GlobalController.Instance.characters[character];
        changeCharacterBtn.image.sprite = data.buttonSprite;
        
        sfx.PlayOneShot((AudioClip) Resources.Load("Sound/" + data.soundFolder + "/selected"));
    }

    public void SetUsername(TMP_InputField field) {
        PhotonNetwork.NickName = field.text;
        validName = field.text.Length > 2;
        if (!validName) {
            ColorBlock colors = field.colors;
            colors.normalColor = new Color(1, 0.7f, 0.7f, 1);
            colors.highlightedColor = new Color(1, 0.55f, 0.55f, 1);
            field.colors = colors;
        } else {
            ColorBlock colors = field.colors;
            colors.normalColor = Color.white;
            field.colors = colors;
        }

        PlayerPrefs.SetString("Nickname", field.text);
        PlayerPrefs.Save();
    }
    private void SetText(GameObject obj, string txt) {
        TextMeshProUGUI textComp = obj.GetComponent<TextMeshProUGUI>();
        textComp.text = txt;
    }
    private void SetText(GameObject obj, string txt, Color color) {
        TextMeshProUGUI textComp = obj.GetComponent<TextMeshProUGUI>();
        textComp.text = txt;
        textComp.color = color;
    }
    IEnumerator FinishQuitting() {
        sfx.PlayOneShot(buhBye);
        quit = true;
        yield return new WaitForSeconds(buhBye.length);
        Application.Quit();
    }
    public void Quit() {
        if (quit) return;
        StartCoroutine(FinishQuitting());
    }

    public void ChangeStarRequirement(int stars) {
        starSlider.value = stars / 5;
        starsText.text = "" + stars;
    }
    public void SetStarRequirementSlider(Slider slider) {
        if (!PhotonNetwork.IsMasterClient) return;
        int newValue = (int) slider.value * 5;
        if (newValue == (int) PhotonNetwork.CurrentRoom.CustomProperties[Enums.NetRoomProperties.StarRequirement]) return;

        ExitGames.Client.Photon.Hashtable table = new ExitGames.Client.Photon.Hashtable();
        table[Enums.NetRoomProperties.StarRequirement] = newValue;
        PhotonNetwork.CurrentRoom.SetCustomProperties(table);
        ChangeStarRequirement(newValue);
    }

    public void SetVolumeMusic() {
        Settings s = Settings.Instance;
        s.VolumeMusic = musicSlider.value;
        s.SaveSettingsToPreferences();
    }
    public void SetVolumeSFX() {
        Settings s = Settings.Instance;
        s.VolumeSFX = sfxSlider.value;
        s.SaveSettingsToPreferences();
    }
    public void SetVolumeMaster() {
        Settings s = Settings.Instance;
        s.VolumeMaster = masterSlider.value;
        s.SaveSettingsToPreferences();
    }
    public void OnNdsResolutionToggle() {
        Settings s = Settings.Instance;
        s.ndsResolution = ndsResolutionToggle.isOn;
        s.SaveSettingsToPreferences();
    }
    public void OnFullscreenToggle() {
        bool value = fullscreenToggle.isOn;
        if (value) {
            prevWidth = Screen.width;
            prevHeight = Screen.height;
            Resolution max = Screen.resolutions[Screen.resolutions.Length-1];
            Screen.SetResolution(max.width, max.height, FullScreenMode.FullScreenWindow);
        } else {
            Screen.SetResolution(prevWidth, prevHeight, FullScreenMode.Windowed);
        }
    }
}
