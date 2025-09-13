using OpenMetaverse;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Collections;
using System;
using System.IO;

public class MarketplaceIntegration : MonoBehaviour
{
    [Header("Marketplace Window")]
    public GameObject marketplaceWindow;
    public Button closeButton;
    public Button refreshButton;
    public TMP_InputField searchField;
    public Button searchButton;
    public TMP_Dropdown categoryDropdown;
    public TMP_Dropdown sortDropdown;
    
    [Header("Product Display")]
    public Transform productGridRoot;
    public GameObject productItemPrefab;
    public ScrollRect productScrollRect;
    
    [Header("Product Details")]
    public GameObject productDetailsPanel;
    public RawImage productImage;
    public TMP_Text productNameText;
    public TMP_Text productDescriptionText;
    public TMP_Text productPriceText;
    public TMP_Text sellerNameText;
    public Button buyButton;
    public Button addToCartButton;
    public Button favoriteButton;
    public Transform reviewsRoot;
    public GameObject reviewItemPrefab;
    
    [Header("Shopping Cart")]
    public GameObject shoppingCartPanel;
    public Transform cartItemsRoot;
    public GameObject cartItemPrefab;
    public TMP_Text cartTotalText;
    public Button checkoutButton;
    public Button clearCartButton;
    
    [Header("My Purchases")]
    public GameObject purchasesPanel;
    public Transform purchasesRoot;
    public GameObject purchaseItemPrefab;
    public Button redeliverButton;
    
    [Header("Pagination")]
    public Button prevPageButton;
    public Button nextPageButton;
    public TMP_Text pageInfoText;
    
    private GridClient client;
    private List<MarketplaceProduct> currentProducts = new();
    private List<MarketplaceProduct> shoppingCart = new();
    private List<MarketplacePurchase> myPurchases = new();
    private MarketplaceProduct selectedProduct;
    private int currentPage = 0;
    private int productsPerPage = 20;
    private string currentSearchTerm = "";
    private int currentCategory = 0;
    private int currentSort = 0;
    
    public class MarketplaceProduct
    {
        public UUID productID;
        public string name;
        public string description;
        public int price;
        public string sellerName;
        public UUID sellerID;
        public string category;
        public string imageUrl;
        public Texture2D image;
        public float rating;
        public int reviewCount;
        public bool isFavorite;
        public System.DateTime dateAdded;
        public List<MarketplaceReview> reviews = new();
    }
    
    public class MarketplaceReview
    {
        public UUID reviewerID;
        public string reviewerName;
        public float rating;
        public string comment;
        public System.DateTime date;
    }
    
    public class MarketplacePurchase
    {
        public UUID transactionID;
        public UUID productID;
        public string productName;
        public int pricePaid;
        public System.DateTime purchaseDate;
        public bool delivered;
        public string status;
    }

    void Awake()
    {
        marketplaceWindow.SetActive(false);
        SetupUI();
    }

    void SetupUI()
    {
        if (closeButton) closeButton.onClick.AddListener(() => marketplaceWindow.SetActive(false));
        if (refreshButton) refreshButton.onClick.AddListener(RefreshProducts);
        if (searchButton) searchButton.onClick.AddListener(SearchProducts);
        if (searchField) searchField.onEndEdit.AddListener((text) => { if (Input.GetKeyDown(KeyCode.Return)) SearchProducts(); });
        
        if (categoryDropdown) categoryDropdown.onValueChanged.AddListener(OnCategoryChanged);
        if (sortDropdown) sortDropdown.onValueChanged.AddListener(OnSortChanged);
        
        // Product details
        if (buyButton) buyButton.onClick.AddListener(BuyProduct);
        if (addToCartButton) addToCartButton.onClick.AddListener(AddToCart);
        if (favoriteButton) favoriteButton.onClick.AddListener(ToggleFavorite);
        
        // Shopping cart
        if (checkoutButton) checkoutButton.onClick.AddListener(Checkout);
        if (clearCartButton) clearCartButton.onClick.AddListener(ClearCart);
        
        // Purchases
        if (redeliverButton) redeliverButton.onClick.AddListener(RedeliverPurchase);
        
        // Pagination
        if (prevPageButton) prevPageButton.onClick.AddListener(PreviousPage);
        if (nextPageButton) nextPageButton.onClick.AddListener(NextPage);
        
        // Setup dropdowns
        SetupDropdowns();
    }

