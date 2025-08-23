// Product Management JavaScript Functions

// Category Management Functions
function loadCategories() {
    fetch('/Product/GetCategories')
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(data => {
            if (data.success) {
                renderCategories(data.data);
            } else {
                showNotification(data.message || 'Terjadi kesalahan saat memuat kategori', 'error');
            }
        })
        .catch(error => {
            console.error('Error loading categories:', error);
            showNotification('Terjadi kesalahan saat memuat kategori', 'error');
        });
}

function renderCategories(categories) {
    const categoryList = document.getElementById('categoryList');
    categoryList.innerHTML = '';

    categories.forEach(category => {
        const categoryItem = document.createElement('div');
        categoryItem.className = 'flex items-center justify-between p-4 bg-gradient-to-r from-gray-50 to-gray-100 rounded-xl border border-gray-200 hover:shadow-md transition-all duration-300';
        categoryItem.innerHTML = `
            <div class="flex-1">
                <input type="text" value="${category.categoryName}" 
                       id="category_${category.categoryId}" 
                       class="bg-transparent border-none focus:outline-none focus:bg-white focus:border-gray-300 focus:rounded-lg px-3 py-2 w-full font-medium text-gray-800" 
                       readonly>
            </div>
            <div class="flex items-center space-x-3">
                <button onclick="editCategory(${category.categoryId})" 
                        class="text-blue-600 hover:text-blue-800 p-2 hover:bg-blue-50 rounded-lg transition-all duration-300" 
                        id="editBtn_${category.categoryId}" title="Edit Kategori">
                    <i class="fas fa-edit text-lg"></i>
                </button>
                <button onclick="saveCategory(${category.categoryId})" 
                        class="text-green-600 hover:text-green-800 p-2 hover:bg-green-50 rounded-lg transition-all duration-300 hidden" 
                        id="saveBtn_${category.categoryId}" title="Simpan">
                    <i class="fas fa-check text-lg"></i>
                </button>
                <button onclick="cancelEditCategory(${category.categoryId}, '${category.categoryName.replace(/'/g, "\\\'")}')" 
                        class="text-gray-600 hover:text-gray-800 p-2 hover:bg-gray-50 rounded-lg transition-all duration-300 hidden" 
                        id="cancelBtn_${category.categoryId}" title="Batal">
                    <i class="fas fa-times text-lg"></i>
                </button>
                <button onclick="deleteCategory(${category.categoryId}, '${category.categoryName.replace(/'/g, "\\\'")}\')" 
                        class="text-red-600 hover:text-red-800 p-2 hover:bg-red-50 rounded-lg transition-all duration-300" title="Hapus Kategori">
                    <i class="fas fa-trash text-lg"></i>
                </button>
            </div>
        `;
        categoryList.appendChild(categoryItem);
    });
}

function editCategory(categoryId) {
    const input = document.getElementById(`category_${categoryId}`);
    const editBtn = document.getElementById(`editBtn_${categoryId}`);
    const saveBtn = document.getElementById(`saveBtn_${categoryId}`);
    const cancelBtn = document.getElementById(`cancelBtn_${categoryId}`);

    input.readOnly = false;
    input.focus();
    input.select();
    input.classList.add('bg-white', 'border-2', 'border-blue-300', 'rounded-lg', 'shadow-sm');

    editBtn.classList.add('hidden');
    saveBtn.classList.remove('hidden');
    cancelBtn.classList.remove('hidden');
}

function saveCategory(categoryId) {
    const input = document.getElementById(`category_${categoryId}`);
    const newName = input.value.trim();

    if (!newName) {
        showNotification('Nama kategori tidak boleh kosong', 'warning');
        input.focus();
        return;
    }

    const formData = new FormData();
    formData.append('categoryId', categoryId);
    formData.append('categoryName', newName);
    formData.append('__RequestVerificationToken', getAntiForgeryToken());

    // Show loading state on save button
    const saveBtn = document.getElementById(`saveBtn_${categoryId}`);
    const originalContent = saveBtn.innerHTML;
    saveBtn.innerHTML = '<i class="fas fa-spinner animate-spin"></i>';
    saveBtn.disabled = true;

    fetch('/Product/UpdateCategory', {
        method: 'POST',
        body: formData
    })
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(data => {
            saveBtn.innerHTML = originalContent;
            saveBtn.disabled = false;

            if (data.success) {
                showNotification(data.message, 'success');
                cancelEditCategory(categoryId, newName);
                // Refresh category tabs after a short delay
                setTimeout(() => location.reload(), 1000);
            } else {
                showNotification(data.message || 'Terjadi kesalahan', 'error');
            }
        })
        .catch(error => {
            saveBtn.innerHTML = originalContent;
            saveBtn.disabled = false;
            console.error('Error updating category:', error);
            showNotification('Terjadi kesalahan saat memperbarui kategori', 'error');
        });
}

