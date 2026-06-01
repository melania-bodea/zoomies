// Change this value if the backend runs on a different local port.
const API_BASE_URL = "https://localhost:7031/api";
const API_ROOT_URL = API_BASE_URL.replace(/\/api\/?$/, "");
const TOKEN_STORAGE_KEY = "zoomies.jwt";

const state = {
  token: localStorage.getItem(TOKEN_STORAGE_KEY),
  currentUser: null,
  listings: [],
  myListings: [],
  wishlistListings: [],
  wishlistIds: [],
  isLoading: false,
  isMyListingsLoading: false,
  isWishlistLoading: false,
  totalCount: 0,
  currentPage: 1,
  pageSize: 20,
  totalPages: 1,
  viewMode: "grid",
  chat: {
    isOpen: false,
    threads: {},
    activeThreadKey: null
  }
};

let pendingDeleteId = null;
let chatConnection = null;
let hoverTimers = {};

function startSignalR() {
  if (!state.token || chatConnection || typeof signalR === "undefined") return;

  chatConnection = new signalR.HubConnectionBuilder()
    .withUrl(`${API_ROOT_URL}/chatHub`, { accessTokenFactory: () => state.token })
    .withAutomaticReconnect()
    .build();

  chatConnection.on("ReceiveMessage", newMessage => {
    showToast(`New message from ${newMessage.senderName || "seller"}.`);
    refreshChat();
  });

  chatConnection.start().catch(error => {
    console.error("SignalR connection failed:", error);
    chatConnection = null;
  });
}

function stopSignalR() {
  if (!chatConnection) return;

  const connection = chatConnection;
  chatConnection = null;
  connection.stop().catch(error => console.error("SignalR stop failed:", error));
}

const els = {
  carGrid: document.getElementById("carGrid"),
  wishlistGrid: document.getElementById("wishlistGrid"),
  wishlistCount: document.getElementById("wishlistCount"),
  wishlistEmptyState: document.getElementById("wishlistEmptyState"),
  backToInventoryBtn: document.getElementById("backToInventoryBtn"),
  myListingsGrid: document.getElementById("myListingsGrid"),
  myListingsCount: document.getElementById("myListingsCount"),
  myListingsEmptyState: document.getElementById("myListingsEmptyState"),
  backFromMyListingsBtn: document.getElementById("backFromMyListingsBtn"),
  backFromAboutBtn: document.getElementById("backFromAboutBtn"),
  backFromContactBtn: document.getElementById("backFromContactBtn"),
  contactBrowseBtn: document.getElementById("contactBrowseBtn"),
  emptyState: document.getElementById("emptyState"),
  resultCount: document.getElementById("resultCount"),
  apiStatus: document.getElementById("apiStatus"),
  activeListingStat: document.getElementById("activeListingStat"),
  filtersForm: document.getElementById("filtersForm"),
  heroSearchForm: document.getElementById("heroSearchForm"),
  heroSearchInput: document.getElementById("heroSearchInput"),
  filterSearch: document.getElementById("filterSearch"),
  filterCategory: document.getElementById("filterCategory"),
  filterMaxPrice: document.getElementById("filterMaxPrice"),
  filterMinYear: document.getElementById("filterMinYear"),
  filterTransmission: document.getElementById("filterTransmission"),
  filterSort: document.getElementById("filterSort"),
  resetFiltersBtn: document.getElementById("resetFiltersBtn"),
  pageSizeSelect: document.getElementById("pageSizeSelect"),
  btnGridView: document.getElementById("btnGridView"),
  btnListView: document.getElementById("btnListView"),
  paginationBar: document.getElementById("paginationBar"),
  prevPageBtn: document.getElementById("prevPageBtn"),
  nextPageBtn: document.getElementById("nextPageBtn"),
  pageInfo: document.getElementById("pageInfo"),
  accountBadge: document.getElementById("accountBadge"),
  currentAccountName: document.getElementById("currentAccountName"),
  currentAccountEmail: document.getElementById("currentAccountEmail"),
  authModal: document.getElementById("authModal"),
  listingModal: document.getElementById("listingModal"),
  detailModal: document.getElementById("detailModal"),
  deleteModal: document.getElementById("deleteModal"),
  contactModal: document.getElementById("contactModal"),
  listingForm: document.getElementById("listingForm"),
  contactForm: document.getElementById("contactForm"),
  authFormsPanel: document.getElementById("authFormsPanel"),
  profilePanel: document.getElementById("profilePanel"),
  profileName: document.getElementById("profileName"),
  profileEmail: document.getElementById("profileEmail"),
  profilePhone: document.getElementById("profilePhone"),
  profilePhoneInput: document.getElementById("profilePhoneInput"),
  profileRole: document.getElementById("profileRole"),
  phoneForm: document.getElementById("phoneForm"),
  loginForm: document.getElementById("loginForm"),
  registerForm: document.getElementById("registerForm"),
  authStatus: document.getElementById("authStatus"),
  accountMyListingsBtn: document.getElementById("accountMyListingsBtn"),
  accountWishlistBtn: document.getElementById("accountWishlistBtn"),
  logoutBtn: document.getElementById("logoutBtn"),
  detailBody: document.getElementById("detailBody"),
  deleteMessage: document.getElementById("deleteMessage"),
  confirmDeleteBtn: document.getElementById("confirmDeleteBtn"),
  paymentForm: document.getElementById("paymentForm"),
  paymentResult: document.getElementById("paymentResult"),
  estimateForm: document.getElementById("estimateForm"),
  estimateResult: document.getElementById("estimateResult"),
  toast: document.getElementById("toast"),
  chatFab: document.getElementById("chatFab"),
  chatWidget: document.getElementById("chatWidget"),
  chatUnreadBadge: document.getElementById("chatUnreadBadge"),
  chatBackBtn: document.getElementById("chatBackBtn"),
  chatTitle: document.getElementById("chatTitle"),
  chatMinimizeBtn: document.getElementById("chatMinimizeBtn"),
  chatThreadList: document.getElementById("chatThreadList"),
  chatConversation: document.getElementById("chatConversation"),
  chatMessages: document.getElementById("chatMessages"),
  chatReplyForm: document.getElementById("chatReplyForm"),
  chatReplyInput: document.getElementById("chatReplyInput")
};

init();

async function init() {
  restoreUserFromToken();
  bindEvents();
  updateAuthUi();
  await loadCars();
  await loadWishlist();
}