    void SetupDropdowns()
    {
        // Category dropdown
        if (categoryDropdown)
        {
            categoryDropdown.options.Clear();
            categoryDropdown.options.Add(new TMP_Dropdown.OptionData("All Categories"));
            categoryDropdown.options.Add(new TMP_Dropdown.OptionData("Animations"));
            categoryDropdown.options.Add(new TMP_Dropdown.OptionData("Avatars"));
            categoryDropdown.options.Add(new TMP_Dropdown.OptionData("Clothing"));
            categoryDropdown.options.Add(new TMP_Dropdown.OptionData("Furniture"));
            categoryDropdown.options.Add(new TMP_Dropdown.OptionData("Gadgets"));
            categoryDropdown.options.Add(new TMP_Dropdown.OptionData("Hair"));
            categoryDropdown.options.Add(new TMP_Dropdown.OptionData("Home & Garden"));
            categoryDropdown.options.Add(new TMP_Dropdown.OptionData("Jewelry"));
            categoryDropdown.options.Add(new TMP_Dropdown.OptionData("Land"));
            categoryDropdown.options.Add(new TMP_Dropdown.OptionData("Scripts"));
            categoryDropdown.options.Add(new TMP_Dropdown.OptionData("Shapes"));
            categoryDropdown.options.Add(new TMP_Dropdown.OptionData("Skins"));
            categoryDropdown.options.Add(new TMP_Dropdown.OptionData("Sounds"));
            categoryDropdown.options.Add(new TMP_Dropdown.OptionData("Textures"));
            categoryDropdown.options.Add(new TMP_Dropdown.OptionData("Vehicles"));
            categoryDropdown.RefreshShownValue();
        }
        
        // Sort dropdown
        if (sortDropdown)
        {
            sortDropdown.options.Clear();
            sortDropdown.options.Add(new TMP_Dropdown.OptionData("Relevance"));
            sortDropdown.options.Add(new TMP_Dropdown.OptionData("Price: Low to High"));
            sortDropdown.options.Add(new TMP_Dropdown.OptionData("Price: High to Low"));
            sortDropdown.options.Add(new TMP_Dropdown.OptionData("Newest First"));
            sortDropdown.options.Add(new TMP_Dropdown.OptionData("Best Selling"));
            sortDropdown.options.Add(new TMP_Dropdown.OptionData("Highest Rated"));
            sortDropdown.RefreshShownValue();
        }
    }

    void Start()
    {
        client = ClientManager.client;
        LoadFavorites();
        LoadPurchaseHistory();
    }

    public void ShowMarketplace()
    {
        marketplaceWindow.SetActive(true);
        RefreshProducts();
    }

    void RefreshProducts()
    {
        StartCoroutine(FetchProducts());
    }

    IEnumerator FetchProducts()
    {
        // In a real implementation, this would query the SL Marketplace API
        // For demonstration, we'll create some mock products
        
        currentProducts.Clear();
        
        // Simulate API delay
        yield return new WaitForSeconds(0.5f);
        
        // Generate mock products
        GenerateMockProducts();
        
        // Apply filtering and sorting
        FilterAndSortProducts();
        
        // Update display
        DisplayProducts();
    }

    void GenerateMockProducts()
    {
        string[] categories = { "Clothing", "Furniture", "Avatars", "Hair", "Shapes", "Animations" };
        string[] adjectives = { "Elegant", "Modern", "Vintage", "Stylish", "Luxury", "Premium", "Classic", "Trendy" };
        string[] nouns = { "Dress", "Chair", "Avatar", "Hairstyle", "Shape", "Dance", "Shirt", "Sofa", "Skin", "Animation" };
        
        for (int i = 0; i < 50; i++)
        {
            var product = new MarketplaceProduct
            {
                productID = UUID.Random(),
                name = $"{adjectives[UnityEngine.Random.Range(0, adjectives.Length)]} {nouns[UnityEngine.Random.Range(0, nouns.Length)]}",
                description = "High quality product with amazing features and excellent craftsmanship.",
                price = UnityEngine.Random.Range(50, 2000),
                sellerName = $"Seller{UnityEngine.Random.Range(1, 100)}",
                sellerID = UUID.Random(),
                category = categories[UnityEngine.Random.Range(0, categories.Length)],
                rating = UnityEngine.Random.Range(3.0f, 5.0f),
                reviewCount = UnityEngine.Random.Range(0, 100),
                dateAdded = System.DateTime.Now.AddDays(-UnityEngine.Random.Range(0, 365))
            };
            
            currentProducts.Add(product);
        }
    }

