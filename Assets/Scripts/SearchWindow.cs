using OpenMetaverse;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class SearchWindow : MonoBehaviour
{
    [Header("Window Management")]
    public GameObject searchWindow;
    public Button closeButton;
    
    [Header("Search Tabs")]
    public Button allTabButton;
    public Button peopleTabButton;
    public Button placesTabButton;
    public Button groupsTabButton;
    public Button eventsTabButton;
    
    [Header("Search Input")]
    public TMP_InputField searchField;
    public Button searchButton;
    public TMP_Dropdown categoryDropdown;
    public Toggle matureContentToggle;
    
    [Header("Results Display")]
    public Transform resultsRoot;
    public GameObject peopleResultPrefab;
    public GameObject placeResultPrefab;
    public GameObject groupResultPrefab;
    public GameObject eventResultPrefab;
    public ScrollRect resultsScrollRect;
    
    [Header("Pagination")]
    public Button prevPageButton;
    public Button nextPageButton;
    public TMP_Text pageInfoText;
    
    [Header("Details Panel")]
    public GameObject detailsPanel;
    public TMP_Text detailsTitle;
    public TMP_Text detailsDescription;
    public Button actionButton; // IM, Teleport, Join, etc.
    public RawImage detailsImage;
    
    private GridClient client;
    private SearchType currentSearchType = SearchType.All;
    private List<SearchResult> currentResults = new();
    private int currentPage = 0;
    private int resultsPerPage = 20;
    private SearchResult selectedResult;
    
    public enum SearchType
    {
        All,
        People,
        Places,
        Groups,
        Events
    }
    
    [System.Serializable]
    public class SearchResult
    {
        public SearchType type;
        public UUID id;
        public string name;
        public string description;
        public Vector3 position;
        public string ownerName;
        public bool forSale;
        public int price;
        public bool mature;
        public Texture2D image;
        
        // For people
        public bool online;
        
        // For groups
        public int memberCount;
        public UUID groupID;
        
        // For events
        public System.DateTime eventDate;
        public string category;
    }

    void Awake()
    {
        searchWindow.SetActive(false);
        SetupUI();
    }

    void SetupUI()
    {
        if (closeButton) closeButton.onClick.AddListener(() => searchWindow.SetActive(false));
        if (searchButton) searchButton.onClick.AddListener(PerformSearch);
        if (searchField) searchField.onEndEdit.AddListener((text) => { if (Input.GetKeyDown(KeyCode.Return)) PerformSearch(); });
        
        // Setup tab buttons
        if (allTabButton) allTabButton.onClick.AddListener(() => SwitchSearchType(SearchType.All));
        if (peopleTabButton) peopleTabButton.onClick.AddListener(() => SwitchSearchType(SearchType.People));
        if (placesTabButton) placesTabButton.onClick.AddListener(() => SwitchSearchType(SearchType.Places));
        if (groupsTabButton) groupsTabButton.onClick.AddListener(() => SwitchSearchType(SearchType.Groups));
        if (eventsTabButton) eventsTabButton.onClick.AddListener(() => SwitchSearchType(SearchType.Events));
        
        // Setup pagination
        if (prevPageButton) prevPageButton.onClick.AddListener(PreviousPage);
        if (nextPageButton) nextPageButton.onClick.AddListener(NextPage);
        
        // Setup action button
        if (actionButton) actionButton.onClick.AddListener(PerformAction);
        
        // Setup category dropdown
        if (categoryDropdown)
        {
            categoryDropdown.options.Clear();
            categoryDropdown.options.Add(new TMP_Dropdown.OptionData("Any Category"));
            categoryDropdown.options.Add(new TMP_Dropdown.OptionData("Shopping"));
            categoryDropdown.options.Add(new TMP_Dropdown.OptionData("Hangout"));
            categoryDropdown.options.Add(new TMP_Dropdown.OptionData("Parks & Nature"));
            categoryDropdown.options.Add(new TMP_Dropdown.OptionData("Residential"));
            categoryDropdown.options.Add(new TMP_Dropdown.OptionData("Arts & Culture"));
            categoryDropdown.options.Add(new TMP_Dropdown.OptionData("Business"));
            categoryDropdown.options.Add(new TMP_Dropdown.OptionData("Educational"));
            categoryDropdown.options.Add(new TMP_Dropdown.OptionData("Gaming"));
            categoryDropdown.options.Add(new TMP_Dropdown.OptionData("Other"));
        }
        
        SwitchSearchType(SearchType.All);
    }

    void Start()
    {
        client = ClientManager.client;
        
        if (client != null)
        {
            client.Directory.DirPeopleReply += OnDirPeopleReply;
            client.Directory.DirPlacesReply += OnDirPlacesReply;
            client.Directory.DirGroupsReply += OnDirGroupsReply;
            client.Directory.DirEventsReply += OnDirEventsReply;
        }
    }

    void OnDestroy()
    {
        if (client != null)
        {
            client.Directory.DirPeopleReply -= OnDirPeopleReply;
            client.Directory.DirPlacesReply -= OnDirPlacesReply;
            client.Directory.DirGroupsReply -= OnDirGroupsReply;
            client.Directory.DirEventsReply -= OnDirEventsReply;
        }
    }

    public void ShowSearchWindow()
    {
        searchWindow.SetActive(true);
        if (searchField) searchField.Select();
    }

    void SwitchSearchType(SearchType searchType)
    {
        currentSearchType = searchType;
        
        // Update tab button states
        UpdateTabButtonStates();
        
        // Clear current results
        ClearResults();
        
        // Update action button text based on search type
        UpdateActionButtonText();
    }

    void UpdateTabButtonStates()
    {
        // This would update the visual state of tab buttons
        // Implementation depends on your UI design
    }

    void UpdateActionButtonText()
    {
        if (actionButton == null) return;
        
        var buttonText = actionButton.GetComponentInChildren<TMP_Text>();
        if (buttonText == null) return;
        
        switch (currentSearchType)
        {
            case SearchType.People:
                buttonText.text = "Send IM";
                break;
            case SearchType.Places:
                buttonText.text = "Teleport";
                break;
            case SearchType.Groups:
                buttonText.text = "Join Group";
                break;
            case SearchType.Events:
                buttonText.text = "Teleport";
                break;
            default:
                buttonText.text = "Action";
                break;
        }
    }

    void PerformSearch()
    {
        if (searchField == null || string.IsNullOrEmpty(searchField.text)) return;
        
        string searchTerm = searchField.text.Trim();
        currentPage = 0;
        ClearResults();
        
        switch (currentSearchType)
        {
            case SearchType.All:
                SearchAll(searchTerm);
                break;
            case SearchType.People:
                SearchPeople(searchTerm);
                break;
            case SearchType.Places:
                SearchPlaces(searchTerm);
                break;
            case SearchType.Groups:
                SearchGroups(searchTerm);
                break;
            case SearchType.Events:
                SearchEvents(searchTerm);
                break;
        }
    }

    void SearchAll(string searchTerm)
    {
        // Search all categories
        SearchPeople(searchTerm);
        SearchPlaces(searchTerm);
        SearchGroups(searchTerm);
        SearchEvents(searchTerm);
    }

    void SearchPeople(string searchTerm)
    {
        if (client == null) return;
        
        client.Directory.StartPeopleSearch(searchTerm, 0);
    }

    void SearchPlaces(string searchTerm)
    {
        if (client == null) return;
        
        DirectoryManager.DirFindFlags flags = DirectoryManager.DirFindFlags.DwellSort;
        if (matureContentToggle && matureContentToggle.isOn)
        {
            flags |= DirectoryManager.DirFindFlags.IncludeMature;
        }
        
        DirectoryManager.SearchTypeFlags category = DirectoryManager.SearchTypeFlags.Any;
        if (categoryDropdown && categoryDropdown.value > 0)
        {
            category = (DirectoryManager.SearchTypeFlags)(1 << (categoryDropdown.value - 1));
        }
        
        client.Directory.StartDirPlacesSearch(searchTerm, flags, category, 0);
    }

    void SearchGroups(string searchTerm)
    {
        if (client == null) return;
        
        client.Directory.StartGroupSearch(searchTerm, 0);
    }

    void SearchEvents(string searchTerm)
    {
        if (client == null) return;
        
        DirectoryManager.DirFindFlags flags = DirectoryManager.DirFindFlags.DateEvents;
        if (matureContentToggle && matureContentToggle.isOn)
        {
            flags |= DirectoryManager.DirFindFlags.IncludeMature;
        }
        
        client.Directory.StartEventsSearch(searchTerm, flags, "u", 0, System.DateTime.Now, 0);
    }

    #region Event Handlers

    void OnDirPeopleReply(object sender, DirPeopleReplyEventArgs e)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            foreach (var person in e.MatchedPeople)
            {
                var result = new SearchResult
                {
                    type = SearchType.People,
                    id = person.AgentID,
                    name = person.FirstName + " " + person.LastName,
                    online = person.Online
                };
                
                currentResults.Add(result);
            }
            
            UpdateResultsDisplay();
        });
    }

    void OnDirPlacesReply(object sender, DirPlacesReplyEventArgs e)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            foreach (var place in e.MatchedPlaces)
            {
                var result = new SearchResult
                {
                    type = SearchType.Places,
                    id = place.ParcelID,
                    name = place.Name,
                    description = place.Description,
                    forSale = place.SalePrice > 0,
                    price = place.SalePrice,
                    mature = place.Mature
                };
                
                currentResults.Add(result);
            }
            
            UpdateResultsDisplay();
        });
    }

    void OnDirGroupsReply(object sender, DirGroupsReplyEventArgs e)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            foreach (var group in e.MatchedGroups)
            {
                var result = new SearchResult
                {
                    type = SearchType.Groups,
                    id = group.GroupID,
                    groupID = group.GroupID,
                    name = group.GroupName,
                    memberCount = group.Members
                };
                
                currentResults.Add(result);
            }
            
            UpdateResultsDisplay();
        });
    }

    void OnDirEventsReply(object sender, DirEventsReplyEventArgs e)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            foreach (var evt in e.MatchedEvents)
            {
                var result = new SearchResult
                {
                    type = SearchType.Events,
                    id = evt.EventID,
                    name = evt.Name,
                    description = evt.Desc,
                    eventDate = evt.Date,
                    category = evt.Category,
                    mature = evt.Mature
                };
                
                currentResults.Add(result);
            }
            
            UpdateResultsDisplay();
        });
    }

    #endregion

    void UpdateResultsDisplay()
    {
        ClearResultsDisplay();
        
        int startIndex = currentPage * resultsPerPage;
        int endIndex = Mathf.Min(startIndex + resultsPerPage, currentResults.Count);
        
        for (int i = startIndex; i < endIndex; i++)
        {
            CreateResultItem(currentResults[i]);
        }
        
        UpdatePaginationInfo();
    }

    void ClearResults()
    {
        currentResults.Clear();
        ClearResultsDisplay();
    }

    void ClearResultsDisplay()
    {
        foreach (Transform child in resultsRoot)
        {
            Destroy(child.gameObject);
        }
    }

    void CreateResultItem(SearchResult result)
    {
        GameObject prefab = GetResultPrefab(result.type);
        if (prefab == null) return;
        
        var resultObj = Instantiate(prefab, resultsRoot);
        var button = resultObj.GetComponent<Button>();
        
        // Setup result display based on type
        switch (result.type)
        {
            case SearchType.People:
                SetupPersonResult(resultObj, result);
                break;
            case SearchType.Places:
                SetupPlaceResult(resultObj, result);
                break;
            case SearchType.Groups:
                SetupGroupResult(resultObj, result);
                break;
            case SearchType.Events:
                SetupEventResult(resultObj, result);
                break;
        }
        
        if (button)
        {
            button.onClick.AddListener(() => SelectResult(result));
        }
    }

    void SetupPersonResult(GameObject resultObj, SearchResult result)
    {
        var nameText = resultObj.transform.Find("NameText")?.GetComponent<TMP_Text>();
        var statusText = resultObj.transform.Find("StatusText")?.GetComponent<TMP_Text>();
        
        if (nameText) nameText.text = result.name;
        if (statusText) statusText.text = result.online ? "Online" : "Offline";
    }

    void SetupPlaceResult(GameObject resultObj, SearchResult result)
    {
        var nameText = resultObj.transform.Find("NameText")?.GetComponent<TMP_Text>();
        var descText = resultObj.transform.Find("DescriptionText")?.GetComponent<TMP_Text>();
        var priceText = resultObj.transform.Find("PriceText")?.GetComponent<TMP_Text>();
        
        if (nameText) nameText.text = result.name;
        if (descText) descText.text = result.description;
        
        if (priceText)
        {
            if (result.forSale)
                priceText.text = $"L${result.price}";
            else
                priceText.text = "Not for sale";
        }
    }

    void SetupGroupResult(GameObject resultObj, SearchResult result)
    {
        var nameText = resultObj.transform.Find("NameText")?.GetComponent<TMP_Text>();
        var memberText = resultObj.transform.Find("MemberText")?.GetComponent<TMP_Text>();
        
        if (nameText) nameText.text = result.name;
        if (memberText) memberText.text = $"{result.memberCount} members";
    }

    void SetupEventResult(GameObject resultObj, SearchResult result)
    {
        var nameText = resultObj.transform.Find("NameText")?.GetComponent<TMP_Text>();
        var dateText = resultObj.transform.Find("DateText")?.GetComponent<TMP_Text>();
        var categoryText = resultObj.transform.Find("CategoryText")?.GetComponent<TMP_Text>();
        
        if (nameText) nameText.text = result.name;
        if (dateText) dateText.text = result.eventDate.ToString("MMM dd, yyyy");
        if (categoryText) categoryText.text = result.category;
    }

    GameObject GetResultPrefab(SearchType type)
    {
        switch (type)
        {
            case SearchType.People: return peopleResultPrefab;
            case SearchType.Places: return placeResultPrefab;
            case SearchType.Groups: return groupResultPrefab;
            case SearchType.Events: return eventResultPrefab;
            default: return peopleResultPrefab;
        }
    }

    void SelectResult(SearchResult result)
    {
        selectedResult = result;
        ShowResultDetails(result);
    }

    void ShowResultDetails(SearchResult result)
    {
        if (detailsPanel) detailsPanel.SetActive(true);
        if (detailsTitle) detailsTitle.text = result.name;
        if (detailsDescription) detailsDescription.text = result.description;
        
        UpdateActionButtonText();
    }

    void PerformAction()
    {
        if (selectedResult == null) return;
        
        switch (selectedResult.type)
        {
            case SearchType.People:
                StartIM(selectedResult.id);
                break;
            case SearchType.Places:
                TeleportToPlace(selectedResult);
                break;
            case SearchType.Groups:
                JoinGroup(selectedResult.groupID);
                break;
            case SearchType.Events:
                TeleportToPlace(selectedResult);
                break;
        }
    }

    void StartIM(UUID agentID)
    {
        if (ClientManager.chat != null)
        {
            ClientManager.chat.StartIM(agentID);
            ClientManager.chatWindow?.SwitchToIM(agentID);
        }
    }

    void TeleportToPlace(SearchResult result)
    {
        if (client == null) return;
        
        // This would need more detailed place information to teleport
        Debug.Log($"Teleporting to {result.name}");
    }

    void JoinGroup(UUID groupID)
    {
        if (client == null) return;
        
        client.Groups.RequestJoinGroup(groupID);
        Debug.Log($"Requesting to join group {groupID}");
    }

    void UpdatePaginationInfo()
    {
        if (pageInfoText)
        {
            int totalPages = Mathf.CeilToInt((float)currentResults.Count / resultsPerPage);
            pageInfoText.text = $"Page {currentPage + 1} of {totalPages} ({currentResults.Count} results)";
        }
        
        if (prevPageButton) prevPageButton.interactable = currentPage > 0;
        if (nextPageButton) nextPageButton.interactable = (currentPage + 1) * resultsPerPage < currentResults.Count;
    }

    void PreviousPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            UpdateResultsDisplay();
        }
    }

    void NextPage()
    {
        if ((currentPage + 1) * resultsPerPage < currentResults.Count)
        {
            currentPage++;
            UpdateResultsDisplay();
        }
    }
}