function bindEvents() {
  els.filtersForm.addEventListener("input", debounce(() => {
    state.currentPage = 1;
    loadCars();
  }, 250));
  els.filtersForm.addEventListener("change", () => {
    state.currentPage = 1;
    loadCars();
  });

  els.heroSearchForm.addEventListener("submit", event => {
    event.preventDefault();
    els.filterSearch.value = els.heroSearchInput.value.trim();
    state.currentPage = 1;
    document.getElementById("inventory").scrollIntoView({ behavior: "smooth" });
    loadCars();
  });

  document.getElementById("categoryTags").addEventListener("click", event => {
    const button = event.target.closest("[data-category]");
    if (!button) return;
    const next = els.filterCategory.value === button.dataset.category ? "" : button.dataset.category;
    els.filterCategory.value = next;
    document.querySelectorAll(".hero-tag").forEach(tag => tag.classList.toggle("active", tag.dataset.category === next));
    state.currentPage = 1;
    document.getElementById("inventory").scrollIntoView({ behavior: "smooth" });
    loadCars();
  });

  els.resetFiltersBtn.addEventListener("click", resetFilters);
  els.pageSizeSelect.addEventListener("change", () => {
    state.pageSize = Math.min(20, Math.max(1, Number(els.pageSizeSelect.value) || 20));
    state.currentPage = 1;
    loadCars();
  });
  els.prevPageBtn.addEventListener("click", () => {
    if (state.currentPage <= 1) return;
    state.currentPage -= 1;
    loadCars();
    document.getElementById("inventoryStart").scrollIntoView({ behavior: "smooth" });
  });
  els.nextPageBtn.addEventListener("click", () => {
    if (state.currentPage >= state.totalPages) return;
    state.currentPage += 1;
    loadCars();
    document.getElementById("inventoryStart").scrollIntoView({ behavior: "smooth" });
  });
  els.btnGridView.addEventListener("click", () => {
    state.viewMode = "grid";
    els.btnGridView.classList.add("active");
    els.btnListView.classList.remove("active");
    render();
  });
  els.btnListView.addEventListener("click", () => {
    state.viewMode = "list";
    els.btnListView.classList.add("active");
    els.btnGridView.classList.remove("active");
    render();
  });

  els.accountBadge.addEventListener("click", () => openModal(els.authModal));
  document.querySelectorAll("[data-open-listing]").forEach(button => button.addEventListener("click", event => {
    event.preventDefault();
    openListingForm();
  }));
  document.querySelectorAll("[data-open-about]").forEach(link => link.addEventListener("click", event => {
    event.preventDefault();
    openAboutPage();
  }));
  document.querySelectorAll("[data-open-contact]").forEach(link => link.addEventListener("click", event => {
    event.preventDefault();
    openContactPage();
  }));

  document.addEventListener("click", event => {
    if (event.target.closest("[data-close-modal]")) closeAllModals();
    if (event.target.classList.contains("modal-backdrop")) closeAllModals();
  });
  document.addEventListener("keydown", event => {
    if (event.key === "Escape") closeAllModals();
  });

  document.querySelectorAll("[data-auth-tab]").forEach(button => button.addEventListener("click", () => switchAuthTab(button.dataset.authTab)));
  els.loginForm.addEventListener("submit", handleLogin);
  els.registerForm.addEventListener("submit", handleRegister);
  els.phoneForm.addEventListener("submit", handlePhoneUpdate);
  els.listingForm.addEventListener("submit", handleListingSave);
  els.contactForm.addEventListener("submit", handleContactSeller);
  els.chatFab.addEventListener("click", toggleChatWidget);
  els.chatMinimizeBtn.addEventListener("click", toggleChatWidget);
  els.chatBackBtn.addEventListener("click", showChatThreadList);
  els.chatReplyForm.addEventListener("submit", handleChatReply);
  els.paymentForm.addEventListener("submit", handlePaymentCalculation);
  els.estimateForm.addEventListener("submit", handlePriceEstimate);
  els.confirmDeleteBtn.addEventListener("click", confirmDelete);
  els.accountMyListingsBtn.addEventListener("click", () => {
    closeAllModals();
    openMyListingsPage();
  });
  els.accountWishlistBtn.addEventListener("click", () => {
    closeAllModals();
    openWishlistPage();
  });
  els.logoutBtn.addEventListener("click", logout);
  document.getElementById("wishlistBtn").addEventListener("click", openWishlistPage);
  els.backToInventoryBtn.addEventListener("click", showInventoryPage);
  els.backFromMyListingsBtn.addEventListener("click", showInventoryPage);
  els.backFromAboutBtn.addEventListener("click", showInventoryPage);
  els.backFromContactBtn.addEventListener("click", showInventoryPage);
  els.contactBrowseBtn.addEventListener("click", showInventoryPage);
  document.querySelectorAll('a[href^="#"]').forEach(link => {
    if (link.matches("[data-open-about], [data-open-contact]")) return;
    link.addEventListener("click", () => {
      showMainSite();
      render();
    });
  });
}

function render() {
  els.carGrid.classList.toggle("list-view", state.viewMode === "list");
  els.carGrid.innerHTML = state.listings.map(car => renderCard(car, "inventory")).join("");
  els.emptyState.hidden = state.isLoading || state.listings.length > 0;
  els.emptyState.textContent = "No listings match the current filters.";
  els.resultCount.textContent = `${state.totalCount} ${state.totalCount === 1 ? "listing" : "listings"} found`;
  els.activeListingStat.textContent = `${state.totalCount}+`;
  const shouldShowPagination = state.totalCount > state.pageSize || state.totalPages > 1;
  els.paginationBar.hidden = !shouldShowPagination;
  if (shouldShowPagination) {
    els.pageInfo.textContent = `Page ${state.currentPage} of ${state.totalPages}`;
    els.prevPageBtn.disabled = state.currentPage <= 1;
    els.nextPageBtn.disabled = state.currentPage >= state.totalPages;
  }
  document.getElementById("wishlistBtn").classList.toggle("active", document.body.classList.contains("show-wishlist-page"));
  renderMyListingsPage();
  renderWishlistPage();
  bindCardActions();
  bindHoverGalleries();
}

function renderWishlistPage() {
  els.wishlistGrid.innerHTML = state.wishlistListings.map(car => renderCard(car, "wishlist")).join("");
  els.wishlistEmptyState.hidden = state.isWishlistLoading || state.wishlistListings.length > 0;
  els.wishlistEmptyState.textContent = state.currentUser ? "No saved cars yet." : "Login to see your saved cars.";
  els.wishlistCount.textContent = `${state.wishlistListings.length} saved ${state.wishlistListings.length === 1 ? "listing" : "listings"}`;
}