    void FilterAndSortProducts()
    {
        var filteredProducts = currentProducts;
        
        // Apply category filter
        if (currentCategory > 0 && categoryDropdown != null)
        {
            string selectedCategory = categoryDropdown.options[currentCategory].text;
            filteredProducts = filteredProducts.FindAll(p => p.category == selectedCategory);
        }
        
        // Apply search filter
        if (!string.IsNullOrEmpty(currentSearchTerm))
        {
            string search = currentSearchTerm.ToLower();
            filteredProducts = filteredProducts.FindAll(p => 
                p.name.ToLower().Contains(search) || 
                p.description.ToLower().Contains(search) ||
                p.sellerName.ToLower().Contains(search));
        }
        
        // Apply sorting
        switch (currentSort)
        {
            case 0: // Relevance
                break;
            case 1: // Price: Low to High
                filteredProducts.Sort((a, b) => a.price.CompareTo(b.price));
                break;
            case 2: // Price: High to Low
                filteredProducts.Sort((a, b) => b.price.CompareTo(a.price));
                break;
            case 3: // Newest First
                filteredProducts.Sort((a, b) => b.dateAdded.CompareTo(a.dateAdded));
                break;
            case 4: // Best Selling
                filteredProducts.Sort((a, b) => b.reviewCount.CompareTo(a.reviewCount));
                break;
            case 5: // Highest Rated
                filteredProducts.Sort((a, b) => b.rating.CompareTo(a.rating));
                break;
        }
        
        currentProducts = filteredProducts;
    }

    void DisplayProducts()
    {
        // Clear existing products
        foreach (Transform child in productGridRoot)
        {
            Destroy(child.gameObject);
        }
        
        // Calculate pagination
        int startIndex = currentPage * productsPerPage;
        int endIndex = Mathf.Min(startIndex + productsPerPage, currentProducts.Count);
        
        // Create product items
        for (int i = startIndex; i < endIndex; i++)
        {
            CreateProductItem(currentProducts[i]);
        }
        
        UpdatePaginationInfo();
    }

    void CreateProductItem(MarketplaceProduct product)
    {
        if (productItemPrefab == null) return;
        
        var productObj = Instantiate(productItemPrefab, productGridRoot);
        var nameText = productObj.transform.Find("NameText")?.GetComponent<TMP_Text>();
        var priceText = productObj.transform.Find("PriceText")?.GetComponent<TMP_Text>();
        var sellerText = productObj.transform.Find("SellerText")?.GetComponent<TMP_Text>();
        var ratingText = productObj.transform.Find("RatingText")?.GetComponent<TMP_Text>();
        var productImage = productObj.transform.Find("ProductImage")?.GetComponent<RawImage>();
        var button = productObj.GetComponent<Button>();
        
        if (nameText) nameText.text = product.name;
        if (priceText) priceText.text = $"L${product.price}";
        if (sellerText) sellerText.text = $"by {product.sellerName}";
        if (ratingText) ratingText.text = $"★ {product.rating:F1} ({product.reviewCount})";
        
        // Load product image
        if (productImage && !string.IsNullOrEmpty(product.imageUrl))
        {
            StartCoroutine(LoadProductImage(product, productImage));
        }
        
        if (button)
        {
            button.onClick.AddListener(() => ShowProductDetails(product));
        }
    }

