
function getCurrentLocalDateTime() {
    const now = new Date();
    now.setMinutes(now.getMinutes() - now.getTimezoneOffset());
    return now.toISOString().slice(0, 16);
}

function getOneWeekFromNow() {
    const now = new Date();
    now.setDate(now.getDate() + 7);
    now.setMinutes(now.getMinutes() - now.getTimezoneOffset());
    return now.toISOString().slice(0, 16);
}


function loadCategories() {
    fetch('/Product/GetCategories')
        .then(response => {
            if (!response.ok) throw new Error('Network response was not ok');
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
        categoryItem.className = 'flex items-center justify-between p-3 bg-gray-50 rounded-lg border hover:shadow-sm transition-all duration-300';
        categoryItem.innerHTML = `
            <div class="flex-1">
                <input type="text" value="${category.categoryName}" 
                       id="category_${category.categoryId}" 
                       class="bg-transparent border-none focus:outline-none focus:bg-white focus:border-gray-300 focus:rounded px-2 py-1 w-full font-medium text-gray-800" 
                       readonly>
            </div>
            <div class="flex items-center space-x-2">
                <button onclick="editCategory(${category.categoryId})" 
                        class="text-blue-600 hover:text-blue-800 p-1 hover:bg-blue-50 rounded transition-all duration-300" 
                        id="editBtn_${category.categoryId}" title="Edit">
                    <i class="fas fa-edit"></i>
                </button>
                <button onclick="saveCategory(${category.categoryId})" 
                        class="text-green-600 hover:text-green-800 p-1 hover:bg-green-50 rounded transition-all duration-300 hidden" 
                        id="saveBtn_${category.categoryId}" title="Simpan">
                    <i class="fas fa-check"></i>
                </button>
                <button onclick="cancelEditCategory(${category.categoryId}, '${category.categoryName.replace(/'/g, "\\\'")}')" 
                        class="text-gray-600 hover:text-gray-800 p-1 hover:bg-gray-50 rounded transition-all duration-300 hidden" 
                        id="cancelBtn_${category.categoryId}" title="Batal">
                    <i class="fas fa-times"></i>
                </button>
                <button onclick="deleteCategory(${category.categoryId}, '${category.categoryName.replace(/'/g, "\\\'")}')" 
                        class="text-red-600 hover:text-red-800 p-1 hover:bg-red-50 rounded transition-all duration-300" title="Hapus">
                    <i class="fas fa-trash"></i>
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
    input.classList.add('bg-white', 'border', 'border-blue-300', 'rounded', 'shadow-sm');

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

    const saveBtn = document.getElementById(`saveBtn_${categoryId}`);
    const originalContent = saveBtn.innerHTML;
    saveBtn.innerHTML = '<i class="fas fa-spinner animate-spin"></i>';
    saveBtn.disabled = true;

    fetch('/Product/UpdateCategory', {
        method: 'POST',
        body: formData
    })
        .then(response => {
            if (!response.ok) throw new Error('Network response was not ok');
            return response.json();
        })
        .then(data => {
            saveBtn.innerHTML = originalContent;
            saveBtn.disabled = false;

            if (data.success) {
                showNotification(data.message, 'success');
                cancelEditCategory(categoryId, newName);
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
    input.classList.remove('bg-white', 'border', 'border-blue-300', 'rounded', 'shadow-sm');

    editBtn.classList.remove('hidden');
    saveBtn.classList.add('hidden');
    cancelBtn.classList.add('hidden');
}

function deleteCategory(categoryId, categoryName) {
    const confirmDialog = document.createElement('div');
    confirmDialog.className = 'fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 backdrop-blur-sm';
    confirmDialog.innerHTML = `
        <div class="bg-white rounded-2xl shadow-2xl p-8 max-w-md mx-4">
            <div class="text-center">
                <div class="w-16 h-16 bg-gradient-to-br from-red-100 to-red-200 rounded-full flex items-center justify-center mx-auto mb-4">
                    <i class="fas fa-trash text-3xl text-red-600"></i>
                </div>
                <h3 class="text-xl font-bold text-gray-800 mb-2">Hapus Kategori</h3>
                <p class="text-gray-600 mb-6">Apakah Anda yakin ingin menghapus kategori "<strong>${categoryName}</strong>"?</p>
                <div class="flex space-x-3">
                    <button onclick="this.closest('.fixed').remove()" class="flex-1 bg-gray-100 hover:bg-gray-200 text-gray-800 py-3 px-4 rounded-lg transition-colors font-semibold">
                        Batal
                    </button>
                    <button onclick="confirmDeleteCategory(${categoryId}); this.closest('.fixed').remove();" class="flex-1 bg-gradient-to-r from-red-600 to-red-700 hover:from-red-700 hover:to-red-800 text-white py-3 px-4 rounded-lg transition-colors font-semibold">
                        Hapus
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
            if (!response.ok) throw new Error('Network response was not ok');
            return response.json();
        })
        .then(data => {
            if (data.success) {
                showNotification(data.message, 'success');
                loadCategories();
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


function loadProductCategory(categoryId) {
    showLoading();

    fetch(`/Product/GetMenuByCategory?categoryId=${categoryId}`)
        .then(response => {
            if (!response.ok) throw new Error('Network response was not ok');
            return response.text();
        })
        .then(html => {
            document.getElementById('existingMenuItems').innerHTML = html;
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
            tab.classList.remove('text-gray-600', 'hover:bg-gray-100');
            tab.classList.add('bg-blue-600', 'text-white', 'shadow-md');
        } else {
            tab.classList.remove('bg-blue-600', 'text-white', 'shadow-md');
            tab.classList.add('text-gray-600', 'hover:bg-gray-100');
        }
    });
}


function showCategoryModal() {
    document.getElementById('categoryModal').classList.remove('hidden');
    document.getElementById('categoryModal').classList.add('flex');
    loadCategories();
}

function hideCategoryModal() {
    document.getElementById('categoryModal').classList.add('hidden');
    document.getElementById('categoryModal').classList.remove('flex');
}

function showAddMenuModal() {
    document.getElementById('menuModalTitle').textContent = 'Tambah Menu Baru';
    document.getElementById('menuSubmitBtn').innerHTML = '<i class="fas fa-save mr-2"></i>Simpan Menu';
    document.getElementById('menuForm').reset();
    document.getElementById('menuItemId').value = '';

    setActiveStatus(true);
    setDiscountStatus(false);

    document.getElementById('currentImagePreview').classList.add('hidden');
    document.getElementById('imagePreview').classList.add('hidden');

    document.getElementById('menuModal').classList.remove('hidden');
    document.getElementById('menuModal').classList.add('flex');

    switchTab('basic');
    updatePricePreview();
}

function showEditMenuModal(menuItemId) {
    document.getElementById('menuModalTitle').textContent = 'Edit Menu';
    document.getElementById('menuSubmitBtn').innerHTML = '<i class="fas fa-save mr-2"></i>Update Menu';

    showLoading();

    fetch(`/Product/GetMenuItem?menuItemId=${menuItemId}`)
        .then(response => {
            if (!response.ok) throw new Error('Network response was not ok');
            return response.json();
        })
        .then(data => {
            hideLoading();
            if (data.success) {
                const menu = data.data;
                document.getElementById('menuItemId').value = menu.menuItemId;
                document.getElementById('menuCategoryId').value = menu.categoryId;
                document.getElementById('menuItemName').value = menu.itemName;
                document.getElementById('menuDescription').value = menu.description || '';
                document.getElementById('menuPrice').value = menu.price;
                document.getElementById('menuStock').value = menu.stock;

                document.getElementById('menuDiscountPercentage').value = menu.discountPercentage || 0;
                document.getElementById('menuDiscountStartDate').value = menu.discountStartDate || '';
                document.getElementById('menuDiscountEndDate').value = menu.discountEndDate || '';

                setDiscountStatus(menu.isDiscountActive);
                setActiveStatus(menu.isActive);

                if (menu.imagePath) {
                    document.getElementById('currentImage').src = menu.imagePath;
                    document.getElementById('currentImagePreview').classList.remove('hidden');
                } else {
                    document.getElementById('currentImagePreview').classList.add('hidden');
                }

                document.getElementById('imagePreview').classList.add('hidden');
                document.getElementById('menuModal').classList.remove('hidden');
                document.getElementById('menuModal').classList.add('flex');

                switchTab('basic');
                updatePricePreview();
            } else {
                showNotification(data.message || 'Terjadi kesalahan saat memuat data menu', 'error');
            }
        })
        .catch(error => {
            hideLoading();
            console.error('Error loading menu:', error);
            showNotification('Terjadi kesalahan saat memuat data menu', 'error');
        });
}

function hideMenuModal() {
    document.getElementById('menuModal').classList.add('hidden');
    document.getElementById('menuModal').classList.remove('flex');
}


function toggleDiscountStatus() {
    const checkbox = document.getElementById('menuDiscountActive');
    const currentState = checkbox.checked;
    setDiscountStatus(!currentState);
    updatePricePreview();
}

function setDiscountStatus(isActive) {
    const checkbox = document.getElementById('menuDiscountActive');
    const hiddenInput = document.getElementById('hiddenDiscountActive');
    const toggleSwitch = document.querySelector('.discount-toggle-switch');
    const toggleDot = document.querySelector('.discount-toggle-dot');
    const fieldsContainer = document.getElementById('discountFieldsContainer');

    if (!checkbox || !toggleSwitch || !toggleDot || !fieldsContainer) return;

    checkbox.checked = isActive;
    hiddenInput.value = isActive ? 'true' : 'false';

    if (isActive) {
        toggleSwitch.classList.remove('bg-gray-300');
        toggleSwitch.classList.add('bg-green-500');
        toggleDot.classList.remove('translate-x-0.5');
        toggleDot.classList.add('translate-x-7');
        fieldsContainer.classList.remove('hidden');

        const startDate = document.getElementById('menuDiscountStartDate');
        const endDate = document.getElementById('menuDiscountEndDate');

        if (!startDate.value) {
            startDate.value = getCurrentLocalDateTime();
        }
        if (!endDate.value) {
            endDate.value = getOneWeekFromNow();
        }
    } else {
        toggleSwitch.classList.remove('bg-green-500');
        toggleSwitch.classList.add('bg-gray-300');
        toggleDot.classList.remove('translate-x-7');
        toggleDot.classList.add('translate-x-0.5');
        fieldsContainer.classList.add('hidden');

        const discountPercentage = document.getElementById('menuDiscountPercentage');
        if (discountPercentage) discountPercentage.value = 0;
    }

    updatePricePreview();
}

function updatePricePreview() {
    const priceInput = document.getElementById('menuPrice');
    const discountPercentageInput = document.getElementById('menuDiscountPercentage');
    const discountActive = document.getElementById('menuDiscountActive');
    const originalPricePreview = document.getElementById('originalPricePreview');
    const discountedPricePreview = document.getElementById('discountedPricePreview');
    const savingsPreview = document.getElementById('savingsPreview');

    if (!priceInput || !discountPercentageInput || !originalPricePreview || !discountedPricePreview || !savingsPreview) return;

    const originalPrice = parseFloat(priceInput.value) || 0;
    const discountPercentage = parseFloat(discountPercentageInput.value) || 0;
    const isDiscountActive = discountActive && discountActive.checked;

    if (originalPrice > 0 && isDiscountActive && discountPercentage > 0) {
        const discountAmount = originalPrice * (discountPercentage / 100);
        const finalPrice = originalPrice - discountAmount;

        originalPricePreview.textContent = `Rp ${originalPrice.toLocaleString('id-ID')}`;
        discountedPricePreview.textContent = `Rp ${finalPrice.toLocaleString('id-ID')}`;
        savingsPreview.textContent = `-${discountPercentage}% (Hemat Rp ${discountAmount.toLocaleString('id-ID')})`;
        savingsPreview.classList.remove('hidden');
        originalPricePreview.parentElement.classList.remove('hidden');
    } else {
        originalPricePreview.textContent = 'Rp 0';
        discountedPricePreview.textContent = originalPrice > 0 ? `Rp ${originalPrice.toLocaleString('id-ID')}` : 'Rp 0';
        savingsPreview.classList.add('hidden');

        if (!isDiscountActive || discountPercentage === 0) {
            originalPricePreview.parentElement.classList.add('hidden');
        }
    }
}


function toggleActiveStatus() {
    const checkbox = document.getElementById('menuIsActive');
    const currentState = checkbox.checked;
    setActiveStatus(!currentState);
}

function setActiveStatus(isActive) {
    const checkbox = document.getElementById('menuIsActive');
    const hiddenInput = document.getElementById('hiddenIsActive');
    const toggleSwitch = document.querySelector('.toggle-switch');
    const toggleDot = document.querySelector('.toggle-dot');

    if (!checkbox || !toggleSwitch || !toggleDot) return;

    checkbox.checked = isActive;
    hiddenInput.value = isActive ? 'true' : 'false';

    if (isActive) {
        toggleSwitch.classList.remove('bg-gray-300');
        toggleSwitch.classList.add('bg-green-500');
        toggleDot.classList.remove('translate-x-0.5');
        toggleDot.classList.add('translate-x-5');
    } else {
        toggleSwitch.classList.remove('bg-green-500');
        toggleSwitch.classList.add('bg-gray-300');
        toggleDot.classList.remove('translate-x-5');
        toggleDot.classList.add('translate-x-0.5');
    }
}


function setupToggleSwitch() {
    setActiveStatus(true);
}

function setupDiscountFeatures() {
    setDiscountStatus(false);

    const priceInput = document.getElementById('menuPrice');
    const discountPercentageInput = document.getElementById('menuDiscountPercentage');
    const discountStartDate = document.getElementById('menuDiscountStartDate');
    const discountEndDate = document.getElementById('menuDiscountEndDate');

    if (priceInput) {
        priceInput.addEventListener('input', updatePricePreview);
    }

    if (discountPercentageInput) {
        discountPercentageInput.addEventListener('input', function () {
            let value = parseFloat(this.value);
            if (value < 0) this.value = 0;
            if (value > 100) this.value = 100;
            updatePricePreview();
        });
    }

    if (discountStartDate && discountEndDate) {
        discountStartDate.addEventListener('change', validateDiscountDates);
        discountEndDate.addEventListener('change', validateDiscountDates);
    }
}

function validateDiscountDates() {
    const startDate = document.getElementById('menuDiscountStartDate');
    const endDate = document.getElementById('menuDiscountEndDate');

    if (!startDate || !endDate || !startDate.value || !endDate.value) return;

    const start = new Date(startDate.value);
    const end = new Date(endDate.value);

    if (end <= start) {
        showNotification('Tanggal berakhir harus setelah tanggal mulai', 'warning');
        const nextDay = new Date(start);
        nextDay.setDate(nextDay.getDate() + 1);
        endDate.value = nextDay.toISOString().slice(0, 16);
    }
}


function setupImagePreview() {
    const imageInput = document.getElementById('menuImage');
    const preview = document.getElementById('imagePreview');
    const previewImage = document.getElementById('previewImage');

    if (!imageInput || !preview || !previewImage) return;

    imageInput.addEventListener('change', function (e) {
        const file = e.target.files[0];
        if (file) {
            if (!file.type.startsWith('image/')) {
                showNotification('File yang dipilih bukan gambar', 'warning');
                imageInput.value = '';
                return;
            }

            if (file.size > 5 * 1024 * 1024) {
                showNotification('Ukuran file tidak boleh lebih dari 5MB', 'warning');
                imageInput.value = '';
                return;
            }

            const reader = new FileReader();
            reader.onload = function (e) {
                previewImage.src = e.target.result;
                preview.classList.remove('hidden');
            };
            reader.readAsDataURL(file);
        } else {
            preview.classList.add('hidden');
        }
    });
}

function clearImagePreview() {
    document.getElementById('menuImage').value = '';
    document.getElementById('imagePreview').classList.add('hidden');
}


function validateMenuForm() {
    const categoryId = document.getElementById('menuCategoryId').value;
    const itemName = document.getElementById('menuItemName').value.trim();
    const price = document.getElementById('menuPrice').value;
    const stock = document.getElementById('menuStock').value;
    const discountActive = document.getElementById('menuDiscountActive').checked;
    const discountPercentage = document.getElementById('menuDiscountPercentage').value;
    const discountStartDate = document.getElementById('menuDiscountStartDate').value;
    const discountEndDate = document.getElementById('menuDiscountEndDate').value;

    if (!categoryId) {
        showNotification('Pilih kategori terlebih dahulu', 'warning');
        switchTab('basic');
        document.getElementById('menuCategoryId').focus();
        return false;
    }

    if (!itemName) {
        showNotification('Nama item tidak boleh kosong', 'warning');
        switchTab('basic');
        document.getElementById('menuItemName').focus();
        return false;
    }

    if (itemName.length > 100) {
        showNotification('Nama item maksimal 100 karakter', 'warning');
        switchTab('basic');
        document.getElementById('menuItemName').focus();
        return false;
    }

    if (!price || parseFloat(price) <= 0) {
        showNotification('Harga harus lebih besar dari 0', 'warning');
        switchTab('basic');
        document.getElementById('menuPrice').focus();
        return false;
    }

    if (!stock || parseInt(stock) < 0) {
        showNotification('Stok tidak boleh negatif', 'warning');
        switchTab('basic');
        document.getElementById('menuStock').focus();
        return false;
    }

    if (discountActive) {
        if (!discountPercentage || parseFloat(discountPercentage) <= 0) {
            showNotification('Persentase diskon harus lebih dari 0 jika diskon aktif', 'warning');
            switchTab('discount');
            document.getElementById('menuDiscountPercentage').focus();
            return false;
        }

        if (parseFloat(discountPercentage) > 100) {
            showNotification('Persentase diskon tidak boleh lebih dari 100%', 'warning');
            switchTab('discount');
            document.getElementById('menuDiscountPercentage').focus();
            return false;
        }

        if (discountStartDate && discountEndDate) {
            const start = new Date(discountStartDate);
            const end = new Date(discountEndDate);

            if (end <= start) {
                showNotification('Tanggal berakhir diskon harus setelah tanggal mulai', 'warning');
                switchTab('discount');
                document.getElementById('menuDiscountEndDate').focus();
                return false;
            }
        }
    }

    const imageFile = document.getElementById('menuImage').files[0];
    if (imageFile) {
        if (!imageFile.type.startsWith('image/')) {
            showNotification('File yang dipilih bukan gambar', 'warning');
            switchTab('basic');
            document.getElementById('menuImage').focus();
            return false;
        }

        if (imageFile.size > 5 * 1024 * 1024) {
            showNotification('Ukuran file tidak boleh lebih dari 5MB', 'warning');
            switchTab('basic');
            document.getElementById('menuImage').focus();
            return false;
        }
    }

    return true;
}


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

            const submitButton = addCategoryForm.querySelector('button[type="submit"]');
            const originalContent = submitButton.innerHTML;
            submitButton.innerHTML = '<i class="fas fa-spinner animate-spin"></i>';
            submitButton.disabled = true;

            fetch('/Product/CreateCategory', {
                method: 'POST',
                body: formData
            })
                .then(response => {
                    if (!response.ok) throw new Error('Network response was not ok');
                    return response.json();
                })
                .then(data => {
                    submitButton.innerHTML = originalContent;
                    submitButton.disabled = false;

                    if (data.success) {
                        showNotification(data.message, 'success');
                        categoryNameInput.value = '';
                        loadCategories();
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


    const menuForm = document.getElementById('menuForm');
    if (menuForm) {
        menuForm.addEventListener('submit', function (e) {
            e.preventDefault();

            if (!validateMenuForm()) {
                return;
            }

            const formData = new FormData(menuForm);

            const isActiveCheckbox = document.getElementById('menuIsActive');
            formData.delete('IsActive');
            formData.append('IsActive', isActiveCheckbox.checked);

            const isDiscountActiveCheckbox = document.getElementById('menuDiscountActive');
            formData.delete('IsDiscountActive');
            formData.append('IsDiscountActive', isDiscountActiveCheckbox.checked);

            formData.append('__RequestVerificationToken', getAntiForgeryToken());

            const menuItemId = document.getElementById('menuItemId').value;
            const isEdit = menuItemId && menuItemId !== '';

            const url = isEdit ? '/Product/UpdateMenuItem' : '/Product/CreateMenuItem';

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
                    if (!response.ok) throw new Error('Network response was not ok');
                    return response.json();
                })
                .then(data => {
                    hideLoading();
                    submitButton.innerHTML = originalContent;
                    submitButton.disabled = false;

                    if (data.success) {
                        showNotification(data.message, 'success');
                        hideMenuModal();

                        const activeTab = document.querySelector('.category-tab.bg-blue-600');
                        if (activeTab) {
                            const categoryId = parseInt(activeTab.getAttribute('data-category-id'));
                            setTimeout(() => loadProductCategory(categoryId), 500);
                        } else {
                            setTimeout(() => location.reload(), 500);
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


document.addEventListener('DOMContentLoaded', function () {
    initializeProductManagement();
    loadCategories();
    setupImagePreview();
    setupToggleSwitch();
    setupDiscountFeatures();
});

function initializeProductManagement() {
    console.log('Product Management initialized with enhanced features');
}


document.addEventListener('keydown', function (e) {
    if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA' || e.target.tagName === 'SELECT') {
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


function switchTab(tab) {
    const basicTab = document.getElementById('basicTab');
    const discountTab = document.getElementById('discountTab');
    const basicContent = document.getElementById('basicTabContent');
    const discountContent = document.getElementById('discountTabContent');

    if (tab === 'basic') {
        basicTab.className = 'px-6 py-3 font-medium text-blue-600 border-b-2 border-blue-600 transition-all';
        discountTab.className = 'px-6 py-3 font-medium text-gray-500 hover:text-blue-600 transition-all';
        basicContent.classList.remove('hidden');
        discountContent.classList.add('hidden');
    } else {
        basicTab.className = 'px-6 py-3 font-medium text-gray-500 hover:text-blue-600 transition-all';
        discountTab.className = 'px-6 py-3 font-medium text-blue-600 border-b-2 border-blue-600 transition-all';
        basicContent.classList.add('hidden');
        discountContent.classList.remove('hidden');
    }
}


function filterByStatus(status) {
    const menuItems = document.querySelectorAll('.menu-item-card');
    menuItems.forEach(item => {
        const isActive = item.getAttribute('data-is-active') === 'true';
        const hasDiscount = item.getAttribute('data-has-discount') === 'true';

        let show = false;
        switch (status) {
            case 'all':
                show = true;
                break;
            case 'active':
                show = isActive;
                break;
            case 'inactive':
                show = !isActive;
                break;
            case 'discount':
                show = hasDiscount;
                break;
        }

        item.style.display = show ? 'block' : 'none';
    });
}

window.loadCategories = loadCategories;
window.editCategory = editCategory;
window.saveCategory = saveCategory;
window.cancelEditCategory = cancelEditCategory;
window.deleteCategory = deleteCategory;
window.confirmDeleteCategory = confirmDeleteCategory;
window.loadProductCategory = loadProductCategory;
window.showCategoryModal = showCategoryModal;
window.hideCategoryModal = hideCategoryModal;
window.showAddMenuModal = showAddMenuModal;
window.showEditMenuModal = showEditMenuModal;
window.hideMenuModal = hideMenuModal;
window.toggleDiscountStatus = toggleDiscountStatus;
window.toggleActiveStatus = toggleActiveStatus;
window.clearImagePreview = clearImagePreview;
window.switchTab = switchTab;
window.filterByStatus = filterByStatus;