function renderMyListingsPage() {
  els.myListingsGrid.innerHTML = state.myListings.map(car => renderCard(car, "myListings")).join("");
  els.myListingsEmptyState.hidden = state.isMyListingsLoading || state.myListings.length > 0;
  els.myListingsEmptyState.textContent = state.currentUser ? "You have not added any listings yet." : "Login to see your listings.";
  els.myListingsCount.textContent = `${state.myListings.length} personal ${state.myListings.length === 1 ? "listing" : "listings"}`;
}

async function loadCars() {
  state.isLoading = true;
  setApiStatus("Loading inventory...");
  render();

  try {
    const result = await apiRequest(`/Cars${buildCarQueryString()}`, { auth: Boolean(state.token) });
    if (Array.isArray(result)) {
      state.listings = result.map(normalizeCar);
      state.totalCount = result.length;
      state.totalPages = 1;
    } else {
      state.listings = (result.items || []).map(normalizeCar);
      state.totalCount = result.totalCount || state.listings.length;
      state.totalPages = result.totalPages || Math.max(1, Math.ceil(state.totalCount / state.pageSize));
      state.currentPage = result.currentPage || state.currentPage;
      state.pageSize = Math.min(20, result.pageSize || state.pageSize);
      els.pageSizeSelect.value = String(state.pageSize);
    }
    setApiStatus("Connected");
  } catch (error) {
    state.listings = [];
    state.totalCount = 0;
    state.totalPages = 1;
    setApiStatus("API not reachable", true);
    showToast(`${error.message}. Start the backend with dotnet run.`);
  } finally {
    state.isLoading = false;
    render();
  }
}

async function loadMyListings() {
  if (!state.currentUser) {
    state.myListings = [];
    render();
    return;
  }

  state.isMyListingsLoading = true;
  render();

  try {
    const cars = await apiRequest("/Cars/mine", { auth: true });
    state.myListings = cars.map(normalizeCar);
  } catch (error) {
    showToast(error.message);
  } finally {
    state.isMyListingsLoading = false;
    render();
  }
}

async function loadWishlist() {
  if (!state.currentUser) {
    state.wishlistListings = [];
    state.wishlistIds = [];
    render();
    return;
  }

  state.isWishlistLoading = true;
  render();

  try {
    const cars = await apiRequest("/Wishlist", { auth: true });
    state.wishlistListings = cars.map(normalizeCar);
    state.wishlistIds = state.wishlistListings.map(car => String(car.id));
  } catch (error) {
    showToast(error.message);
  } finally {
    state.isWishlistLoading = false;
    render();
  }
}

function renderCard(car, context = "inventory") {
  const canManage = canManageCar(car);
  const isWishlisted = state.wishlistIds.includes(String(car.id));
  const isListView = state.viewMode === "list" && context === "inventory";
  const manageActions = canManage
    ? `
        <button class="btn btn-muted" type="button" data-action="edit">Edit</button>
        <button class="btn btn-danger" type="button" data-action="delete">Delete</button>
      `
    : "";
  const description = car.description || "";
  const descSnippet = description.length > 180
    ? `${escapeHtml(description.slice(0, 180))}...`
    : escapeHtml(description);
  const galleryUrls = car.galleryUrls.length > 0 ? car.galleryUrls : [car.imageUrl];
  const imageTags = galleryUrls
    .map((url, index) => `<img src="${escapeAttr(url)}" class="hover-img ${index === 0 ? "active" : ""}" alt="${escapeAttr(car.make)} ${escapeAttr(car.model)}" loading="lazy" />`)
    .join("");
  const dots = galleryUrls.length > 1
    ? `<div class="gallery-indicators">${galleryUrls.map((_, index) => `<span class="gallery-dot ${index === 0 ? "active" : ""}"></span>`).join("")}</div>`
    : "";

  return `
    <article class="car-card ${isListView ? "list-view" : ""}" data-id="${car.id}" data-context="${context}">
      <div class="car-img-wrap" data-hover-gallery>
        ${imageTags}
        ${dots}
        ${car.featured ? '<span class="featured-badge">Featured</span>' : ""}
        <button class="wishlist-fab ${isWishlisted ? "active" : ""}" type="button" data-action="wishlist" aria-label="${isWishlisted ? "Remove from wishlist" : "Add to wishlist"}">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78L12 21.23l7.78-7.78a5.5 5.5 0 0 0 1.06-8.84z"/></svg>
        </button>
        <div class="car-overlay">
          <h3>${escapeHtml(car.make)} ${escapeHtml(car.model)}</h3>
          <p class="car-meta"><span>${car.year}</span><span>${formatNumber(car.mileage)} mi</span></p>
        </div>
      </div>
      <div class="car-data-wrap">
        <div class="list-details">
          <strong>Seller:</strong> ${escapeHtml(car.sellerName)}<br><br>
          ${descSnippet}
        </div>
        <div class="car-footer">
          <div><div class="car-price-label">Price</div><div class="car-price">${formatCurrency(car.price)}</div></div>
          <div><div class="car-trans-label">Trans</div><div class="car-trans">${escapeHtml(car.transmission)}</div></div>
        </div>
        <div class="card-actions">
          <button class="btn btn-outline" type="button" data-action="details">Details</button>
          ${manageActions}
        </div>
      </div>
    </article>
  `;
}

function bindHoverGalleries() {
  document.querySelectorAll(".car-card").forEach(card => {
    const wrap = card.querySelector(".car-img-wrap[data-hover-gallery]");
    if (!wrap) return;

    const images = wrap.querySelectorAll(".hover-img");
    const dots = wrap.querySelectorAll(".gallery-dot");
    if (images.length <= 1) return;

    const key = `${card?.dataset.context || "inventory"}-${card?.dataset.id || ""}`;
    let index = 0;

    const setActiveImage = nextIndex => {
      images[index].classList.remove("active");
      dots[index]?.classList.remove("active");
      index = nextIndex % images.length;
      images[index].classList.add("active");
      dots[index]?.classList.add("active");
    };

    card.addEventListener("mouseenter", () => {
      clearInterval(hoverTimers[key]);
      setActiveImage((index + 1) % images.length);
      hoverTimers[key] = setInterval(() => {
        setActiveImage((index + 1) % images.length);
      }, 1600);
    });

    card.addEventListener("mouseleave", () => {
      clearInterval(hoverTimers[key]);
      index = 0;
      images.forEach(image => image.classList.remove("active"));
      dots.forEach(dot => dot.classList.remove("active"));
      images[0].classList.add("active");
      dots[0]?.classList.add("active");
    });
  });
}