function cancelEditCategory(categoryId, originalName) {
    const input = document.getElementById(`category_${categoryId}`);
    const editBtn = document.getElementById(`editBtn_${categoryId}`);
    const saveBtn = document.getElementById(`saveBtn_${categoryId}`);
    const cancelBtn = document.getElementById(`cancelBtn_${categoryId}`);

    input.value = originalName;
    input.readOnly = true;
    input.classList.remove('bg-white', 'border-2', 'border-blue-300', 'rounded-lg', 'shadow-sm');

    editBtn.classList.remove('hidden');
    saveBtn.classList.add('hidden');
    cancelBtn.classList.add('hidden');
}

function deleteCategory(categoryId, categoryName) {
    // Create custom confirmation dialog
    const confirmDialog = document.createElement('div');
    confirmDialog.className = 'fixed inset-0 bg-black bg-opacity-50 backdrop-blur-sm flex items-center justify-center z-50';
    confirmDialog.innerHTML = `
        <div class="bg-white rounded-2xl shadow-2xl p-8 max-w-md mx-4">
            <div class="text-center">
                <div class="w-16 h-16 bg-red-100 rounded-full flex items-center justify-center mx-auto mb-4">
                    <i class="fas fa-trash text-2xl text-red-600"></i>
                </div>
                <h3 class="text-xl font-bold text-gray-800 mb-2">Hapus Kategori</h3>
                <p class="text-gray-600 mb-6">Apakah Anda yakin ingin menghapus kategori "<strong>${categoryName}</strong>"? Semua menu dalam kategori ini akan terpengaruh.</p>
                <div class="flex space-x-3">
                    <button onclick="this.closest('.fixed').remove()" class="flex-1 bg-gray-100 hover:bg-gray-200 text-gray-800 py-3 px-4 rounded-xl transition-all duration-300 font-medium">
                        Batal
                    </button>
                    <button onclick="confirmDeleteCategory(${categoryId}); this.closest('.fixed').remove();" class="flex-1 bg-gradient-to-r from-red-500 to-red-600 hover:from-red-600 hover:to-red-700 text-white py-3 px-4 rounded-xl transition-all duration-300 font-medium shadow-lg">
                        Hapus Kategori
                    </button>
                </div>
            </div>
        </div>
    `;

    document.body.appendChild(confirmDialog);
}

function confirmDeleteCategory(categoryId) {
    const formData = new FormData();
    formData.append('categoryId', categoryId);
    formData.append('__RequestVerificationToken', getAntiForgeryToken());

    fetch('/Product/DeleteCategory', {
        method: 'POST',
        body: formData
    })
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(data => {
            if (data.success) {
                showNotification(data.message, 'success');
                loadCategories();
                // Refresh page to update category tabs
                setTimeout(() => location.reload(), 1000);
            } else {
                showNotification(data.message || 'Terjadi kesalahan', 'error');
            }
        })
        .catch(error => {
            console.error('Error deleting category:', error);
            showNotification('Terjadi kesalahan saat menghapus kategori', 'error');
        });
}

