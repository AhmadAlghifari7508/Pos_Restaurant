let currentMenuItems = [];
let currentOrder = {};
let searchTimeout;

document.addEventListener('DOMContentLoaded', function () {
    initializePOS();
});

function initializePOS() {
    refreshOrderSummary();
    setupSearchFunctionality();
    setupKeyboardShortcuts();
    console.log('Enhanced POS System with Menu Discounts and Slide Payment initialized successfully');
}

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
                updateItemCountAndDiscountInfo(container);
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

function updateItemCountAndDiscountInfo(container) {
    const itemCountElement = document.getElementById('itemCount');
    if (itemCountElement) {
        const menuItems = container.querySelectorAll('.menu-item');
        const discountItems = container.querySelectorAll('.menu-item[data-has-discount="true"]');

        let countText = `${menuItems.length} item(s)`;
        if (discountItems.length > 0) {
            countText += ` (${discountItems.length} dengan diskon)`;
        }

        itemCountElement.innerHTML = countText;
    }
}

function loadAllCategories() {
    showLoading();

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
                updateItemCountAndDiscountInfo(container);
            }
            hideLoading();
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
                updateItemCountAndDiscountInfo(container);
            }
            hideLoading();
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

function addToOrder(menuItemId, itemName, price, note = '') {
    const menuItem = document.querySelector(`[data-menu-id="${menuItemId}"]`);
    const hasDiscount = menuItem ? menuItem.getAttribute('data-has-discount') === 'true' : false;
    const originalPrice = menuItem ? parseFloat(menuItem.getAttribute('data-original-price')) || price : price;
    const discountPercentage = menuItem ? parseFloat(menuItem.getAttribute('data-discount-percentage')) || 0 : 0;
    const discountAmount = menuItem ? parseFloat(menuItem.getAttribute('data-discount-amount')) || 0 : 0;

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

                let message = data.message;
                if (data.hasDiscount && data.discountInfo) {
                    message += ` ${data.discountInfo}`;
                } else if (hasDiscount) {
                    message += ` (Hemat Rp ${discountAmount.toLocaleString('id-ID')})`;
                }

                showNotification(message, 'success');
                addVisualFeedbackWithDiscount(menuItemId, hasDiscount);
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
                if (newQuantity > 5) {
                    showNotification(`Quantity diperbarui ke ${newQuantity}`, 'success', 1500);
                }
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

            initializeDiscountToggle();
        })
        .catch(error => {
            console.error('Error refreshing order summary:', error);
            showNotification('Terjadi kesalahan saat memuat order', 'error');
        });
}

function initializeDiscountToggle() {
    const discountToggle = document.getElementById('discountToggle');
    if (discountToggle) {
        const newToggle = discountToggle.cloneNode(true);
        discountToggle.parentNode.replaceChild(newToggle, discountToggle);

        newToggle.addEventListener('change', function () {
            toggleDiscount(this.checked);
        });
    }
    updateDiscountLabel();
}

function toggleDiscount(isApplied) {
    const formData = new FormData();
    formData.append('applyDiscount', isApplied);
    formData.append('__RequestVerificationToken', getAntiForgeryToken());

    fetch('/Home/ApplyDiscount', {
        method: 'POST',
        body: formData
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                refreshOrderSummary();
                showNotification(data.message, 'success');
            } else {
                const discountToggle = document.getElementById('discountToggle');
                if (discountToggle) {
                    discountToggle.checked = !isApplied;
                }
                showNotification(data.message || 'Terjadi kesalahan', 'error');
            }
        })
        .catch(error => {
            console.error('Error applying discount:', error);
            const discountToggle = document.getElementById('discountToggle');
            if (discountToggle) {
                discountToggle.checked = !isApplied;
            }
            showNotification('Terjadi kesalahan saat menerapkan diskon', 'error');
        });
}

function updateDiscountLabel() {
    fetch('/Home/GetOrderSummaryData')
        .then(response => response.json())
        .then(data => {
            if (data.success && data.data.discountPercentage) {
                const discountLabel = document.getElementById('discountLabel');
                if (discountLabel) {
                    discountLabel.textContent = `Diskon Order (${data.data.discountPercentage}%):`;
                }
            }
        })
        .catch(error => {
            console.error('Error updating discount label:', error);
        });
}