function bindCardActions() {
  document.querySelectorAll("#carGrid [data-action], #wishlistGrid [data-action], #myListingsGrid [data-action]").forEach(button => {
    button.addEventListener("click", async event => {
      const actionButton = event.target.closest("[data-action]");
      const card = actionButton.closest(".car-card");
      const id = Number(card.dataset.id);
      const context = card.dataset.context || "inventory";
      const action = actionButton.dataset.action;
      if (action === "details") openDetails(id, context);
      if (action === "wishlist") await toggleWishlist(id);
      if (action === "edit") openListingForm(id);
      if (action === "delete") openDeleteConfirm(id);
    });
  });
}

function openListingForm(id) {
  if (!state.currentUser) {
    openModal(els.authModal);
    showToast("Login before managing listings.");
    return;
  }

  const car = id ? findCarById(id) : null;
  if (car && !canManageCar(car)) {
    showToast("You can only edit your own listings.");
    return;
  }
  document.getElementById("listingModalTitle").textContent = car ? "Edit Listing" : "Add Listing";
  els.listingForm.reset();
  document.getElementById("listingId").value = car?.id || "";
  document.getElementById("listingMake").value = car?.make || "";
  document.getElementById("listingModel").value = car?.model || "";
  document.getElementById("listingYear").value = car?.year || "";
  document.getElementById("listingMileage").value = car?.mileage || "";
  document.getElementById("listingPrice").value = car?.price || "";
  document.getElementById("listingTransmission").value = car?.transmission || "Automatic";
  document.getElementById("listingCondition").value = car?.condition || "Used";
  document.getElementById("listingCategory").value = car?.category || "Sports";
  document.getElementById("listingImage").value = "";
  document.getElementById("listingDescription").value = car?.description || "";
  openModal(els.listingModal);
}

async function handleListingSave(event) {
  event.preventDefault();
  const id = document.getElementById("listingId").value;
  const payload = new FormData();
  const imageInput = document.getElementById("listingImage");

  payload.append("Make", document.getElementById("listingMake").value.trim());
  payload.append("Model", document.getElementById("listingModel").value.trim());
  payload.append("Year", document.getElementById("listingYear").value);
  payload.append("Mileage", document.getElementById("listingMileage").value);
  payload.append("Price", document.getElementById("listingPrice").value);
  payload.append("Transmission", document.getElementById("listingTransmission").value);
  payload.append("Condition", document.getElementById("listingCondition").value);
  payload.append("Category", document.getElementById("listingCategory").value);
  payload.append("Description", document.getElementById("listingDescription").value.trim());

  if (imageInput.files.length > 0) {
    for (const file of imageInput.files) {
      if (file.size > 5 * 1024 * 1024) {
        showToast(`Image ${file.name} is too large. Please select an image under 5MB.`);
        return;
      }
      payload.append("Images", file);
    }
  } else if (!id) {
    showToast("Please upload at least one image.");
    return;
  }

  try {
    if (id) {
      await apiRequest(`/Cars/${id}`, { method: "PUT", body: payload, auth: true });
      showToast("Listing updated.");
    } else {
      await apiRequest("/Cars", { method: "POST", body: payload, auth: true });
      showToast("Listing added.");
    }

    closeAllModals();
    await refreshAllListingData();
  } catch (error) {
    showToast(error.message);
  }
}

function openDeleteConfirm(id) {
  const car = findCarById(id);
  if (!car) return;
  if (!canManageCar(car)) {
    showToast("You can only delete your own listings.");
    return;
  }
  pendingDeleteId = id;
  els.deleteMessage.textContent = `Delete ${car.year} ${car.make} ${car.model}?`;
  openModal(els.deleteModal);
}

async function confirmDelete() {
  try {
    await apiRequest(`/Cars/${pendingDeleteId}`, { method: "DELETE", auth: true });
    pendingDeleteId = null;
    closeAllModals();
    showToast("Listing deleted.");
    await refreshAllListingData();
  } catch (error) {
    showToast(error.message);
  }
}