    IEnumerator LoadProductImage(MarketplaceProduct product, RawImage imageComponent)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(product.imageUrl))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                product.image = ((DownloadHandlerTexture)request.downloadHandler).texture;
                imageComponent.texture = product.image;
            }
        }
    }

    void ShowProductDetails(MarketplaceProduct product)
    {
        selectedProduct = product;
        
        if (productDetailsPanel) productDetailsPanel.SetActive(true);
        
        if (productNameText) productNameText.text = product.name;
        if (productDescriptionText) productDescriptionText.text = product.description;
        if (productPriceText) productPriceText.text = $"L${product.price}";
        if (sellerNameText) sellerNameText.text = $"Sold by: {product.sellerName}";
        
        if (productImage && product.image)
        {
            productImage.texture = product.image;
        }
        
        // Update favorite button
        if (favoriteButton)
        {
            var buttonText = favoriteButton.GetComponentInChildren<TMP_Text>();
            if (buttonText)
            {
                buttonText.text = product.isFavorite ? "★ Remove from Favorites" : "☆ Add to Favorites";
            }
        }
        
        // Load reviews
        LoadProductReviews(product);
    }

    void LoadProductReviews(MarketplaceProduct product)
    {
        // Clear existing reviews
        foreach (Transform child in reviewsRoot)
        {
            Destroy(child.gameObject);
        }
        
        // Generate mock reviews
        for (int i = 0; i < UnityEngine.Random.Range(3, 8); i++)
        {
            var review = new MarketplaceReview
            {
                reviewerName = $"User{UnityEngine.Random.Range(1, 1000)}",
                rating = UnityEngine.Random.Range(3.0f, 5.0f),
                comment = "Great product! Highly recommended.",
                date = System.DateTime.Now.AddDays(-UnityEngine.Random.Range(1, 180))
            };
            
            CreateReviewItem(review);
        }
    }

    void CreateReviewItem(MarketplaceReview review)
    {
        if (reviewItemPrefab == null) return;
        
        var reviewObj = Instantiate(reviewItemPrefab, reviewsRoot);
        var nameText = reviewObj.transform.Find("ReviewerName")?.GetComponent<TMP_Text>();
        var ratingText = reviewObj.transform.Find("Rating")?.GetComponent<TMP_Text>();
        var commentText = reviewObj.transform.Find("Comment")?.GetComponent<TMP_Text>();
        var dateText = reviewObj.transform.Find("Date")?.GetComponent<TMP_Text>();
        
        if (nameText) nameText.text = review.reviewerName;
        if (ratingText) ratingText.text = $"★ {review.rating:F1}";
        if (commentText) commentText.text = review.comment;
        if (dateText) dateText.text = review.date.ToString("MMM dd, yyyy");
    }

    #region Shopping Cart

    void AddToCart()
    {
        if (selectedProduct == null) return;
        
        if (!shoppingCart.Contains(selectedProduct))
        {
            shoppingCart.Add(selectedProduct);
            UpdateCartDisplay();
            
            Debug.Log($"Added {selectedProduct.name} to cart");
        }
        else
        {
            Debug.Log("Product already in cart");
        }
    }

    void UpdateCartDisplay()
    {
        // Clear existing cart items
        foreach (Transform child in cartItemsRoot)
        {
            Destroy(child.gameObject);
        }
        
        int totalPrice = 0;
        
        // Create cart items
        foreach (var product in shoppingCart)
        {
            CreateCartItem(product);
            totalPrice += product.price;
        }
        
        if (cartTotalText) cartTotalText.text = $"Total: L${totalPrice}";
    }

    void CreateCartItem(MarketplaceProduct product)
    {
        if (cartItemPrefab == null) return;
        
        var cartObj = Instantiate(cartItemPrefab, cartItemsRoot);
        var nameText = cartObj.GetComponentInChildren<TMP_Text>();
        var removeButton = cartObj.GetComponentInChildren<Button>();
        
        if (nameText) nameText.text = $"{product.name} - L${product.price}";
        
        if (removeButton)
        {
            removeButton.onClick.AddListener(() =>
            {
                shoppingCart.Remove(product);
                UpdateCartDisplay();
            });
        }
    }

    void ClearCart()
    {
        shoppingCart.Clear();
        UpdateCartDisplay();
    }

    void Checkout()
    {
        if (shoppingCart.Count == 0) return;
        
        int totalPrice = shoppingCart.Sum(p => p.price);
        
        // In a real implementation, this would process payment
        Debug.Log($"Processing checkout for L${totalPrice}");
        
        // Simulate purchase
        foreach (var product in shoppingCart)
        {
            var purchase = new MarketplacePurchase
            {
                transactionID = UUID.Random(),
                productID = product.productID,
                productName = product.name,
                pricePaid = product.price,
                purchaseDate = System.DateTime.Now,
                delivered = true,
                status = "Completed"
            };
            
            myPurchases.Add(purchase);
        }
        
        ClearCart();
        SavePurchaseHistory();
        
        Debug.Log("Purchase completed successfully!");
    }

    #endregion

    void BuyProduct()
    {
        if (selectedProduct == null) return;
        
        // Direct purchase
        Debug.Log($"Buying {selectedProduct.name} for L${selectedProduct.price}");
        
        var purchase = new MarketplacePurchase
        {
            transactionID = UUID.Random(),
            productID = selectedProduct.productID,
            productName = selectedProduct.name,
            pricePaid = selectedProduct.price,
            purchaseDate = System.DateTime.Now,
            delivered = true,
            status = "Completed"
        };
        
        myPurchases.Add(purchase);
        SavePurchaseHistory();
        
        Debug.Log("Purchase completed!");
    }

    void ToggleFavorite()
    {
        if (selectedProduct == null) return;
        
        selectedProduct.isFavorite = !selectedProduct.isFavorite;
        
        // Update button text
        if (favoriteButton)
        {
            var buttonText = favoriteButton.GetComponentInChildren<TMP_Text>();
            if (buttonText)
            {
                buttonText.text = selectedProduct.isFavorite ? "★ Remove from Favorites" : "☆ Add to Favorites";
            }
        }
        
        SaveFavorites();
    }

    void SearchProducts()
    {
        if (searchField == null) return;
        
        currentSearchTerm = searchField.text;
        currentPage = 0;
        RefreshProducts();
    }

    void OnCategoryChanged(int value)
    {
        currentCategory = value;
        currentPage = 0;
        RefreshProducts();
    }

    void OnSortChanged(int value)
    {
        currentSort = value;
        RefreshProducts();
    }

    void UpdatePaginationInfo()
    {
        if (pageInfoText)
        {
            int totalPages = Mathf.CeilToInt((float)currentProducts.Count / productsPerPage);
            pageInfoText.text = $"Page {currentPage + 1} of {totalPages} ({currentProducts.Count} products)";
        }
        
        if (prevPageButton) prevPageButton.interactable = currentPage > 0;
        if (nextPageButton) nextPageButton.interactable = (currentPage + 1) * productsPerPage < currentProducts.Count;
    }

    void PreviousPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            DisplayProducts();
        }
    }

    void NextPage()
    {
        if ((currentPage + 1) * productsPerPage < currentProducts.Count)
        {
            currentPage++;
            DisplayProducts();
        }
    }

    void RedeliverPurchase()
    {
        // Redeliver selected purchase
        Debug.Log("Redelivering purchase");
    }

    #region Data Persistence

    void SaveFavorites()
    {
        var favorites = currentProducts.FindAll(p => p.isFavorite);
        var favoriteIDs = favorites.ConvertAll(f => f.productID.ToString());
        
        string json = JsonUtility.ToJson(new Serialization<string>(favoriteIDs));
        string path = Path.Combine(Application.persistentDataPath, "MarketplaceFavorites.json");
        
        try
        {
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to save favorites: {ex.Message}");
        }
    }

    void LoadFavorites()
    {
        string path = Path.Combine(Application.persistentDataPath, "MarketplaceFavorites.json");
        
        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);
                var favoriteData = JsonUtility.FromJson<Serialization<string>>(json);
                
                foreach (var idString in favoriteData.ToArray())
                {
                    if (UUID.TryParse(idString, out UUID favoriteID))
                    {
                        var product = currentProducts.Find(p => p.productID == favoriteID);
                        if (product != null)
                        {
                            product.isFavorite = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load favorites: {ex.Message}");
            }
        }
    }

    void SavePurchaseHistory()
    {
        string json = JsonUtility.ToJson(new Serialization<MarketplacePurchase>(myPurchases));
        string path = Path.Combine(Application.persistentDataPath, "MarketplacePurchases.json");
        
        try
        {
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to save purchase history: {ex.Message}");
        }
    }

    void LoadPurchaseHistory()
    {
        string path = Path.Combine(Application.persistentDataPath, "MarketplacePurchases.json");
        
        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);
                var purchaseData = JsonUtility.FromJson<Serialization<MarketplacePurchase>>(json);
                myPurchases = purchaseData.ToList();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load purchase history: {ex.Message}");
            }
        }
    }

    #endregion

    // Helper class for JSON serialization of lists
    [System.Serializable]
    public class Serialization<T>
    {
        public T[] items;
        
        public Serialization(List<T> list)
        {
            items = list.ToArray();
        }
        
        public List<T> ToList()
        {
            return new List<T>(items);
        }
        
        public T[] ToArray()
        {
            return items;
        }
    }

    // Public methods for external integration
    public void ShowProductsByCategory(string category)
    {
        currentSearchTerm = "";
        
        if (categoryDropdown && searchField)
        {
            searchField.text = "";
            
            for (int i = 0; i < categoryDropdown.options.Count; i++)
            {
                if (categoryDropdown.options[i].text == category)
                {
                    categoryDropdown.value = i;
                    break;
                }
            }
        }
        
        ShowMarketplace();
    }

    public void SearchForProduct(string searchTerm)
    {
        if (searchField)
        {
            searchField.text = searchTerm;
            SearchProducts();
        }
        
        ShowMarketplace();
    }
}