function updateItemNote(menuItemId, note) {
    const formData = new FormData();
    formData.append('menuItemId', menuItemId);
    formData.append('note', note ? note.trim() : ''); 
    formData.append('__RequestVerificationToken', getAntiForgeryToken());

    fetch('/Home/UpdateItemNote', {
        method: 'POST',
        body: formData
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
               
                console.log('Note saved successfully for item:', menuItemId, '- Note:', data.note);
            } else {
                showNotification(data.message || 'Terjadi kesalahan', 'error');
            }
        })
        .catch(error => {
            console.error('Error saving note:', error);
            showNotification('Terjadi kesalahan saat menyimpan catatan', 'error');
        });
}

function showSlidePayment() {
    fetch('/Home/GetOrderSummaryData')
        .then(response => response.json())
        .then(data => {
            if (!data.success || data.data.isEmpty) {
                showNotification('Tidak ada item dalam order', 'warning');
                return;
            }

            const slideContainer = document.getElementById('slideContainer');
            if (slideContainer) {
                slideContainer.classList.add('slide-left');
                setTimeout(updateSlidePaymentSummary, 300);
            }

            if (data.data.totalSavings && data.data.totalSavings > 0) {
                showNotification(`Total penghematan: Rp ${data.data.totalSavings.toLocaleString('id-ID')}`, 'discount', 5000);
            }
        })
        .catch(error => {
            console.error('Error checking order:', error);
            showNotification('Terjadi kesalahan saat membuka pembayaran', 'error');
        });
}

function hideSlidePayment() {
    const slideContainer = document.getElementById('slideContainer');
    if (slideContainer) {
        slideContainer.classList.remove('slide-left');
    }
    resetSlidePaymentForm();
}

function updateSlidePaymentSummary() {
    fetch('/Home/GetOrderSummaryData')
        .then(response => response.json())
        .then(data => {
            if (data.success && !data.data.isEmpty) {
                const orderData = data.data;
                const totalElement = document.getElementById('paymentTotalAmount');
                if (totalElement) {
                    totalElement.textContent = `Rp ${orderData.total.toLocaleString('id-ID')}`;
                    calculateSlideChange();
                }
            }
        })
        .catch(error => {
            console.error('Error updating payment summary:', error);
        });
}

function calculateSlideChange() {
    const cashInput = document.getElementById('slideCashAmount');
    const totalElement = document.getElementById('paymentTotalAmount');

    if (!cashInput || !totalElement) return;

    const cash = parseFloat(cashInput.value) || 0;
    const totalText = totalElement.textContent;
    const total = parseFloat(totalText.replace(/[Rp.,\s]/g, '')) || 0;

    const change = Math.max(0, cash - total);

    const cashReceivedElement = document.getElementById('slideCashReceived');
    const paymentTotalElement = document.getElementById('slidePaymentTotal');
    const changeElement = document.getElementById('slideChange');
    const confirmBtn = document.getElementById('slideConfirmBtn');

    if (cashReceivedElement) {
        cashReceivedElement.textContent = `Rp ${cash.toLocaleString('id-ID')}`;
    }
    if (paymentTotalElement) {
        paymentTotalElement.textContent = `Rp ${total.toLocaleString('id-ID')}`;
    }
    if (changeElement) {
        changeElement.textContent = `Rp ${change.toLocaleString('id-ID')}`;
    }

    if (confirmBtn) {
        if (cash >= total && total > 0) {
            confirmBtn.disabled = false;
            confirmBtn.classList.remove('opacity-50', 'cursor-not-allowed');
            changeElement?.classList.remove('text-red-600');
            changeElement?.classList.add('text-green-600');
        } else {
            confirmBtn.disabled = true;
            confirmBtn.classList.add('opacity-50', 'cursor-not-allowed');
            changeElement?.classList.remove('text-green-600');
            changeElement?.classList.add('text-red-600');
        }
    }
}

function toggleSlideTableNo() {
    const orderTypeRadios = document.querySelectorAll('input[name="slideOrderType"]');
    const checkedRadio = document.querySelector('input[name="slideOrderType"]:checked');
    const tableNoContainer = document.getElementById('slideTableNoContainer');
    const tableNoInput = document.getElementById('slideTableNo');

    if (checkedRadio && tableNoContainer && tableNoInput) {
        if (checkedRadio.value === 'Dine In') {
            tableNoContainer.style.display = 'block';
            tableNoInput.required = true;
        } else {
            tableNoContainer.style.display = 'none';
            tableNoInput.required = false;
            tableNoInput.value = '';
        }
    }
}