function openDetails(id, context = "inventory") {
  const car = findCarById(id);
  if (!car) return;
  const canManage = canManageCar(car);
  const isPersonalListing = isOwnListing(car);
  const isWishlisted = state.wishlistIds.includes(String(car.id));
  const manageActions = canManage
    ? `
          <button class="btn btn-muted" type="button" id="detailEditBtn">Edit</button>
          <button class="btn btn-danger" type="button" id="detailDeleteBtn">Delete</button>
      `
    : "";
  const buyerActions = !isPersonalListing
    ? `
          <button class="btn btn-primary" type="button" id="contactSellerBtn">Contact Seller</button>
          <button class="wishlist-fab ${isWishlisted ? "active" : ""}" type="button" id="detailWishlistBtn" aria-label="${isWishlisted ? "Remove from wishlist" : "Add to wishlist"}">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78L12 21.23l7.78-7.78a5.5 5.5 0 0 0 1.06-8.84z"/></svg>
          </button>
      `
    : "";
  const galleryUrls = car.galleryUrls.length > 0 ? car.galleryUrls : [car.imageUrl];
  const detailImages = galleryUrls
    .map((url, index) => `<img src="${escapeAttr(url)}" class="detail-gallery-img ${index === 0 ? "active" : ""}" alt="${escapeAttr(car.make)} ${escapeAttr(car.model)}" />`)
    .join("");
  const galleryNav = galleryUrls.length > 1
    ? `
        <button class="gallery-nav-btn prev" type="button" id="galPrev" aria-label="Previous image">&lsaquo;</button>
        <button class="gallery-nav-btn next" type="button" id="galNext" aria-label="Next image">&rsaquo;</button>
        <div class="gallery-indicators">${galleryUrls.map((_, index) => `<span class="gallery-dot ${index === 0 ? "active" : ""}"></span>`).join("")}</div>
      `
    : "";
  els.detailBody.innerHTML = `
    <div class="detail-grid">
      <div class="detail-gallery">
        ${detailImages}
        ${galleryNav}
      </div>
      <div>
        <div class="detail-title">${escapeHtml(car.year)} ${escapeHtml(car.make)} ${escapeHtml(car.model)}</div>
        <div class="detail-price">${formatCurrency(car.price)}</div>
        <p class="detail-description"><strong>Seller:</strong> ${escapeHtml(car.sellerName)}</p>
        <p class="detail-description">${escapeHtml(car.description)}</p>
        <div class="detail-specs">
          <div class="spec"><span>Mileage</span><strong>${formatNumber(car.mileage)} mi</strong></div>
          <div class="spec"><span>Transmission</span><strong>${escapeHtml(car.transmission)}</strong></div>
          <div class="spec"><span>Condition</span><strong>${escapeHtml(car.condition)}</strong></div>
          <div class="spec"><span>Category</span><strong>${escapeHtml(car.category)}</strong></div>
        </div>
        <div class="detail-actions">
          ${buyerActions}
          ${manageActions}
        </div>
      </div>
    </div>
  `;
  openModal(els.detailModal);
  if (galleryUrls.length > 1) {
    let currentGalleryIndex = 0;
    const detailGalleryImages = els.detailBody.querySelectorAll(".detail-gallery-img");
    const detailGalleryDots = els.detailBody.querySelectorAll(".gallery-dot");
    const updateGallery = nextIndex => {
      detailGalleryImages[currentGalleryIndex].classList.remove("active");
      detailGalleryDots[currentGalleryIndex]?.classList.remove("active");
      currentGalleryIndex = nextIndex;
      if (currentGalleryIndex < 0) currentGalleryIndex = galleryUrls.length - 1;
      if (currentGalleryIndex >= galleryUrls.length) currentGalleryIndex = 0;
      detailGalleryImages[currentGalleryIndex].classList.add("active");
      detailGalleryDots[currentGalleryIndex]?.classList.add("active");
    };
    document.getElementById("galPrev").addEventListener("click", () => updateGallery(currentGalleryIndex - 1));
    document.getElementById("galNext").addEventListener("click", () => updateGallery(currentGalleryIndex + 1));
  }
  const contactSellerBtn = document.getElementById("contactSellerBtn");
  const detailWishlistBtn = document.getElementById("detailWishlistBtn");
  if (contactSellerBtn) contactSellerBtn.addEventListener("click", () => openContactForm(id));
  if (detailWishlistBtn) detailWishlistBtn.addEventListener("click", async () => {
      const changed = await toggleWishlist(id);
      if (changed) {
        closeAllModals();
        openDetails(id, context);
      }
    });
  if (canManage) {
    document.getElementById("detailEditBtn").addEventListener("click", () => { closeAllModals(); openListingForm(id); });
    document.getElementById("detailDeleteBtn").addEventListener("click", () => { closeAllModals(); openDeleteConfirm(id); });
  }
}

async function handleLogin(event) {
  event.preventDefault();
  const email = document.getElementById("loginEmail").value.trim();
  const password = document.getElementById("loginPassword").value;

  try {
    const result = await apiRequest("/Auth/login", { method: "POST", body: { email, password } });
    saveToken(result.token);
    els.authStatus.textContent = `Logged in as ${state.currentUser.email}`;
    updateAuthUi();
    showToast("Logged in.");
    closeAllModals();
    await refreshAllListingData();
  } catch (error) {
    els.authStatus.textContent = error.message;
  }
}

async function handleRegister(event) {
  event.preventDefault();
  const name = document.getElementById("registerName").value.trim();
  const email = document.getElementById("registerEmail").value.trim();
  const phoneNumber = document.getElementById("registerPhone").value.trim();
  const password = document.getElementById("registerPassword").value;

  try {
    await apiRequest("/Auth/register", { method: "POST", body: { name, email, phoneNumber, password } });
    const result = await apiRequest("/Auth/login", { method: "POST", body: { email, password } });
    saveToken(result.token);
    els.authStatus.textContent = `Account created for ${email}`;
    updateAuthUi();
    showToast("Account created and logged in.");
    closeAllModals();
    await refreshAllListingData();
  } catch (error) {
    els.authStatus.textContent = error.message;
  }
}

function switchAuthTab(tab) {
  document.querySelectorAll("[data-auth-tab]").forEach(button => button.classList.toggle("active", button.dataset.authTab === tab));
  els.loginForm.hidden = tab !== "login";
  els.registerForm.hidden = tab !== "register";
  els.authStatus.textContent = "";
}

async function handlePhoneUpdate(event) {
  event.preventDefault();
  const phoneNumber = els.profilePhoneInput.value.trim();

  try {
    const result = await apiRequest("/Auth/phone", {
      method: "PUT",
      auth: true,
      body: { phoneNumber }
    });
    if (result.token) saveToken(result.token);
    state.currentUser.phone = result.phoneNumber || phoneNumber;
    updateAuthUi();
    await refreshAllListingData();
    showToast("Phone number saved.");
  } catch (error) {
    showToast(error.message);
  }
}

function resetFilters() {
  els.filtersForm.reset();
  els.heroSearchInput.value = "";
  document.querySelectorAll(".hero-tag").forEach(tag => tag.classList.remove("active"));
  state.currentPage = 1;
  loadCars();
}

async function toggleWishlist(carId) {
  if (!state.currentUser) {
    openModal(els.authModal);
    showToast("Login before saving cars.");
    return false;
  }

  const id = String(carId);
  const wasSaved = state.wishlistIds.includes(id);

  try {
    if (wasSaved) {
      await apiRequest(`/Wishlist/${carId}`, { method: "DELETE", auth: true });
      showToast("Removed from wishlist.");
    } else {
      await apiRequest(`/Wishlist/${carId}`, { method: "POST", auth: true });
      showToast("Added to wishlist.");
    }

    await loadWishlist();
    return true;
  } catch (error) {
    showToast(error.message);
    return false;
  }
}

async function openWishlistPage() {
  if (!state.currentUser) {
    openModal(els.authModal);
    showToast("Login to see your wishlist.");
    return;
  }

  els.filtersForm.reset();
  els.heroSearchInput.value = "";
  document.querySelectorAll(".hero-tag").forEach(tag => tag.classList.remove("active"));
  showAppPage("wishlist");
  await loadWishlist();
  window.scrollTo({ top: 0, behavior: "smooth" });
}

async function openMyListingsPage() {
  if (!state.currentUser) {
    openModal(els.authModal);
    showToast("Login to see your listings.");
    return;
  }

  showAppPage("myListings");
  await loadMyListings();
  window.scrollTo({ top: 0, behavior: "smooth" });
}

function openAboutPage() {
  showAppPage("about");
  window.scrollTo({ top: 0, behavior: "smooth" });
}

function openContactPage() {
  showAppPage("contact");
  window.scrollTo({ top: 0, behavior: "smooth" });
}

