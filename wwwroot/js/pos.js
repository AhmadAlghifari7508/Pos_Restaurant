// POS System JavaScript Functions

// Global variables
let currentMenuItems = [];
let currentOrder = {};
let searchTimeout;

// Initialize POS system
document.addEventListener('DOMContentLoaded', function () {
    initializePOS();
});

function initializePOS() {
    // Initialize order summary refresh
    refreshOrderSummary();

    // Set up search functionality
    setupSearchFunctionality();

    // Set up keyboard shortcuts
    setupKeyboardShortcuts();

    console.log('POS System initialized successfully');
}

// Search functionality
function setupSearchFunctionality() {
    const searchInput = document.getElementById('searchInput');
    if (searchInput) {
        searchInput.addEventListener('input', function () {
            performSearch();
        });

        searchInput.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') {
                clearSearch();
            }
        });
    }
}

function performSearch() {
    clearTimeout(searchTimeout);
    const searchInput = document.getElementById('searchInput');
    if (!searchInput) return;

    const searchTerm = searchInput.value.trim();

    searchTimeout = setTimeout(() => {
        if (searchTerm.length >= 2 || searchTerm.length === 0) {
            if (searchTerm.length > 0) {
                searchMenuItems(searchTerm);
            } else {
                loadAllCategories();
            }
        }

        // Show/hide clear button
        const clearBtn = document.getElementById('clearSearchBtn');
        if (clearBtn) {
            if (searchTerm.length > 0) {
                clearBtn.classList.remove('hidden');
            } else {
                clearBtn.classList.add('hidden');
            }
        }
    }, 500);
}

function searchMenuItems(searchTerm) {
    showLoading();

    fetch(`/Home/SearchMenuItems?searchTerm=${encodeURIComponent(searchTerm)}`)
        .then(response => {
            if (!response.ok) throw new Error('Network response was not ok');
            return response.text();
        })
        .then(html => {
            const container = document.getElementById('menuItemsContainer');
            if (container) {
                container.innerHTML = html;
            }
            hideLoading();
            updateCategoryTabsForSearch(searchTerm);
        })
        .catch(error => {
            console.error('Error searching menu items:', error);
            hideLoading();
            showNotification('Terjadi kesalahan saat mencari menu', 'error');
        });
}

function clearSearch() {
    const searchInput = document.getElementById('searchInput');
    const clearBtn = document.getElementById('clearSearchBtn');

    if (searchInput) searchInput.value = '';
    if (clearBtn) clearBtn.classList.add('hidden');

    // Load all categories when clearing search
    loadAllCategories();
}

function updateCategoryTabsForSearch(searchTerm) {
    const categoryTabs = document.querySelectorAll('.category-tab');
    categoryTabs.forEach(tab => {
        if (searchTerm) {
            tab.classList.remove('border-b-2', 'border-blue-500', 'text-blue-600');
            tab.classList.add('text-gray-600');
        }
    });
}

// Category functions
function loadAllCategories() {
    showLoading();

    // Clear search when switching to all categories
    const searchInput = document.getElementById('searchInput');
    const clearBtn = document.getElementById('clearSearchBtn');

    if (searchInput && searchInput.value) {
        searchInput.value = '';
    }
    if (clearBtn) clearBtn.classList.add('hidden');

    fetch('/Home/GetMenuByCategory?categoryId=0')
        .then(response => {
            if (!response.ok) throw new Error('Network response was not ok');
            return response.text();
        })
        .then(html => {
            const container = document.getElementById('menuItemsContainer');
            if (container) {
                container.innerHTML = html;
            }
            hideLoading();

            // Update active category tab
            updateActiveCategoryTab(0);
        })
        .catch(error => {
            console.error('Error loading all categories:', error);
            hideLoading();
            showNotification('Terjadi kesalahan saat memuat semua menu', 'error');
        });
}

function loadCategory(categoryId) {
    showLoading();

    // Clear search when switching categories
    const searchInput = document.getElementById('searchInput');
    const clearBtn = document.getElementById('clearSearchBtn');

    if (searchInput) searchInput.value = '';
    if (clearBtn) clearBtn.classList.add('hidden');

    fetch(`/Home/GetMenuByCategory?categoryId=${categoryId}`)
        .then(response => {
            if (!response.ok) throw new Error('Network response was not ok');
            return response.text();
        })
        .then(html => {
            const container = document.getElementById('menuItemsContainer');
            if (container) {
                container.innerHTML = html;
            }
            hideLoading();

            // Update active category tab
            updateActiveCategoryTab(categoryId);
        })
        .catch(error => {
            console.error('Error loading category:', error);
            hideLoading();
            showNotification('Terjadi kesalahan saat memuat kategori', 'error');
        });
}