function processSlidePayment() {
    const customerName = document.getElementById('slideCustomerName')?.value || '';
    const orderTypeRadio = document.querySelector('input[name="slideOrderType"]:checked');
    const tableNo = document.getElementById('slideTableNo')?.value || '';
    const cash = parseFloat(document.getElementById('slideCashAmount')?.value || '0');

    if (!orderTypeRadio) {
        showNotification('Pilih tipe pesanan', 'error');
        return;
    }

    const orderType = orderTypeRadio.value;

    if (orderType === 'Dine In' && (!tableNo || parseInt(tableNo) <= 0)) {
        showNotification('Nomor meja harus diisi untuk Dine In', 'error');
        return;
    }

    if (cash <= 0) {
        showNotification('Jumlah cash harus diisi', 'error');
        return;
    }

    const totalElement = document.getElementById('paymentTotalAmount');
    if (totalElement) {
        const total = parseFloat(totalElement.textContent.replace(/[Rp.,\s]/g, '')) || 0;
        if (cash < total) {
            showNotification('Jumlah cash tidak mencukupi', 'error');
            return;
        }
    }

    const formData = new FormData();
    formData.append('CustomerName', customerName);
    formData.append('OrderType', orderType);
    if (orderType === 'Dine In') {
        formData.append('TableNo', tableNo);
    }
    formData.append('PaymentMethod', 'Cash');
    formData.append('Cash', cash);
    formData.append('__RequestVerificationToken', getAntiForgeryToken());

    showLoading();

    fetch('/Home/ProcessPayment', {
        method: 'POST',
        body: formData
    })
        .then(response => response.json())
        .then(data => {
            hideLoading();

            if (data.success) {
                hideSlidePayment();
                showNotification('Pembayaran berhasil diproses!', 'success');

                if (data.totalSavings && data.totalSavings > 0) {
                    setTimeout(() => {
                        showNotification(`Total penghematan: Rp ${data.totalSavings.toLocaleString('id-ID')}`, 'discount', 5000);
                    }, 2000);
                }

                if (data.shouldPrintReceipt && data.orderId) {
                    setTimeout(() => {
                        if (confirm('Pembayaran berhasil! Cetak struk sekarang?')) {
                            showReceiptPreview(data.orderId);
                        } else {
                            showNotification('Struk dapat dicetak dari menu Dashboard', 'info', 5000);
                        }
                    }, 2500);
                }

                setTimeout(() => {
                    refreshOrderSummary();
                }, 1500);
            } else {
                showNotification(data.message || 'Terjadi kesalahan dalam pembayaran', 'error');
            }
        })
        .catch(error => {
            hideLoading();
            console.error('Payment error:', error);
            showNotification('Terjadi kesalahan sistem', 'error');
        });
}

function resetSlidePaymentForm() {
    const customerNameInput = document.getElementById('slideCustomerName');
    const cashInput = document.getElementById('slideCashAmount');
    const tableNoInput = document.getElementById('slideTableNo');
    const orderTypeSelect = document.getElementById('slideOrderType');

    if (customerNameInput) customerNameInput.value = '';
    if (cashInput) cashInput.value = '';
    if (tableNoInput) tableNoInput.value = '';
    if (orderTypeSelect) orderTypeSelect.value = 'Dine In';

    toggleSlideTableNo();
    calculateSlideChange();
}

function showPaymentModal() {
    showSlidePayment();
}

function hidePaymentModal() {
    hideSlidePayment();
}

function addVisualFeedbackWithDiscount(menuItemId, hasDiscount) {
    const menuItem = document.querySelector(`[data-menu-id="${menuItemId}"]`);
    if (menuItem) {
        if (hasDiscount) {
            menuItem.classList.add('animate-pulse');
            menuItem.style.transform = 'scale(1.05)';
            menuItem.style.boxShadow = '0 0 15px rgba(239, 68, 68, 0.6)';
            setTimeout(() => {
                menuItem.style.transform = '';
                menuItem.style.boxShadow = '';
                menuItem.classList.remove('animate-pulse');
            }, 800);
        } else {
            menuItem.classList.add('animate-pulse');
            setTimeout(() => {
                menuItem.classList.remove('animate-pulse');
            }, 500);
        }
    }
}

function addVisualFeedback(menuItemId) {
    addVisualFeedbackWithDiscount(menuItemId, false);
}

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

function showNotification(message, type = 'info', duration = 3000) {
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
        case 'discount':
            bgColor = 'bg-purple-500';
            textColor = 'text-white';
            icon = 'fas fa-tags';
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

    setTimeout(() => {
        notification.classList.remove('translate-x-full');
    }, 10);

    setTimeout(() => {
        notification.classList.add('translate-x-full');
        setTimeout(() => {
            notification.remove();
        }, 300);
    }, duration);
}

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