function showInventoryPage() {
  showMainSite();
  document.getElementById("inventory").scrollIntoView({ behavior: "smooth" });
  render();
}

function showAppPage(page) {
  showMainSite();
  document.body.classList.add("show-app-page");
  const pageClassMap = {
    myListings: "show-my-listings-page",
    wishlist: "show-wishlist-page",
    about: "show-about-page",
    contact: "show-contact-page"
  };
  document.body.classList.add(pageClassMap[page] || "show-wishlist-page");
  render();
}

function showMainSite() {
  document.body.classList.remove("show-app-page", "show-wishlist-page", "show-my-listings-page", "show-about-page", "show-contact-page");
}

function openContactForm(carId) {
  if (!state.currentUser) {
    closeAllModals();
    openModal(els.authModal);
    showToast("Login before contacting a seller.");
    return;
  }

  document.getElementById("contactCarId").value = carId;
  const car = findCarById(carId);
  const sellerPhone = car?.sellerPhone || "";
  const sellerPhoneLink = document.getElementById("contactSellerPhone");
  sellerPhoneLink.textContent = sellerPhone || "No phone listed";
  sellerPhoneLink.href = sellerPhone ? `tel:${sellerPhone.replace(/[^\d+]/g, "")}` : "#";
  closeAllModals();
  openModal(els.contactModal);
}

async function handleContactSeller(event) {
  event.preventDefault();
  const carId = Number(document.getElementById("contactCarId").value);
  const message = document.getElementById("contactMessage").value.trim();

  try {
    await apiRequest("/ContactMessages", {
      method: "POST",
      auth: true,
      body: { carId, message }
    });
    closeAllModals();
    showToast("Message sent to seller.");
    await refreshChat();
  } catch (error) {
    showToast(error.message);
  }
}

async function handleChatReply(event) {
  event.preventDefault();
  const text = els.chatReplyInput.value.trim();
  const thread = state.chat.threads[state.chat.activeThreadKey];
  if (!text || !thread) return;

  try {
    await apiRequest("/ContactMessages", {
      method: "POST",
      auth: true,
      body: {
        carId: thread.carId,
        recipientUserId: thread.otherUserId,
        message: text
      }
    });
    els.chatReplyInput.value = "";
    await refreshChat();
  } catch (error) {
    showToast(error.message);
  }
}

function handlePaymentCalculation(event) {
  event.preventDefault();
  const price = Number(document.getElementById("financePrice").value);
  const downPayment = Number(document.getElementById("financeDownPayment").value);
  const annualInterest = Number(document.getElementById("financeInterest").value);
  const months = Number(document.getElementById("financeTerm").value);
  const principal = Math.max(0, price - downPayment);
  const monthlyRate = annualInterest / 100 / 12;
  const monthlyPayment = monthlyRate === 0
    ? principal / months
    : principal * monthlyRate / (1 - Math.pow(1 + monthlyRate, -months));
  const totalPaid = monthlyPayment * months + downPayment;

  els.paymentResult.innerHTML = `
    <span>Estimated monthly payment</span>
    <strong>${formatCurrency(monthlyPayment)}/month</strong>
    <div class="finance-breakdown">
      <div><span>Amount financed</span><b>${formatCurrency(principal)}</b></div>
      <div><span>Total with down payment</span><b>${formatCurrency(totalPaid)}</b></div>
    </div>
    <div class="finance-hint">This is an estimate, not a bank offer.</div>
  `;
}

async function handlePriceEstimate(event) {
  event.preventDefault();
  const defectName = document.getElementById("defectName").value.trim();
  const repairCost = Number(document.getElementById("defectRepairCost").value);
  const defects = defectName
    ? [{
        name: defectName,
        severity: Number(document.getElementById("defectSeverity").value),
        estimatedRepairCost: repairCost
      }]
    : [];

  const payload = {
    make: document.getElementById("estimateMake").value.trim(),
    model: document.getElementById("estimateModel").value.trim(),
    year: Number(document.getElementById("estimateYear").value),
    mileage: Number(document.getElementById("estimateMileage").value),
    baseMarketPrice: Number(document.getElementById("estimateBasePrice").value),
    condition: document.getElementById("estimateCondition").value,
    defects
  };

  els.estimateResult.innerHTML = '<span>Estimated fair price</span><strong>Calculating...</strong>';

  try {
    const result = await apiRequest("/PriceEstimates", { method: "POST", body: payload });
    els.estimateResult.innerHTML = `
      <span>${escapeHtml(result.vehicle || "Estimated fair price")}</span>
      <strong>${formatCurrency(result.estimatedPrice)}</strong>
      <div class="finance-breakdown">
        <div><span>Market price</span><b>${formatCurrency(result.baseMarketPrice)}</b></div>
        <div><span>Age</span><b>${formatCurrency(result.ageAdjustment)}</b></div>
        <div><span>Mileage</span><b>${formatCurrency(result.mileageAdjustment)}</b></div>
        <div><span>Condition</span><b>${formatCurrency(result.conditionAdjustment)}</b></div>
        <div><span>Defects</span><b>${formatCurrency(result.defectAdjustment)}</b></div>
      </div>
      ${renderFinanceNotes(result.notes)}
    `;
  } catch (error) {
    els.estimateResult.innerHTML = `
      <span>Estimated fair price</span>
      <strong>$0</strong>
      <div class="finance-hint">${escapeHtml(error.message)}. Start the backend and try again.</div>
    `;
  }
}

function openModal(modal) {
  closeAllModals();
  modal.classList.add("open");
  document.body.classList.add("modal-open");
}

function closeAllModals() {
  document.querySelectorAll(".modal-backdrop").forEach(modal => modal.classList.remove("open"));
  document.body.classList.remove("modal-open");
}

function showToast(message) {
  els.toast.textContent = message;
  els.toast.classList.add("show");
  clearTimeout(showToast.timer);
  showToast.timer = setTimeout(() => els.toast.classList.remove("show"), 2600);
}

