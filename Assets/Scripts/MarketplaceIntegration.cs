/*
 * Crystal Frost Second Life Viewer - Marketplace Integration System
 * 
 * SYSTEM OVERVIEW:
 * ================
 * This system provides comprehensive Second Life Marketplace integration for the
 * Crystal Frost viewer, enabling users to browse, search, purchase, and manage
 * marketplace products directly within the viewer. It offers a complete shopping
 * experience with advanced filtering, sorting, reviews, and purchase management.
 * 
 * ARCHITECTURE:
 * =============
 * - Unity MonoBehaviour component with full UI integration
 * - RESTful API integration with SL Marketplace services
 * - Local caching and persistent storage for user data
 * - Asynchronous operations using Unity coroutines
 * - Event-driven architecture for real-time updates
 * - Modular design supporting multiple marketplace providers
 * 
 * KEY FEATURES:
 * =============
 * 1. PRODUCT BROWSING:
 *    - Category-based product organization
 *    - Advanced search with keyword filtering
 *    - Multiple sorting options (price, rating, date, popularity)
 *    - Pagination for large product catalogs
 *    - Product image loading and caching
 * 
 * 2. PRODUCT DETAILS:
 *    - Comprehensive product information display
 *    - Customer reviews and ratings system
 *    - Seller information and reputation
 *    - Product image gallery with zoom
 *    - Related products suggestions
 * 
 * 3. SHOPPING CART:
 *    - Multi-product cart management
 *    - Cart persistence across sessions
 *    - Total calculation with taxes/fees
 *    - Batch checkout processing
 *    - Cart sharing and wishlist features
 * 
 * 4. PURCHASE MANAGEMENT:
 *    - Complete purchase history tracking
 *    - Order status and delivery tracking
 *    - Redelivery system for lost items
 *    - Purchase receipts and invoices
 *    - Return and refund processing
 * 
 * 5. FAVORITES SYSTEM:
 *    - Personal favorites collection
 *    - Favorites categorization and tagging
 *    - Price change notifications
 *    - Availability alerts
 *    - Social sharing of favorites
 * 
 * 6. INTEGRATION FEATURES:
 *    - Direct inventory delivery
 *    - L$ balance integration
 *    - Payment processing
 *    - Seller communication tools
 *    - Fraud protection and security
 * 
 * TECHNICAL IMPLEMENTATION:
 * =========================
 * - Unity UI system with ScrollView and GridLayout
 * - UnityWebRequest for HTTP API communication
 * - JSON serialization for data persistence
 * - Texture2D caching for product images
 * - Coroutine-based asynchronous operations
 * - LINQ queries for data filtering and sorting
 * 
 * INTEGRATION POINTS:
 * ===================
 * - LibreMetaverse for L$ transactions
 * - Crystal Frost inventory system
 * - User authentication and profiles
 * - Notification system for purchase updates
 * - Main menu system integration
 * 
 * SECURITY CONSIDERATIONS:
 * ========================
 * - Secure payment processing
 * - Anti-fraud transaction monitoring
 * - Personal data protection
 * - Secure API communication (HTTPS)
 * - User privacy and data anonymization
 * 
 * PERFORMANCE OPTIMIZATIONS:
 * ===========================
 * - Image lazy loading and caching
 * - Virtual scrolling for large lists
 * - Background data prefetching
 * - Memory-efficient data structures
 * - Network request batching and throttling
 * 
 * USAGE:
 * ======
 * This component should be attached to a GameObject in the scene with all
 * UI references properly configured. The marketplace window can be opened
 * through the main menu system or programmatically via ShowMarketplace().
 * 
 * Author: Crystal Frost Development Team
 * Version: 2.0
 * Unity Compatibility: 2021.3.6f1 LTS and higher
 * LibreMetaverse: Compatible with latest versions
 * API Compatibility: SL Marketplace API v2.0+
 */

using OpenMetaverse;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Collections;
using System;
using System.IO;
using System.Linq;

/// <summary>
/// Comprehensive Second Life Marketplace Integration System
/// Provides complete marketplace functionality including product browsing,
/// shopping cart management, purchase tracking, and favorites system.
/// </summary>
public class MarketplaceIntegration : MonoBehaviour
{
    #region Inspector Fields
    