function setupKeyboardShortcuts() {
    document.addEventListener('keydown', function (e) {
        if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA') {
            if (e.key === 'Escape') {
                const slideContainer = document.getElementById('slideContainer');
                if (slideContainer && slideContainer.classList.contains('slide-left')) {
                    e.preventDefault();
                    hideSlidePayment();
                }
            }
            return;
        }

        switch (e.key) {
            case 'F1':
                e.preventDefault();
                const searchInput = document.getElementById('searchInput');
                if (searchInput) searchInput.focus();
                break;
            case 'F2':
                e.preventDefault();
                showSlidePayment();
                break;
            case 'F3':
                e.preventDefault();
                clearOrder();
                break;
            case 'F4':
                e.preventDefault();
                showDiscountedItems();
                break;
            case 'Escape':
                e.preventDefault();
                hideSlidePayment();
                break;
        }
    });
}

function showDiscountedItems() {
    const menuItems = document.querySelectorAll('.menu-item');
    let hasDiscountItems = false;

    menuItems.forEach(item => {
        const hasDiscount = item.getAttribute('data-has-discount') === 'true';
        if (hasDiscount) {
            item.style.display = 'block';
            hasDiscountItems = true;
        } else {
            item.style.display = 'none';
        }
    });

    if (hasDiscountItems) {
        showNotification('Menampilkan hanya menu dengan diskon (tekan F4 lagi untuk reset)', 'discount', 4000);
        const resetHandler = function (e) {
            if (e.key === 'F4') {
                e.preventDefault();
                menuItems.forEach(item => {
                    item.style.display = 'block';
                });
                showNotification('Tampilan direset, menampilkan semua menu', 'info', 2000);
                document.removeEventListener('keydown', resetHandler);
            }
        };
        document.addEventListener('keydown', resetHandler);
    } else {
        showNotification('Tidak ada menu dengan diskon aktif', 'warning');
    }
}

function getDiscountInfo(menuItemId) {
    const menuItem = document.querySelector(`[data-menu-id="${menuItemId}"]`);
    if (!menuItem) return null;

    return {
        hasDiscount: menuItem.getAttribute('data-has-discount') === 'true',
        originalPrice: parseFloat(menuItem.getAttribute('data-original-price')) || 0,
        finalPrice: parseFloat(menuItem.getAttribute('data-price')) || 0,
        discountPercentage: parseFloat(menuItem.getAttribute('data-discount-percentage')) || 0,
        discountAmount: parseFloat(menuItem.getAttribute('data-discount-amount')) || 0
    };
}

function highlightDiscountedItems() {
    const discountedItems = document.querySelectorAll('.menu-item[data-has-discount="true"]');
    discountedItems.forEach(item => {
        item.style.animation = 'discount-highlight 2s ease-in-out infinite alternate';
    });

    setTimeout(() => {
        discountedItems.forEach(item => {
            item.style.animation = '';
        });
    }, 10000);
}

function autoPrintToPhysicalPrinter(orderId) {
    if (!orderId) {
        console.error('Order ID tidak valid');
        return;
    }

    console.log('Auto printing receipt for order:', orderId);

    const printUrl = `/Receipt/Print?orderId=${orderId}`;
    const printWindow = window.open(
        printUrl,
        '_blank',
        'width=400,height=600,menubar=no,toolbar=no,location=no,status=no'
    );

    if (!printWindow) {
        showNotification('Pop-up diblokir! Silakan izinkan pop-up untuk mencetak.', 'warning', 5000);
    }
}

function showPrintConfirmation(orderId, totalSavings) {
    const modalHtml = `
        <div id="printConfirmModal" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
            <div class="bg-white rounded-lg shadow-2xl p-6 max-w-md w-full mx-4">
                <div class="text-center">
                    <div class="mx-auto flex items-center justify-center h-16 w-16 rounded-full bg-green-100 mb-4">
                        <i class="fas fa-check text-green-600 text-3xl"></i>
                    </div>
                    <h3 class="text-lg font-bold text-gray-900 mb-2">Pembayaran Berhasil!</h3>
                    ${totalSavings > 0 ? `
                        <div class="bg-green-50 border border-green-200 rounded-lg p-3 mb-4">
                            <p class="text-green-700 font-semibold">
                                <i class="fas fa-tags mr-2"></i>
                                Hemat: Rp ${totalSavings.toLocaleString('id-ID')}
                            </p>
                        </div>
                    ` : ''}
                    <p class="text-gray-700 mb-6">Cetak struk sekarang?</p>
                    <div class="flex space-x-3">
                        <button onclick="closePrintModal()" 
                                class="flex-1 px-4 py-2 bg-gray-200 hover:bg-gray-300 text-gray-800 rounded-lg">
                            Nanti
                        </button>
                        <button onclick="printNow(${orderId})" 
                                class="flex-1 px-4 py-2 bg-green-500 hover:bg-green-600 text-white rounded-lg">
                            <i class="fas fa-print mr-2"></i>Cetak
                        </button>
                    </div>
                </div>
            </div>
        </div>
    `;
    document.body.insertAdjacentHTML('beforeend', modalHtml);
}