async function apiRequest(path, options = {}) {
  const headers = { Accept: "application/json" };
  const isFormData = options.body instanceof FormData;
  if (options.body && !isFormData) headers["Content-Type"] = "application/json";
  if (options.auth) {
    if (!state.token) throw new Error("Please login first");
    headers.Authorization = `Bearer ${state.token}`;
  }

  const response = await fetch(`${API_BASE_URL}${path}`, {
    method: options.method || "GET",
    headers,
    body: options.body ? (isFormData ? options.body : JSON.stringify(options.body)) : undefined
  });

  const contentType = response.headers.get("content-type") || "";
  const data = response.status === 204
    ? null
    : contentType.includes("application/json")
      ? await response.json()
      : await response.text();

  if (!response.ok) {
    if (response.status === 401 && options.auth) {
      clearSession("Your login expired. Please login again.");
      openModal(els.authModal);
    }

    const message = typeof data === "string" ? data : data?.title || "Request failed";
    throw new Error(message);
  }

  return data;
}

function buildCarQueryString() {
  const params = new URLSearchParams();
  addParam(params, "search", els.filterSearch.value.trim());
  addParam(params, "category", els.filterCategory.value);
  addParam(params, "maxPrice", els.filterMaxPrice.value);
  addParam(params, "minYear", els.filterMinYear.value);
  addParam(params, "transmission", els.filterTransmission.value);
  addParam(params, "page", state.currentPage);
  addParam(params, "pageSize", state.pageSize);

  const sortMap = {
    priceAsc: ["price", "asc"],
    priceDesc: ["price", "desc"],
    yearDesc: ["year", "desc"],
    mileageAsc: ["mileage", "asc"]
  };
  const sort = sortMap[els.filterSort.value];
  if (sort) {
    params.set("sortBy", sort[0]);
    params.set("sortDirection", sort[1]);
  }

  const query = params.toString();
  return query ? `?${query}` : "";
}

function addParam(params, name, value) {
  if (value !== undefined && value !== null && String(value).trim() !== "") {
    params.set(name, value);
  }
}

function normalizeCar(car) {
  const ownerId = String(car.userId || car.ownerId || "");
  const isOwner = Boolean(car.isOwner || (state.currentUser && ownerId === state.currentUser.id));
  const canManage = Boolean(car.canManage || (state.currentUser && (state.currentUser.role === "Admin" || isOwner)));
  const fallbackImage = "https://images.unsplash.com/photo-1492144534655-ae79c964c9d7?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=900";
  const normalizeImageUrl = url => {
    if (!url) return "";
    return url.startsWith("/") ? `${API_ROOT_URL}${url}` : url;
  };
  const imageUrl = normalizeImageUrl(car.imageUrl) || fallbackImage;
  const galleryUrls = (car.galleryUrls || [])
    .map(normalizeImageUrl)
    .filter(Boolean);
  if (galleryUrls.length === 0) galleryUrls.push(imageUrl);

  return {
    id: car.id,
    make: car.make,
    model: car.model,
    year: car.year,
    mileage: car.mileage,
    price: car.price,
    transmission: car.transmission || "Automatic",
    condition: car.condition || "Used",
    category: car.category || "Sports",
    featured: Boolean(car.isFeatured),
    imageUrl,
    galleryUrls,
    description: car.description || "No description has been added for this listing yet.",
    sellerName: car.sellerName || "Private Seller",
    sellerPhone: car.sellerPhone || "",
    isOwner,
    canManage
  };
}

async function refreshAllListingData() {
  await loadCars();
  await loadWishlist();
  if (document.body.classList.contains("show-my-listings-page")) {
    await loadMyListings();
  }
}

function findCarById(id) {
  const allKnownCars = [
    ...state.listings,
    ...state.myListings,
    ...state.wishlistListings
  ];

  return allKnownCars.find(car => car.id === id);
}

function saveToken(token) {
  state.token = token;
  localStorage.setItem(TOKEN_STORAGE_KEY, token);
  restoreUserFromToken();
}