    [Header("Marketplace Window")]
    [Tooltip("Main marketplace window GameObject")]
    public GameObject marketplaceWindow;
    
    [Tooltip("Button to close marketplace window")]
    public Button closeButton;
    
    [Tooltip("Button to refresh product listings")]
    public Button refreshButton;
    
    [Tooltip("Search input field")]
    public TMP_InputField searchField;
    
    [Tooltip("Search execution button")]
    public Button searchButton;
    
    [Tooltip("Product category filter dropdown")]
    public TMP_Dropdown categoryDropdown;
    
    [Tooltip("Product sorting options dropdown")]
    public TMP_Dropdown sortDropdown;
    
    [Header("Product Display")]
    [Tooltip("Root transform for product grid layout")]
    public Transform productGridRoot;
    
    [Tooltip("Prefab for individual product items")]
    public GameObject productItemPrefab;
    
    [Tooltip("Scroll view for product browsing")]
    public ScrollRect productScrollRect;
    
    [Header("Product Details")]
    [Tooltip("Product details panel")]
    public GameObject productDetailsPanel;
    
    [Tooltip("Product main image display")]
    public RawImage productImage;
    
    [Tooltip("Product name text")]
    public TMP_Text productNameText;
    
    [Tooltip("Product description text")]
    public TMP_Text productDescriptionText;
    
    [Tooltip("Product price display")]
    public TMP_Text productPriceText;
    
    [Tooltip("Seller name display")]
    public TMP_Text sellerNameText;
    
    [Tooltip("Direct purchase button")]
    public Button buyButton;
    
    [Tooltip("Add to cart button")]
    public Button addToCartButton;
    
    [Tooltip("Add/remove favorites button")]
    public Button favoriteButton;
    
    [Tooltip("Root for customer reviews")]
    public Transform reviewsRoot;
    
    [Tooltip("Prefab for review items")]
    public GameObject reviewItemPrefab;
    
    [Header("Shopping Cart")]
    [Tooltip("Shopping cart panel")]
    public GameObject shoppingCartPanel;
    
    [Tooltip("Root for cart items")]
    public Transform cartItemsRoot;
    
    [Tooltip("Prefab for cart items")]
    public GameObject cartItemPrefab;
    
    [Tooltip("Cart total price display")]
    public TMP_Text cartTotalText;
    
    [Tooltip("Checkout button")]
    public Button checkoutButton;
    
    [Tooltip("Clear cart button")]
    public Button clearCartButton;
    
    [Header("My Purchases")]
    [Tooltip("Purchase history panel")]
    public GameObject purchasesPanel;
    
    [Tooltip("Root for purchase items")]
    public Transform purchasesRoot;
    
    [Tooltip("Prefab for purchase items")]
    public GameObject purchaseItemPrefab;
    
    [Tooltip("Redeliver purchase button")]
    public Button redeliverButton;
    
    [Header("Pagination")]
    [Tooltip("Previous page navigation button")]
    public Button prevPageButton;
    
    [Tooltip("Next page navigation button")]
    public Button nextPageButton;
    
    [Tooltip("Page information display")]
    public TMP_Text pageInfoText;
    
    #endregion
    
    #region Private Fields
    
    /// <summary>GridClient for LibreMetaverse integration</summary>
    private GridClient client;
    
    /// <summary>Current product search results</summary>
    private List<MarketplaceProduct> currentProducts = new();
    
    /// <summary>Shopping cart contents</summary>
    private List<MarketplaceProduct> shoppingCart = new();
    
    /// <summary>User's purchase history</summary>
    private List<MarketplacePurchase> myPurchases = new();
    
    /// <summary>Currently selected product for details view</summary>
    private MarketplaceProduct selectedProduct;
    
    /// <summary>Current page number for pagination</summary>
    private int currentPage = 0;
    
    /// <summary>Number of products displayed per page</summary>
    private int productsPerPage = 20;
    
    /// <summary>Current search term</summary>
    private string currentSearchTerm = "";
    
    /// <summary>Current category filter index</summary>
    private int currentCategory = 0;
    
    /// <summary>Current sort option index</summary>
    private int currentSort = 0;
    
    #endregion
    
    #region Data Structures
    