function updateActiveCategoryTab(categoryId) {
    document.querySelectorAll('.category-tab').forEach(tab => {
        const tabCategoryId = parseInt(tab.getAttribute('data-category-id'));
        if (tabCategoryId === categoryId) {
            tab.classList.add('border-b-2', 'border-blue-500', 'text-blue-600');
            tab.classList.remove('text-gray-600');
        } else {
            tab.classList.remove('border-b-2', 'border-blue-500', 'text-blue-600');
            tab.classList.add('text-gray-600');
        }
    });
}

// Order management functions
function addToOrder(menuItemId, itemName, price, note = '') {
    const formData = new FormData();
    formData.append('menuItemId', menuItemId);
    formData.append('quantity', 1);
    formData.append('note', note);
    formData.append('__RequestVerificationToken', getAntiForgeryToken());

    fetch('/Home/AddToOrder', {
        method: 'POST',
        body: formData
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                refreshOrderSummary();
                showNotification(data.message, 'success');

                // Add visual feedback to the menu item
                addVisualFeedback(menuItemId);
            } else {
                showNotification(data.message || 'Terjadi kesalahan', 'error');
            }
        })
        .catch(error => {
            console.error('Error adding to order:', error);
            showNotification('Terjadi kesalahan saat menambahkan item', 'error');
        });
}

function updateQuantity(menuItemId, newQuantity) {
    if (newQuantity <= 0) {
        removeFromOrder(menuItemId);
        return;
    }

    const formData = new FormData();
    formData.append('menuItemId', menuItemId);
    formData.append('quantity', newQuantity);
    formData.append('__RequestVerificationToken', getAntiForgeryToken());

    fetch('/Home/UpdateOrderItemQuantity', {
        method: 'POST',
        body: formData
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                refreshOrderSummary();
                showNotification('Quantity berhasil diperbarui', 'success');
            } else {
                showNotification(data.message || 'Terjadi kesalahan', 'error');
            }
        })
        .catch(error => {
            console.error('Error updating quantity:', error);
            showNotification('Terjadi kesalahan saat mengupdate quantity', 'error');
        });
}

function removeFromOrder(menuItemId) {
    if (!confirm('Apakah Anda yakin ingin menghapus item ini dari order?')) {
        return;
    }

    const formData = new FormData();
    formData.append('menuItemId', menuItemId);
    formData.append('__RequestVerificationToken', getAntiForgeryToken());

    fetch('/Home/RemoveFromOrder', {
        method: 'POST',
        body: formData
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                refreshOrderSummary();
                showNotification(data.message, 'success');
            } else {
                showNotification(data.message || 'Terjadi kesalahan', 'error');
            }
        })
        .catch(error => {
            console.error('Error removing from order:', error);
            showNotification('Terjadi kesalahan saat menghapus item', 'error');
        });
}

function clearOrder() {
    if (!confirm('Apakah Anda yakin ingin menghapus semua item dari order?')) {
        return;
    }

    const formData = new FormData();
    formData.append('__RequestVerificationToken', getAntiForgeryToken());

    fetch('/Home/ClearOrder', {
        method: 'POST',
        body: formData
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                refreshOrderSummary();
                showNotification('Order berhasil dihapus', 'success');
            } else {
                showNotification(data.message || 'Terjadi kesalahan', 'error');
            }
        })
        .catch(error => {
            console.error('Error clearing order:', error);
            showNotification('Terjadi kesalahan saat menghapus order', 'error');
        });
}

function refreshOrderSummary() {
    fetch('/Home/GetCurrentOrder')
        .then(response => {
            if (!response.ok) throw new Error('Network response was not ok');
            return response.text();
        })
        .then(html => {
            const container = document.getElementById('orderSummaryContainer');
            if (container) {
                container.innerHTML = html;
            }
        })
        .catch(error => {
            console.error('Error refreshing order summary:', error);
            showNotification('Terjadi kesalahan saat memuat order', 'error');
        });
}

// Payment modal functions
function showPaymentModal() {
    // First check if there's any order
    fetch('/Home/GetOrderSummaryData')
        .then(response => response.json())
        .then(data => {
            if (!data.success || data.data.isEmpty) {
                showNotification('Tidak ada item dalam order', 'warning');
                return;
            }

            const modal = document.getElementById('paymentModal');
            if (modal) {
                modal.classList.remove('hidden');
                modal.classList.add('flex');
                document.body.style.overflow = 'hidden';

                // Initialize payment modal
                if (typeof updatePaymentSummary === 'function') {
                    updatePaymentSummary();
                }
            }
        })
        .catch(error => {
            console.error('Error checking order:', error);
            showNotification('Terjadi kesalahan saat membuka pembayaran', 'error');
        });
}

function hidePaymentModal() {
    const modal = document.getElementById('paymentModal');
    if (modal) {
        modal.classList.add('hidden');
        modal.classList.remove('flex');
        document.body.style.overflow = 'auto';
    }
}