function restoreUserFromToken() {
  if (!state.token) return;
  try {
    const payload = decodeJwtPayload(state.token);
    state.currentUser = {
      id: String(payload.nameid || payload.sub || payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"] || ""),
      name: payload.unique_name || payload.name || payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"] || "Zoomies User",
      email: payload.email || payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"] || "",
      phone: payload.phone_number || payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/mobilephone"] || "",
      role: payload.role || payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || "User"
    };
  } catch (error) {
    logout();
  }
}

function decodeJwtPayload(token) {
  const base64Url = token.split(".")[1];
  const base64 = base64Url.replace(/-/g, "+").replace(/_/g, "/");
  const padded = base64.padEnd(base64.length + (4 - base64.length % 4) % 4, "=");
  return JSON.parse(atob(padded));
}

function updateAuthUi() {
  els.authFormsPanel.hidden = Boolean(state.currentUser);
  els.profilePanel.hidden = !state.currentUser;

  if (state.currentUser) {
    const displayRole = state.currentUser.role || "User";
    const displayName = displayRole.toLowerCase() === "admin"
      ? "Admin"
      : state.currentUser.name || state.currentUser.email || "Zoomies User";
    const displayEmail = state.currentUser.email || "Logged in";

    els.currentAccountName.textContent = displayName;
    els.currentAccountEmail.textContent = displayEmail;
    els.profileName.textContent = displayName;
    els.profileEmail.textContent = displayEmail;
    els.profilePhone.textContent = state.currentUser.phone || "Not set";
    els.profilePhoneInput.value = state.currentUser.phone || "";
    els.profileRole.textContent = displayRole;
    els.authStatus.textContent = "";
    els.chatFab.classList.add("show");
    startSignalR();
    refreshChat();
  } else {
    els.currentAccountName.textContent = "Guest";
    els.currentAccountEmail.textContent = "Not logged in";
    els.profileName.textContent = "Guest";
    els.profileEmail.textContent = "Not logged in";
    els.profilePhone.textContent = "Not set";
    els.profilePhoneInput.value = "";
    els.profileRole.textContent = "Guest";
    els.authStatus.textContent = "Not logged in";
    els.chatFab.classList.remove("show");
    els.chatWidget.classList.remove("open");
    state.chat.isOpen = false;
    state.chat.activeThreadKey = null;
    state.chat.threads = {};
    updateChatUnreadBadge(0);
    stopSignalR();
  }
}

function logout() {
  clearSession("Logged out.");
}

function clearSession(message) {
  state.token = null;
  state.currentUser = null;
  state.myListings = [];
  state.wishlistListings = [];
  state.wishlistIds = [];
  state.chat.threads = {};
  state.chat.activeThreadKey = null;
  localStorage.removeItem(TOKEN_STORAGE_KEY);
  showMainSite();
  updateAuthUi();
  showToast(message);
  switchAuthTab("login");
  render();
}

function canManageCar(car) {
  return Boolean(state.currentUser && car.canManage);
}

function isOwnListing(car) {
  return Boolean(state.currentUser && car.isOwner);
}

function setApiStatus(message, isError = false) {
  els.apiStatus.textContent = `- ${message}`;
  els.apiStatus.classList.toggle("error", isError);
}

async function refreshChat() {
  if (!state.currentUser) return;

  try {
    const [inbox, sent] = await Promise.all([
      apiRequest("/ContactMessages/inbox", { auth: true }),
      apiRequest("/ContactMessages/sent", { auth: true })
    ]);
    const messages = Array.from(new Map([...inbox, ...sent].map(message => [message.id, message])).values())
      .sort((a, b) => new Date(a.createdAt) - new Date(b.createdAt));

    state.chat.threads = {};
    let unreadCount = 0;

    messages.forEach(message => {
      const senderUserId = String(message.senderUserId || "");
      const recipientUserId = String(message.recipientUserId || message.sellerUserId || "");
      const isIncoming = senderUserId !== state.currentUser.id;
      const otherUserId = isIncoming ? senderUserId : recipientUserId;
      if (!otherUserId) return;

      const threadKey = `${message.carId}_${otherUserId}`;
      if (!state.chat.threads[threadKey]) {
        state.chat.threads[threadKey] = {
          carId: message.carId,
          carTitle: message.carTitle || "Vehicle",
          otherUserId,
          otherUserName: isIncoming ? (message.senderName || "Buyer") : "Seller",
          messages: [],
          hasUnread: false
        };
      }

      if (isIncoming && state.chat.threads[threadKey].otherUserName === "Seller") {
        state.chat.threads[threadKey].otherUserName = message.senderName || "Seller";
      }

      state.chat.threads[threadKey].messages.push({ ...message, isIncoming });

      if (isIncoming && !message.isRead) {
        state.chat.threads[threadKey].hasUnread = true;
        unreadCount += 1;
      }
    });

    updateChatUnreadBadge(unreadCount);
    renderChatThreadList();
    if (state.chat.activeThreadKey) renderActiveConversation(state.chat.activeThreadKey);
  } catch (error) {
    console.error("Failed to load chat messages:", error);
  }
}

function toggleChatWidget() {
  if (!state.currentUser) return;
  state.chat.isOpen = !state.chat.isOpen;
  els.chatWidget.classList.toggle("open", state.chat.isOpen);

  if (state.chat.isOpen) {
    if (!state.chat.activeThreadKey) showChatThreadList();
    refreshChat();
  }
}

function updateChatUnreadBadge(count) {
  els.chatUnreadBadge.textContent = count > 99 ? "99+" : String(count);
  els.chatUnreadBadge.hidden = count === 0;
}

function renderChatThreadList() {
  els.chatThreadList.innerHTML = "";
  const threads = Object.entries(state.chat.threads)
    .sort(([, a], [, b]) => {
      const latestA = a.messages[a.messages.length - 1]?.createdAt || 0;
      const latestB = b.messages[b.messages.length - 1]?.createdAt || 0;
      return new Date(latestB) - new Date(latestA);
    });

  if (threads.length === 0) {
    els.chatThreadList.innerHTML = '<div class="empty-state">No messages yet.</div>';
    return;
  }

  threads.forEach(([threadKey, thread]) => {
    const lastMessage = thread.messages[thread.messages.length - 1];
    const item = document.createElement("button");
    item.type = "button";
    item.className = `chat-thread-item ${thread.hasUnread ? "unread" : ""}`;
    item.innerHTML = `
      <div class="chat-thread-header">
        <span class="chat-thread-name">${escapeHtml(thread.otherUserName)}</span>
        <span class="chat-thread-car">${escapeHtml(thread.carTitle)}</span>
      </div>
      <div class="chat-thread-preview">${lastMessage.isIncoming ? "Them" : "You"}: ${escapeHtml(lastMessage.message)}</div>
    `;
    item.addEventListener("click", () => openThread(threadKey));
    els.chatThreadList.appendChild(item);
  });
}

async function openThread(threadKey) {
  state.chat.activeThreadKey = threadKey;
  els.chatThreadList.style.display = "none";
  els.chatConversation.classList.add("active");
  els.chatBackBtn.hidden = false;
  els.chatTitle.textContent = state.chat.threads[threadKey]?.otherUserName || "Messages";
  renderActiveConversation(threadKey);

  const unreadIds = state.chat.threads[threadKey]?.messages
    .filter(message => message.isIncoming && !message.isRead)
    .map(message => message.id) || [];

  for (const id of unreadIds) {
    try {
      await apiRequest(`/ContactMessages/${id}/read`, { method: "PUT", auth: true });
    } catch {
      // Ignore read-receipt failures so the conversation stays usable.
    }
  }

  if (unreadIds.length > 0) await refreshChat();
}

function renderActiveConversation(threadKey) {
  const thread = state.chat.threads[threadKey];
  if (!thread) return;

  els.chatMessages.innerHTML = thread.messages.map(message => `
    <div class="chat-bubble ${message.isIncoming ? "incoming" : "outgoing"}">
      <div>${escapeHtml(message.message)}</div>
      <div class="chat-meta">${formatChatTime(message.createdAt)}</div>
    </div>
  `).join("");
  els.chatMessages.scrollTop = els.chatMessages.scrollHeight;
}

function showChatThreadList() {
  state.chat.activeThreadKey = null;
  els.chatConversation.classList.remove("active");
  els.chatThreadList.style.display = "flex";
  els.chatBackBtn.hidden = true;
  els.chatTitle.textContent = "Messages";
}

function formatChatTime(value) {
  const normalized = typeof value === "string" && !value.endsWith("Z") ? `${value}Z` : value;
  return new Date(normalized).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
}

function debounce(fn, delay) {
  let timer;
  return () => {
    clearTimeout(timer);
    timer = setTimeout(fn, delay);
  };
}

function formatCurrency(value) {
  return new Intl.NumberFormat("en-US", { style: "currency", currency: "USD", maximumFractionDigits: 0 }).format(value);
}

function formatNumber(value) {
  return new Intl.NumberFormat("en-US").format(value);
}

function renderFinanceNotes(notes) {
  if (!Array.isArray(notes) || notes.length === 0) return "";
  return `
    <ul class="finance-note-list">
      ${notes.map(note => `<li>${escapeHtml(note)}</li>`).join("")}
    </ul>
  `;
}

function escapeHtml(value) {
  return String(value).replace(/[&<>"']/g, char => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[char]));
}

function escapeAttr(value) {
  return escapeHtml(value);
}