// Add Category Form Handler
document.addEventListener('DOMContentLoaded', function () {
    const addCategoryForm = document.getElementById('addCategoryForm');
    if (addCategoryForm) {
        addCategoryForm.addEventListener('submit', function (e) {
            e.preventDefault();

            const categoryNameInput = document.getElementById('newCategoryName');
            const categoryName = categoryNameInput.value.trim();

            if (!categoryName) {
                showNotification('Nama kategori tidak boleh kosong', 'warning');
                categoryNameInput.focus();
                return;
            }

            if (categoryName.length > 50) {
                showNotification('Nama kategori maksimal 50 karakter', 'warning');
                categoryNameInput.focus();
                return;
            }

            const formData = new FormData();
            formData.append('categoryName', categoryName);
            formData.append('__RequestVerificationToken', getAntiForgeryToken());

            // Show loading state
            const submitButton = addCategoryForm.querySelector('button[type="submit"]');
            const originalContent = submitButton.innerHTML;
            submitButton.innerHTML = '<i class="fas fa-spinner animate-spin"></i>';
            submitButton.disabled = true;

            fetch('/Product/CreateCategory', {
                method: 'POST',
                body: formData
            })
                .then(response => {
                    if (!response.ok) {
                        throw new Error('Network response was not ok');
                    }
                    return response.json();
                })
                .then(data => {
                    submitButton.innerHTML = originalContent;
                    submitButton.disabled = false;

                    if (data.success) {
                        showNotification(data.message, 'success');
                        categoryNameInput.value = '';
                        loadCategories();
                        // Refresh page to update category tabs
                        setTimeout(() => location.reload(), 1000);
                    } else {
                        showNotification(data.message || 'Terjadi kesalahan', 'error');
                    }
                })
                .catch(error => {
                    submitButton.innerHTML = originalContent;
                    submitButton.disabled = false;
                    console.error('Error creating category:', error);
                    showNotification('Terjadi kesalahan saat menambahkan kategori', 'error');
                });
        });
    }

    // Menu Form Handler
    const menuForm = document.getElementById('menuForm');
    if (menuForm) {
        menuForm.addEventListener('submit', function (e) {
            e.preventDefault();

            // Validate form
            if (!validateMenuForm()) {
                return;
            }

            const formData = new FormData(menuForm);

            // Ensure IsActive value is properly set
            const isActiveCheckbox = document.getElementById('menuIsActive');
            const hiddenIsActive = document.getElementById('hiddenIsActive');

            // Remove any existing IsActive from formData and add the correct value
            formData.delete('IsActive');
            formData.append('IsActive', isActiveCheckbox.checked);

            formData.append('__RequestVerificationToken', getAntiForgeryToken());

            const menuItemId = document.getElementById('menuItemId').value;
            const isEdit = menuItemId && menuItemId !== '';

            const url = isEdit ? '/Product/UpdateMenuItem' : '/Product/CreateMenuItem';

            // Show loading state
            const submitButton = document.getElementById('menuSubmitBtn');
            const originalContent = submitButton.innerHTML;
            submitButton.innerHTML = '<i class="fas fa-spinner animate-spin mr-2"></i>Memproses...';
            submitButton.disabled = true;

            showLoading();

            fetch(url, {
                method: 'POST',
                body: formData
            })
                .then(response => {
                    if (!response.ok) {
                        throw new Error('Network response was not ok');
                    }
                    return response.json();
                })
                .then(data => {
                    hideLoading();
                    submitButton.innerHTML = originalContent;
                    submitButton.disabled = false;

                    if (data.success) {
                        showNotification(data.message, 'success');
                        hideMenuModal();

                        // Reload current category
                        const activeTab = document.querySelector('.category-tab.bg-gradient-to-r');
                        if (activeTab) {
                            const categoryId = parseInt(activeTab.getAttribute('data-category-id'));
                            setTimeout(() => loadProductCategory(categoryId), 500);
                        } else {
                            // Load first category if no active tab
                            const firstTab = document.querySelector('.category-tab');
                            if (firstTab) {
                                const categoryId = parseInt(firstTab.getAttribute('data-category-id'));
                                setTimeout(() => loadProductCategory(categoryId), 500);
                            }
                        }
                    } else {
                        showNotification(data.message || 'Terjadi kesalahan', 'error');
                    }
                })
                .catch(error => {
                    hideLoading();
                    submitButton.innerHTML = originalContent;
                    submitButton.disabled = false;
                    console.error('Error saving menu item:', error);
                    showNotification('Terjadi kesalahan saat menyimpan menu item', 'error');
                });
        });
    }
});