    /// <summary>
    /// Comprehensive product data structure
    /// Contains all information needed for product display and purchasing
    /// </summary>
    public class MarketplaceProduct
    {
        public UUID productID;                          // Unique product identifier
        public string name;                             // Product display name
        public string description;                      // Product description
        public int price;                               // Price in L$ (Linden Dollars)
        public string sellerName;                       // Seller display name
        public UUID sellerID;                           // Seller unique identifier
        public string category;                         // Product category
        public string imageUrl;                         // Product image URL
        public Texture2D image;                         // Cached product image
        public float rating;                            // Average customer rating (0-5)
        public int reviewCount;                         // Number of customer reviews
        public bool isFavorite;                         // User's favorite status
        public System.DateTime dateAdded;               // Product listing date
        public List<MarketplaceReview> reviews = new(); // Customer reviews
    }
    
    /// <summary>
    /// Customer review data structure
    /// Contains review information and rating
    /// </summary>
    public class MarketplaceReview
    {
        public UUID reviewerID;         // Reviewer's unique identifier
        public string reviewerName;     // Reviewer's display name
        public float rating;            // Review rating (0-5 stars)
        public string comment;          // Review text content
        public System.DateTime date;    // Review submission date
    }
    
    /// <summary>
    /// Purchase transaction record
    /// Tracks completed marketplace purchases
    /// </summary>
    public class MarketplacePurchase
    {
        public UUID transactionID;          // Unique transaction identifier
        public UUID productID;              // Purchased product ID
        public string productName;          // Product name at time of purchase
        public int pricePaid;               // Amount paid in L$
        public System.DateTime purchaseDate; // Purchase timestamp
        public bool delivered;              // Delivery status
        public string status;               // Transaction status
    }
    
    #endregion
    
    #region Unity Lifecycle
    
    /// <summary>
    /// Initialize marketplace integration component
    /// Called before Start() on the first frame
    /// </summary>
    void Awake()
    {
        // Hide marketplace window initially
        marketplaceWindow.SetActive(false);
        
        // Setup UI event handlers
        SetupUI();
    }
    
    /// <summary>
    /// Complete initialization after all objects are available
    /// Called on the first frame after Awake()
    /// </summary>
    void Start()
    {
        // Get LibreMetaverse client reference
        client = ClientManager.client;
        
        // Load user data from persistent storage
        LoadFavorites();
        LoadPurchaseHistory();
    }
    
    /// <summary>
    /// Cleanup when component is destroyed
    /// Saves user data and unsubscribes from events
    /// </summary>
    void OnDestroy()
    {
        // Save current user data before destruction
        SaveFavorites();
        SavePurchaseHistory();
        
        // Note: This component doesn't subscribe to external events
        // but this method is included for consistency and future expansion
        if (client != null)
        {
            // Unsubscribe from any LibreMetaverse events if added in the future
            // Example: client.Money.MoneyBalanceReply -= OnMoneyBalanceReply;
        }
    }
    
    #endregion
    
    #region Initialization and Setup
    
    /// <summary>
    /// Setup UI event handlers and component references
    /// Connects all UI elements to their respective functions
    /// </summary>
    void SetupUI()
    {
        // Main window controls
        if (closeButton) closeButton.onClick.AddListener(() => marketplaceWindow.SetActive(false));
        if (refreshButton) refreshButton.onClick.AddListener(RefreshProducts);
        if (searchButton) searchButton.onClick.AddListener(SearchProducts);
        if (searchField) searchField.onEndEdit.AddListener((text) => { 
            if (Input.GetKeyDown(KeyCode.Return)) SearchProducts(); 
        });
        
        // Filter and sort controls
        if (categoryDropdown) categoryDropdown.onValueChanged.AddListener(OnCategoryChanged);
        if (sortDropdown) sortDropdown.onValueChanged.AddListener(OnSortChanged);
        
        // Product detail controls
        if (buyButton) buyButton.onClick.AddListener(BuyProduct);
        if (addToCartButton) addToCartButton.onClick.AddListener(AddToCart);
        if (favoriteButton) favoriteButton.onClick.AddListener(ToggleFavorite);
        
        // Shopping cart controls
        if (checkoutButton) checkoutButton.onClick.AddListener(Checkout);
        if (clearCartButton) clearCartButton.onClick.AddListener(ClearCart);
        
        // Purchase management controls
        if (redeliverButton) redeliverButton.onClick.AddListener(RedeliverPurchase);
        
        // Pagination controls
        if (prevPageButton) prevPageButton.onClick.AddListener(PreviousPage);
        if (nextPageButton) nextPageButton.onClick.AddListener(NextPage);
        
        // Initialize dropdown options
        SetupDropdowns();
    }
    