// Visual feedback functions
function addVisualFeedback(menuItemId) {
    const menuItem = document.querySelector(`[data-menu-id="${menuItemId}"]`);
    if (menuItem) {
        menuItem.classList.add('animate-pulse');
        setTimeout(() => {
            menuItem.classList.remove('animate-pulse');
        }, 500);
    }
}

// Loading functions
function showLoading() {
    const overlay = document.getElementById('loadingOverlay');
    if (overlay) {
        overlay.classList.remove('hidden');
        overlay.classList.add('flex');
    }
}

function hideLoading() {
    const overlay = document.getElementById('loadingOverlay');
    if (overlay) {
        overlay.classList.add('hidden');
        overlay.classList.remove('flex');
    }
}

// Notification system
function showNotification(message, type = 'info', duration = 3000) {
    // Remove existing notifications
    const existingNotifications = document.querySelectorAll('.notification');
    existingNotifications.forEach(notification => notification.remove());

    const notification = document.createElement('div');
    notification.className = `notification fixed top-4 right-4 px-6 py-3 rounded-lg shadow-lg z-50 transform transition-all duration-300 translate-x-full`;

    let bgColor, textColor, icon;
    switch (type) {
        case 'success':
            bgColor = 'bg-green-500';
            textColor = 'text-white';
            icon = 'fas fa-check-circle';
            break;
        case 'error':
            bgColor = 'bg-red-500';
            textColor = 'text-white';
            icon = 'fas fa-exclamation-circle';
            break;
        case 'warning':
            bgColor = 'bg-yellow-500';
            textColor = 'text-white';
            icon = 'fas fa-exclamation-triangle';
            break;
        default:
            bgColor = 'bg-blue-500';
            textColor = 'text-white';
            icon = 'fas fa-info-circle';
    }

    notification.className += ` ${bgColor} ${textColor}`;
    notification.innerHTML = `
        <div class="flex items-center space-x-2">
            <i class="${icon}"></i>
            <span>${message}</span>
        </div>
    `;

    document.body.appendChild(notification);

    // Animate in
    setTimeout(() => {
        notification.classList.remove('translate-x-full');
    }, 10);

    // Auto remove
    setTimeout(() => {
        notification.classList.add('translate-x-full');
        setTimeout(() => {
            notification.remove();
        }, 300);
    }, duration);
}

// Utility functions
function getAntiForgeryToken() {
    const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
    return tokenElement ? tokenElement.value : '';
}

function formatCurrency(amount) {
    return new Intl.NumberFormat('id-ID', {
        style: 'currency',
        currency: 'IDR',
        minimumFractionDigits: 0,
        maximumFractionDigits: 0
    }).format(amount);
}

function formatNumber(number) {
    return new Intl.NumberFormat('id-ID').format(number);
}

// Keyboard shortcuts
function setupKeyboardShortcuts() {
    document.addEventListener('keydown', function (e) {
        // Only process shortcuts if not typing in input fields
        if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA') {
            return;
        }

        switch (e.key) {
            case 'F1':
                e.preventDefault();
                // Focus on search
                const searchInput = document.getElementById('searchInput');
                if (searchInput) searchInput.focus();
                break;
            case 'F2':
                e.preventDefault();
                showPaymentModal();
                break;
            case 'F3':
                e.preventDefault();
                clearOrder();
                break;
            case 'Escape':
                e.preventDefault();
                hidePaymentModal();
                break;
        }
    });
}

// Export functions for global access
window.POS = {
    addToOrder: addToOrder,
    updateQuantity: updateQuantity,
    removeFromOrder: removeFromOrder,
    clearOrder: clearOrder,
    loadCategory: loadCategory,
    loadAllCategories: loadAllCategories,
    showPaymentModal: showPaymentModal,
    hidePaymentModal: hidePaymentModal,
    refreshOrderSummary: refreshOrderSummary,
    showNotification: showNotification,
    searchMenuItems: searchMenuItems,
    clearSearch: clearSearch,
    performSearch: performSearch
};

// Make functions globally available (for backward compatibility)
window.addToOrder = addToOrder;
window.updateQuantity = updateQuantity;
window.removeFromOrder = removeFromOrder;
window.clearOrder = clearOrder;
window.loadCategory = loadCategory;
window.loadAllCategories = loadAllCategories;
window.showPaymentModal = showPaymentModal;
window.hidePaymentModal = hidePaymentModal;
window.refreshOrderSummary = refreshOrderSummary;
window.showNotification = showNotification;
window.showLoading = showLoading;
window.hideLoading = hideLoading;
window.searchMenuItems = searchMenuItems;
window.clearSearch = clearSearch;
window.performSearch = performSearch;