function validateMenuForm() {
    const categoryId = document.getElementById('menuCategoryId').value;
    const itemName = document.getElementById('menuItemName').value.trim();
    const price = document.getElementById('menuPrice').value;
    const stock = document.getElementById('menuStock').value;

    if (!categoryId) {
        showNotification('Pilih kategori terlebih dahulu', 'warning');
        document.getElementById('menuCategoryId').focus();
        return false;
    }

    if (!itemName) {
        showNotification('Nama item tidak boleh kosong', 'warning');
        document.getElementById('menuItemName').focus();
        return false;
    }

    if (itemName.length > 100) {
        showNotification('Nama item maksimal 100 karakter', 'warning');
        document.getElementById('menuItemName').focus();
        return false;
    }

    if (!price || parseFloat(price) <= 0) {
        showNotification('Harga harus lebih besar dari 0', 'warning');
        document.getElementById('menuPrice').focus();
        return false;
    }

    if (!stock || parseInt(stock) < 0) {
        showNotification('Stok tidak boleh negatif', 'warning');
        document.getElementById('menuStock').focus();
        return false;
    }

    // Validate image file if selected
    const imageFile = document.getElementById('menuImage').files[0];
    if (imageFile) {
        if (!imageFile.type.startsWith('image/')) {
            showNotification('File yang dipilih bukan gambar', 'warning');
            document.getElementById('menuImage').focus();
            return false;
        }

        if (imageFile.size > 5 * 1024 * 1024) {
            showNotification('Ukuran file tidak boleh lebih dari 5MB', 'warning');
            document.getElementById('menuImage').focus();
            return false;
        }
    }

    return true;
}

// Keyboard shortcuts
document.addEventListener('keydown', function (e) {
    // Only process shortcuts if not typing in input fields
    if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA' || e.target.tagName === 'SELECT') {
        // Allow Escape key even in input fields to close modals
        if (e.key === 'Escape') {
            e.preventDefault();
            hideMenuModal();
            hideCategoryModal();
        }
        return;
    }

    switch (e.key) {
        case 'Escape':
            e.preventDefault();
            hideMenuModal();
            hideCategoryModal();
            break;
        case 'n':
        case 'N':
            if (e.ctrlKey) {
                e.preventDefault();
                showAddMenuModal();
            }
            break;
        case 'c':
        case 'C':
            if (e.ctrlKey) {
                e.preventDefault();
                showCategoryModal();
            }
            break;
    }
});

// Modal click outside to close
document.addEventListener('click', function (e) {
    const categoryModal = document.getElementById('categoryModal');
    const menuModal = document.getElementById('menuModal');

    if (e.target === categoryModal) {
        hideCategoryModal();
    }

    if (e.target === menuModal) {
        hideMenuModal();
    }
});

// Utility Functions
function getAntiForgeryToken() {
    const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
    return tokenElement ? tokenElement.value : '';
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

function showNotification(message, type = 'info', duration = 4000) {
    // Remove existing notifications
    const existingNotifications = document.querySelectorAll('.notification');
    existingNotifications.forEach(notification => notification.remove());

    const notification = document.createElement('div');
    notification.className = `notification fixed top-4 right-4 px-6 py-4 rounded-xl shadow-2xl z-50 transform transition-all duration-300 translate-x-full max-w-sm`;

    let bgColor, textColor, icon, borderColor;
    switch (type) {
        case 'success':
            bgColor = 'bg-green-500';
            textColor = 'text-white';
            icon = 'fas fa-check-circle';
            borderColor = 'border-green-600';
            break;
        case 'error':
            bgColor = 'bg-red-500';
            textColor = 'text-white';
            icon = 'fas fa-exclamation-circle';
            borderColor = 'border-red-600';
            break;
        case 'warning':
            bgColor = 'bg-yellow-500';
            textColor = 'text-white';
            icon = 'fas fa-exclamation-triangle';
            borderColor = 'border-yellow-600';
            break;
        default:
            bgColor = 'bg-blue-500';
            textColor = 'text-white';
            icon = 'fas fa-info-circle';
            borderColor = 'border-blue-600';
    }

    notification.className += ` ${bgColor} ${textColor} border-l-4 ${borderColor}`;
    notification.innerHTML = `
        <div class="flex items-start space-x-3">
            <i class="${icon} text-lg mt-0.5"></i>
            <div class="flex-1">
                <p class="font-medium">${message}</p>
            </div>
            <button onclick="this.parentElement.parentElement.remove()" class="text-white hover:text-gray-200 transition-colors">
                <i class="fas fa-times"></i>
            </button>
        </div>
    `;

    document.body.appendChild(notification);

    setTimeout(() => {
        notification.classList.remove('translate-x-full');
    }, 10);

    setTimeout(() => {
        notification.classList.add('translate-x-full');
        setTimeout(() => {
            if (notification.parentNode) {
                notification.remove();
            }
        }, 300);
    }, duration);
}