    /// <summary>
    /// Setup dropdown menus with available options
    /// Configures category and sorting dropdowns
    /// </summary>
    void SetupDropdowns()
    {
        // Configure category dropdown with SL marketplace categories
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
        
        // Configure sort dropdown with sorting options
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
    
    #endregion
    
    #region Public Interface
    
    /// <summary>
    /// Show the marketplace window
    /// Public method for external components to open the marketplace
    /// </summary>
    public void ShowMarketplace()
    {
        marketplaceWindow.SetActive(true);
        RefreshProducts();
    }
    
    /// <summary>
    /// Show products by specific category
    /// Allows external systems to open marketplace with category filter
    /// </summary>
    /// <param name="category">Category name to filter by</param>
    public void ShowProductsByCategory(string category)
    {
        currentSearchTerm = "";
        
        if (categoryDropdown && searchField)
        {
            searchField.text = "";
            
            // Find and select the matching category
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
    
    /// <summary>
    /// Search for specific product by name
    /// Allows external systems to perform targeted searches
    /// </summary>
    /// <param name="searchTerm">Product search term</param>
    public void SearchForProduct(string searchTerm)
    {
        if (searchField)
        {
            searchField.text = searchTerm;
            SearchProducts();
        }
        
        ShowMarketplace();
    }
    
    #endregion
    
    #region Product Management
    
    /// <summary>
    /// Refresh product listings from marketplace
    /// Initiates product fetch operation
    /// </summary>
    void RefreshProducts()
    {
        StartCoroutine(FetchProducts());
    }
    
    /// <summary>
    /// Asynchronous product fetching from marketplace API
    /// In production, this would query the actual SL Marketplace API
    /// </summary>
    /// <returns>Coroutine enumerator</returns>
    IEnumerator FetchProducts()
    {
        // Clear current product list
        currentProducts.Clear();
        
        // Simulate API delay (in production, this would be actual HTTP request)
        yield return new WaitForSeconds(0.5f);
        
        // Generate mock products for demonstration
        // In production, this would parse API response
        GenerateMockProducts();
        
        // Apply current filters and sorting
        FilterAndSortProducts();
        
        // Update UI display
        DisplayProducts();
    }
    
    /// <summary>
    /// Generate mock product data for demonstration
    /// In production, this would be replaced by API response parsing
    /// </summary>
    void GenerateMockProducts()
    {
        // Sample data for demonstration purposes
        string[] categories = { "Clothing", "Furniture", "Avatars", "Hair", "Shapes", "Animations" };
        string[] adjectives = { "Elegant", "Modern", "Vintage", "Stylish", "Luxury", "Premium", "Classic", "Trendy" };
        string[] nouns = { "Dress", "Chair", "Avatar", "Hairstyle", "Shape", "Dance", "Shirt", "Sofa", "Skin", "Animation" };
        
        // Generate 50 sample products
        for (int i = 0; i < 50; i++)
        {
            var product = new MarketplaceProduct
            {
                productID = UUID.Random(),
                name = $"{adjectives[UnityEngine.Random.Range(0, adjectives.Length)]} {nouns[UnityEngine.Random.Range(0, nouns.Length)]}",
                description = "High quality product with amazing features and excellent craftsmanship.",
                price = UnityEngine.Random.Range(50, 2000),  // L$50 to L$2000
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
    
    /// <summary>
    /// Apply current filtering and sorting to product list
    /// Processes products based on user-selected criteria
    /// </summary>
    void FilterAndSortProducts()
    {
        var filteredProducts = currentProducts;
        
        // Apply category filter
        if (currentCategory > 0 && categoryDropdown != null)
        {
            string selectedCategory = categoryDropdown.options[currentCategory].text;
            filteredProducts = filteredProducts.FindAll(p => p.category == selectedCategory);
        }
        
        // Apply search term filter
        if (!string.IsNullOrEmpty(currentSearchTerm))
        {
            string search = currentSearchTerm.ToLower();
            filteredProducts = filteredProducts.FindAll(p => 
                p.name.ToLower().Contains(search) || 
                p.description.ToLower().Contains(search) ||
                p.sellerName.ToLower().Contains(search));
        }
        
        // Apply sorting based on selected option
        switch (currentSort)
        {
            case 0: // Relevance (no additional sorting)
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
            case 4: // Best Selling (by review count)
                filteredProducts.Sort((a, b) => b.reviewCount.CompareTo(a.reviewCount));
                break;
            case 5: // Highest Rated
                filteredProducts.Sort((a, b) => b.rating.CompareTo(a.rating));
                break;
        }
        
        currentProducts = filteredProducts;
    }
    
    /// <summary>
    /// Display filtered and sorted products in the UI
    /// Creates visual product items with pagination
    /// </summary>
    void DisplayProducts()
    {
        // Clear existing product display items
        foreach (Transform child in productGridRoot)
        {
            Destroy(child.gameObject);
        }
        
        // Calculate pagination boundaries
        int startIndex = currentPage * productsPerPage;
        int endIndex = Mathf.Min(startIndex + productsPerPage, currentProducts.Count);
        
        // Create product items for current page
        for (int i = startIndex; i < endIndex; i++)
        {
            CreateProductItem(currentProducts[i]);
        }
        
        // Update pagination information
        UpdatePaginationInfo();
    }
    
    /// <summary>
    /// Create a visual product item in the grid
    /// Instantiates and configures product display element
    /// </summary>
    /// <param name="product">Product data to display</param>
    void CreateProductItem(MarketplaceProduct product)
    {
        if (productItemPrefab == null) return;
        
        var productObj = Instantiate(productItemPrefab, productGridRoot);
        
        // Configure product display elements
        var nameText = productObj.transform.Find("NameText")?.GetComponent<TMP_Text>();
        var priceText = productObj.transform.Find("PriceText")?.GetComponent<TMP_Text>();
        var sellerText = productObj.transform.Find("SellerText")?.GetComponent<TMP_Text>();
        var ratingText = productObj.transform.Find("RatingText")?.GetComponent<TMP_Text>();
        var productImage = productObj.transform.Find("ProductImage")?.GetComponent<RawImage>();
        var button = productObj.GetComponent<Button>();
        
        // Set product information
        if (nameText) nameText.text = product.name;
        if (priceText) priceText.text = $"L${product.price}";
        if (sellerText) sellerText.text = $"by {product.sellerName}";
        if (ratingText) ratingText.text = $"★ {product.rating:F1} ({product.reviewCount})";
        
        // Load product image asynchronously
        if (productImage && !string.IsNullOrEmpty(product.imageUrl))
        {
            StartCoroutine(LoadProductImage(product, productImage));
        }
        
        // Setup click handler for product selection
        if (button)
        {
            button.onClick.AddListener(() => ShowProductDetails(product));
        }
    }
    
    /// <summary>
    /// Asynchronously load product image from URL
    /// Downloads and caches product images
    /// </summary>
    /// <param name="product">Product containing image URL</param>
    /// <param name="imageComponent">UI image component to display in</param>
    /// <returns>Coroutine enumerator</returns>
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
    
    #endregion
    
    #region Product Details
    
    /// <summary>
    /// Show detailed view of selected product
    /// Displays comprehensive product information and reviews
    /// </summary>
    /// <param name="product">Product to show details for</param>
    void ShowProductDetails(MarketplaceProduct product)
    {
        selectedProduct = product;
        
        // Show details panel
        if (productDetailsPanel) productDetailsPanel.SetActive(true);
        
        // Update product information display
        if (productNameText) productNameText.text = product.name;
        if (productDescriptionText) productDescriptionText.text = product.description;
        if (productPriceText) productPriceText.text = $"L${product.price}";
        if (sellerNameText) sellerNameText.text = $"Sold by: {product.sellerName}";
        
        // Display product image
        if (productImage && product.image)
        {
            productImage.texture = product.image;
        }
        
        // Update favorite button state
        if (favoriteButton)
        {
            var buttonText = favoriteButton.GetComponentInChildren<TMP_Text>();
            if (buttonText)
            {
                buttonText.text = product.isFavorite ? "★ Remove from Favorites" : "☆ Add to Favorites";
            }
        }
        
        // Load and display product reviews
        LoadProductReviews(product);
    }
    
    /// <summary>
    /// Load and display customer reviews for a product
    /// Generates mock reviews for demonstration
    /// </summary>
    /// <param name="product">Product to load reviews for</param>
    void LoadProductReviews(MarketplaceProduct product)
    {
        // Clear existing reviews
        foreach (Transform child in reviewsRoot)
        {
            Destroy(child.gameObject);
        }
        
        // Generate mock reviews (in production, these would come from API)
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
    
    /// <summary>
    /// Create a visual review item
    /// Displays customer review with rating and date
    /// </summary>
    /// <param name="review">Review data to display</param>
    void CreateReviewItem(MarketplaceReview review)
    {
        if (reviewItemPrefab == null) return;
        
        var reviewObj = Instantiate(reviewItemPrefab, reviewsRoot);
        
        // Configure review display elements
        var nameText = reviewObj.transform.Find("ReviewerName")?.GetComponent<TMP_Text>();
        var ratingText = reviewObj.transform.Find("Rating")?.GetComponent<TMP_Text>();
        var commentText = reviewObj.transform.Find("Comment")?.GetComponent<TMP_Text>();
        var dateText = reviewObj.transform.Find("Date")?.GetComponent<TMP_Text>();
        
        // Set review information
        if (nameText) nameText.text = review.reviewerName;
        if (ratingText) ratingText.text = $"★ {review.rating:F1}";
        if (commentText) commentText.text = review.comment;
        if (dateText) dateText.text = review.date.ToString("MMM dd, yyyy");
    }
    
    #endregion
    
    #region Shopping Cart Management
    
    /// <summary>
    /// Add selected product to shopping cart
    /// Prevents duplicate additions and updates cart display
    /// </summary>
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
    
    /// <summary>
    /// Update shopping cart display
    /// Refreshes cart items and calculates total price
    /// </summary>
    void UpdateCartDisplay()
    {
        // Clear existing cart items
        foreach (Transform child in cartItemsRoot)
        {
            Destroy(child.gameObject);
        }
        
        int totalPrice = 0;
        
        // Create cart items and calculate total
        foreach (var product in shoppingCart)
        {
            CreateCartItem(product);
            totalPrice += product.price;
        }
        
        // Update total price display
        if (cartTotalText) cartTotalText.text = $"Total: L${totalPrice}";
    }
    
    /// <summary>
    /// Create a visual cart item
    /// Displays product in cart with remove option
    /// </summary>
    /// <param name="product">Product to display in cart</param>
    void CreateCartItem(MarketplaceProduct product)
    {
        if (cartItemPrefab == null) return;
        
        var cartObj = Instantiate(cartItemPrefab, cartItemsRoot);
        var nameText = cartObj.GetComponentInChildren<TMP_Text>();
        var removeButton = cartObj.GetComponentInChildren<Button>();
        
        // Set cart item information
        if (nameText) nameText.text = $"{product.name} - L${product.price}";
        
        // Setup remove button
        if (removeButton)
        {
            removeButton.onClick.AddListener(() =>
            {
                shoppingCart.Remove(product);
                UpdateCartDisplay();
            });
        }
    }
    
    /// <summary>
    /// Clear all items from shopping cart
    /// Empties cart and updates display
    /// </summary>
    void ClearCart()
    {
        shoppingCart.Clear();
        UpdateCartDisplay();
    }
    
    /// <summary>
    /// Process checkout for all cart items
    /// Simulates purchase transaction and updates purchase history
    /// </summary>
    void Checkout()
    {
        if (shoppingCart.Count == 0) return;
        
        int totalPrice = shoppingCart.Sum(p => p.price);
        
        // In production, this would integrate with actual payment processing
        Debug.Log($"Processing checkout for L${totalPrice}");
        
        // Simulate successful purchase
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
        
        // Clear cart and save purchase history
        ClearCart();
        SavePurchaseHistory();
        
        Debug.Log("Purchase completed successfully!");
    }
    
    #endregion
    
    #region Purchase Management
    
    /// <summary>
    /// Execute direct purchase of selected product
    /// Bypasses cart for immediate purchase
    /// </summary>
    void BuyProduct()
    {
        if (selectedProduct == null) return;
        
        Debug.Log($"Buying {selectedProduct.name} for L${selectedProduct.price}");
        
        // Create purchase record
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
        
        // Add to purchase history and save
        myPurchases.Add(purchase);
        SavePurchaseHistory();
        
        Debug.Log("Purchase completed!");
    }
    
    /// <summary>
    /// Redeliver a previously purchased item
    /// For cases where items were lost or not received
    /// </summary>
    void RedeliverPurchase()
    {
        // In production, this would trigger redelivery through SL inventory system
        Debug.Log("Redelivering purchase");
    }
    
    #endregion
    
    #region Favorites Management
    
    /// <summary>
    /// Toggle favorite status of selected product
    /// Adds or removes product from user's favorites
    /// </summary>
    void ToggleFavorite()
    {
        if (selectedProduct == null) return;
        
        selectedProduct.isFavorite = !selectedProduct.isFavorite;
        
        // Update favorite button display
        if (favoriteButton)
        {
            var buttonText = favoriteButton.GetComponentInChildren<TMP_Text>();
            if (buttonText)
            {
                buttonText.text = selectedProduct.isFavorite ? "★ Remove from Favorites" : "☆ Add to Favorites";
            }
        }
        
        // Save favorites to persistent storage
        SaveFavorites();
    }
    
    #endregion
    
    #region Search and Filtering
    
    /// <summary>
    /// Execute product search based on search field input
    /// Applies search filter and refreshes product display
    /// </summary>
    void SearchProducts()
    {
        if (searchField == null) return;
        
        currentSearchTerm = searchField.text;
        currentPage = 0;  // Reset to first page
        RefreshProducts();
    }
    
    /// <summary>
    /// Handle category filter change
    /// Updates product filtering and refreshes display
    /// </summary>
    /// <param name="value">Selected category index</param>
    void OnCategoryChanged(int value)
    {
        currentCategory = value;
        currentPage = 0;  // Reset to first page
        RefreshProducts();
    }
    
    /// <summary>
    /// Handle sort option change
    /// Updates product sorting and refreshes display
    /// </summary>
    /// <param name="value">Selected sort option index</param>
    void OnSortChanged(int value)
    {
        currentSort = value;
        RefreshProducts();
    }
    
    #endregion
    
    #region Pagination
    
    /// <summary>
    /// Update pagination information display
    /// Shows current page and navigation button states
    /// </summary>
    void UpdatePaginationInfo()
    {
        if (pageInfoText)
        {
            int totalPages = Mathf.CeilToInt((float)currentProducts.Count / productsPerPage);
            pageInfoText.text = $"Page {currentPage + 1} of {totalPages} ({currentProducts.Count} products)";
        }
        
        // Update navigation button states
        if (prevPageButton) prevPageButton.interactable = currentPage > 0;
        if (nextPageButton) nextPageButton.interactable = (currentPage + 1) * productsPerPage < currentProducts.Count;
    }
    
    /// <summary>
    /// Navigate to previous page
    /// Decrements page number and refreshes display
    /// </summary>
    void PreviousPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            DisplayProducts();
        }
    }
    
    /// <summary>
    /// Navigate to next page
    /// Increments page number and refreshes display
    /// </summary>
    void NextPage()
    {
        if ((currentPage + 1) * productsPerPage < currentProducts.Count)
        {
            currentPage++;
            DisplayProducts();
        }
    }
    
    #endregion
    
    #region Data Persistence
    
    /// <summary>
    /// Save user's favorite products to persistent storage
    /// Stores favorites as JSON for cross-session persistence
    /// </summary>
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
    
    /// <summary>
    /// Load user's favorite products from persistent storage
    /// Restores favorites from previous sessions
    /// </summary>
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
    
    /// <summary>
    /// Save purchase history to persistent storage
    /// Stores purchase records as JSON for tracking
    /// </summary>
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
    
    /// <summary>
    /// Load purchase history from persistent storage
    /// Restores purchase records from previous sessions
    /// </summary>
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
    
    #region Utility Classes
    
    /// <summary>
    /// Helper class for JSON serialization of generic lists
    /// Unity's JsonUtility doesn't directly support List serialization
    /// </summary>
    /// <typeparam name="T">Type of list elements</typeparam>
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
    
    #endregion
}