function closePrintModal() {
    const modal = document.getElementById('printConfirmModal');
    if (modal) modal.remove();
}

function printNow(orderId) {
    closePrintModal();
    autoPrintToPhysicalPrinter(orderId);
}

function showReceiptPreview(orderId) {
    if (!orderId) {
        console.error('Order ID tidak valid untuk preview');
        return;
    }

    autoPrintToPhysicalPrinter(orderId);
}

window.POS = {
    addToOrder: addToOrder,
    updateQuantity: updateQuantity,
    removeFromOrder: removeFromOrder,
    clearOrder: clearOrder,
    loadCategory: loadCategory,
    loadAllCategories: loadAllCategories,
    showPaymentModal: showSlidePayment,
    hidePaymentModal: hideSlidePayment,
    showSlidePayment: showSlidePayment,
    hideSlidePayment: hideSlidePayment,
    refreshOrderSummary: refreshOrderSummary,
    showNotification: showNotification,
    searchMenuItems: searchMenuItems,
    clearSearch: clearSearch,
    performSearch: performSearch,
    calculateSlideChange: calculateSlideChange,
    toggleSlideTableNo: toggleSlideTableNo,
    processSlidePayment: processSlidePayment,
    updateSlidePaymentSummary: updateSlidePaymentSummary,
    resetSlidePaymentForm: resetSlidePaymentForm,
    showDiscountedItems: showDiscountedItems,
    highlightDiscountedItems: highlightDiscountedItems,
    getDiscountInfo: getDiscountInfo,
    updateItemNote: updateItemNote
};

window.addToOrder = addToOrder;
window.updateQuantity = updateQuantity;
window.removeFromOrder = removeFromOrder;
window.clearOrder = clearOrder;
window.loadCategory = loadCategory;
window.loadAllCategories = loadAllCategories;
window.showPaymentModal = showSlidePayment;
window.hidePaymentModal = hideSlidePayment;
window.showSlidePayment = showSlidePayment;
window.hideSlidePayment = hideSlidePayment;
window.refreshOrderSummary = refreshOrderSummary;
window.showNotification = showNotification;
window.showLoading = showLoading;
window.hideLoading = hideLoading;
window.searchMenuItems = searchMenuItems;
window.clearSearch = clearSearch;
window.performSearch = performSearch;
window.calculateSlideChange = calculateSlideChange;
window.toggleSlideTableNo = toggleSlideTableNo;
window.processSlidePayment = processSlidePayment;
window.updateSlidePaymentSummary = updateSlidePaymentSummary;
window.resetSlidePaymentForm = resetSlidePaymentForm;
window.autoPrintToPhysicalPrinter = autoPrintToPhysicalPrinter;
window.showPrintConfirmation = showPrintConfirmation;
window.closePrintModal = closePrintModal;
window.printNow = printNow;
window.showReceiptPreview = showReceiptPreview;
window.updateItemNote = updateItemNote;

const enhancedCSS = `
    @keyframes discount-highlight {
        from { box-shadow: 0 0 10px rgba(239, 68, 68, 0.5); }
        to { box-shadow: 0 0 20px rgba(239, 68, 68, 0.8), 0 0 30px rgba(239, 68, 68, 0.3); }
    }
    
    .menu-item[data-has-discount="true"]:hover {
        transform: translateY(-2px);
        transition: transform 0.2s ease;
    }
    
    .notification.bg-purple-500 {
        background: linear-gradient(135deg, #8b5cf6 0%, #a855f7 100%);
    }

    .slide-container {
        transition: transform 0.3s ease-in-out;
    }
    
    .slide-left {
        transform: translateX(-50%);
    }

    input[type="number"]::-webkit-outer-spin-button,
    input[type="number"]::-webkit-inner-spin-button {
        -webkit-appearance: none;
        margin: 0;
    }

    input[type="number"] {
        -moz-appearance: textfield;
    }

    input[type="radio"]:checked {
        background-color: #3b82f6;
        border-color: #3b82f6;
    }

    .slide-container * {
        transition: all 0.2s ease;
    }
`;

const style = document.createElement('style');
style.textContent = enhancedCSS;
document.head.appendChild(style);