// TAMBAH: Function untuk toggle password visibility
function togglePasswordVisibility(inputId, buttonElement) {
    const input = document.getElementById(inputId);
    const icon = buttonElement.querySelector('i');

    if (input.type === 'password') {
        input.type = 'text';
        icon.className = 'fas fa-eye-slash';
    } else {
        input.type = 'password';
        icon.className = 'fas fa-eye';
    }
}

// Utility Functions// Settings JavaScript Functions - Updated for AuthService

// Initialize settings page
function initializeSettings() {
    console.log('Settings page initialized');

    // Set default dates for filters
    const today = new Date();
    const oneWeekAgo = new Date(today.getTime() - 7 * 24 * 60 * 60 * 1000);

    const stockStartDate = document.getElementById('stockStartDate');
    const stockEndDate = document.getElementById('stockEndDate');
    const activityStartDate = document.getElementById('activityStartDate');
    const activityEndDate = document.getElementById('activityEndDate');
    const cashierStartDate = document.getElementById('cashierStartDate');
    const cashierEndDate = document.getElementById('cashierEndDate');

    if (stockStartDate) stockStartDate.value = formatDate(oneWeekAgo);
    if (stockEndDate) stockEndDate.value = formatDate(today);
    if (activityStartDate) activityStartDate.value = formatDate(oneWeekAgo);
    if (activityEndDate) activityEndDate.value = formatDate(today);
    if (cashierStartDate) cashierStartDate.value = formatDate(today);
    if (cashierEndDate) cashierEndDate.value = formatDate(today);

    // Load current user data if on account page
    if (window.location.search.includes('section=account') || window.location.search === '' || !window.location.search.includes('section=')) {
        setTimeout(() => {
            loadCurrentUserData();
        }, 500);
    }

    // Auto load cashier dashboard jika di halaman tersebut
    if (window.location.search.includes('cashier-dashboard')) {
        setTimeout(() => {
            filterCashierDashboard();
        }, 500);
    }
}

// Utility function to format date
function formatDate(date) {
    return date.toISOString().split('T')[0];
}

// Load current user data - MENGGUNAKAN AuthService
function loadCurrentUserData() {
    console.log('Loading current user data...');

    fetch('/Settings/GetCurrentUser')
        .then(response => {
            console.log('Response status:', response.status);
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            return response.json();
        })
        .then(data => {
            console.log('Response data:', data);

            if (data.success) {
                const user = data.data;
                console.log('User data:', user);

                // Update UI elements
                const emailElement = document.getElementById('currentUserEmail');
                const lastLoginElement = document.getElementById('currentUserLastLogin');
                const createdAtElement = document.getElementById('currentUserCreatedAt');

                if (emailElement) {
                    emailElement.innerHTML = `<span class="font-bold text-gray-800 bg-white px-3 py-1 rounded-lg shadow-sm">${user.email || 'Tidak ada email'}</span>`;
                    console.log('Email updated to:', user.email);
                }

                if (lastLoginElement) {
                    const lastLoginText = user.lastLogin ?
                        new Date(user.lastLogin).toLocaleDateString('id-ID', {
                            year: 'numeric',
                            month: 'long',
                            day: 'numeric',
                            hour: '2-digit',
                            minute: '2-digit'
                        }) : 'Belum pernah';
                    lastLoginElement.innerHTML = `<span class="font-bold text-gray-800 bg-white px-3 py-1 rounded-lg shadow-sm">${lastLoginText}</span>`;
                    console.log('Last login updated to:', lastLoginText);
                }

                // TAMBAH: Update Created At
                if (createdAtElement) {
                    const createdAtText = user.createdAt ?
                        new Date(user.createdAt).toLocaleDateString('id-ID', {
                            year: 'numeric',
                            month: 'long',
                            day: 'numeric'
                        }) : 'Data tidak tersedia';
                    createdAtElement.innerHTML = `<span class="font-bold text-gray-800 bg-white px-3 py-1 rounded-lg shadow-sm">${createdAtText}</span>`;
                    console.log('Created at updated to:', createdAtText);
                }
            } else {
                console.error('Error from server:', data.message);
                showNotification('Error loading user data: ' + data.message, 'error');
            }
        })
        .catch(error => {
            console.error('Fetch error:', error);
            showNotification('Network error loading user data', 'error');

            // Update UI dengan error state
            const emailElement = document.getElementById('currentUserEmail');
            const lastLoginElement = document.getElementById('currentUserLastLogin');
            const createdAtElement = document.getElementById('currentUserCreatedAt');

            if (emailElement) {
                emailElement.innerHTML = '<span class="text-red-600 bg-red-50 px-3 py-1 rounded-lg">Error memuat</span>';
            }
            if (lastLoginElement) {
                lastLoginElement.innerHTML = '<span class="text-red-600 bg-red-50 px-3 py-1 rounded-lg">Error memuat</span>';
            }
            if (createdAtElement) {
                createdAtElement.innerHTML = '<span class="text-red-600 bg-red-50 px-3 py-1 rounded-lg">Error memuat</span>';
            }
        });
}

// Show edit current user modal - MENGGUNAKAN AuthService
function showEditCurrentUserModal() {
    console.log('Opening edit current user modal...');
    showLoading();

    fetch('/Settings/GetCurrentUser')
        .then(response => {
            console.log('Edit modal - Response status:', response.status);
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            return response.json();
        })
        .then(data => {
            hideLoading();
            console.log('Edit modal - Response data:', data);

            if (data.success) {
                const user = data.data;
                console.log('Edit modal - User data:', user);

                // Update form fields
                const fullNameField = document.getElementById('editCurrentUserFullName');
                const usernameField = document.getElementById('editCurrentUserUsername');
                const emailField = document.getElementById('editCurrentUserEmail');

                if (fullNameField) {
                    fullNameField.value = user.fullName || '';
                    console.log('Full name field updated');
                }

                if (usernameField) {
                    usernameField.value = user.username || '';
                    console.log('Username field updated');
                }

                if (emailField) {
                    emailField.value = user.email || '';
                    console.log('Email field updated');
                }

                // Clear password fields
                document.getElementById('editCurrentUserCurrentPassword').value = '';
                document.getElementById('editCurrentUserNewPassword').value = '';
                document.getElementById('editCurrentUserConfirmPassword').value = '';

                showModal('editCurrentUserModal');
            } else {
                showNotification(data.message || 'Terjadi kesalahan', 'error');
            }
        })
        .catch(error => {
            hideLoading();
            console.error('Error loading current user:', error);
            showNotification('Terjadi kesalahan saat memuat data pengguna', 'error');
        });
}

// Hide edit current user modal
function hideEditCurrentUserModal() {
    hideModal('editCurrentUserModal');
}

// Validate current user form
function validateCurrentUserForm() {
    const fullName = document.getElementById('editCurrentUserFullName').value.trim();
    const email = document.getElementById('editCurrentUserEmail').value.trim();
    const currentPassword = document.getElementById('editCurrentUserCurrentPassword').value;
    const newPassword = document.getElementById('editCurrentUserNewPassword').value;
    const confirmPassword = document.getElementById('editCurrentUserConfirmPassword').value;

    if (!fullName) {
        showNotification('Nama lengkap tidak boleh kosong', 'warning');
        return false;
    }

    if (!email) {
        showNotification('Email tidak boleh kosong', 'warning');
        return false;
    }

    // Email validation
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(email)) {
        showNotification('Format email tidak valid', 'warning');
        return false;
    }

    // Password validation (only if changing password)
    if (newPassword || confirmPassword || currentPassword) {
        if (!currentPassword) {
            showNotification('Password lama harus diisi untuk mengubah password', 'warning');
            return false;
        }

        if (!newPassword) {
            showNotification('Password baru harus diisi', 'warning');
            return false;
        }

        if (newPassword.length < 6) {
            showNotification('Password baru minimal 6 karakter', 'warning');
            return false;
        }

        if (newPassword !== confirmPassword) {
            showNotification('Password baru dan konfirmasi password tidak cocok', 'warning');
            return false;
        }
    }

    return true;
}

// Cashier Dashboard Functions
function filterCashierDashboard() {
    const startDate = document.getElementById('cashierStartDate').value;
    const endDate = document.getElementById('cashierEndDate').value;

    if (startDate && endDate && startDate > endDate) {
        showNotification('Tanggal mulai tidak boleh lebih besar dari tanggal akhir', 'warning');
        return;
    }

    showLoading();

    const params = new URLSearchParams();
    if (startDate) params.append('startDate', startDate);
    if (endDate) params.append('endDate', endDate);

    fetch(`/Settings/GetCashierDashboard?${params.toString()}`)
        .then(response => response.text())
        .then(html => {
            hideLoading();
            document.getElementById('cashierDashboardContainer').innerHTML = html;
        })
        .catch(error => {
            hideLoading();
            console.error('Error loading cashier dashboard:', error);
            showNotification('Terjadi kesalahan saat memuat dashboard kasir', 'error');
        });
}

// Create User Functions
function showCreateUserModal() {
    document.getElementById('createUserForm').reset();
    document.getElementById('newUserRole').value = 'Cashier'; // Default to Cashier
    showModal('createUserModal');
}

function hideCreateUserModal() {
    hideModal('createUserModal');
}

function validateCreateUserForm() {
    const fullName = document.getElementById('newUserFullName').value.trim();
    const username = document.getElementById('newUserUsername').value.trim();
    const email = document.getElementById('newUserEmail').value.trim();
    const password = document.getElementById('newUserPassword').value;
    const confirmPassword = document.getElementById('newUserConfirmPassword').value;

    if (!fullName) {
        showNotification('Nama lengkap tidak boleh kosong', 'warning');
        return false;
    }

    if (!username) {
        showNotification('Username tidak boleh kosong', 'warning');
        return false;
    }

    if (!email) {
        showNotification('Email tidak boleh kosong', 'warning');
        return false;
    }

    // Email validation
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(email)) {
        showNotification('Format email tidak valid', 'warning');
        return false;
    }

    if (!password) {
        showNotification('Password tidak boleh kosong', 'warning');
        return false;
    }

    if (password.length < 6) {
        showNotification('Password minimal 6 karakter', 'warning');
        return false;
    }

    if (password !== confirmPassword) {
        showNotification('Password dan konfirmasi password tidak cocok', 'warning');
        return false;
    }

    return true;
}

// Stock History Functions
function filterStockHistory() {
    const startDate = document.getElementById('stockStartDate').value;
    const endDate = document.getElementById('stockEndDate').value;

    if (startDate && endDate && startDate > endDate) {
        showNotification('Tanggal mulai tidak boleh lebih besar dari tanggal akhir', 'warning');
        return;
    }

    showLoading();

    const params = new URLSearchParams();
    if (startDate) params.append('startDate', startDate);
    if (endDate) params.append('endDate', endDate);

    fetch(`/Settings/GetStockHistory?${params.toString()}`)
        .then(response => response.text())
        .then(html => {
            hideLoading();
            document.getElementById('stockHistoryContainer').innerHTML = html;
        })
        .catch(error => {
            hideLoading();
            console.error('Error loading stock history:', error);
            showNotification('Terjadi kesalahan saat memuat riwayat stok', 'error');
        });
}

// User Activity Functions
function filterUserActivity() {
    const startDate = document.getElementById('activityStartDate').value;
    const endDate = document.getElementById('activityEndDate').value;

    if (startDate && endDate && startDate > endDate) {
        showNotification('Tanggal mulai tidak boleh lebih besar dari tanggal akhir', 'warning');
        return;
    }

    showLoading();

    const params = new URLSearchParams();
    if (startDate) params.append('startDate', startDate);
    if (endDate) params.append('endDate', endDate);

    fetch(`/Settings/GetUserActivity?${params.toString()}`)
        .then(response => response.text())
        .then(html => {
            hideLoading();
            document.getElementById('userActivityContainer').innerHTML = html;
        })
        .catch(error => {
            hideLoading();
            console.error('Error loading user activity:', error);
            showNotification('Terjadi kesalahan saat memuat aktivitas pengguna', 'error');
        });
}

// Logout function - FIXED
function logout() {
    if (!confirm('Apakah Anda yakin ingin logout?')) {
        return;
    }

    const formData = new FormData();
    formData.append('__RequestVerificationToken', getAntiForgeryToken());

    showLoading();

    fetch('/Settings/Logout', {
        method: 'POST',
        body: formData
    })
        .then(response => response.json())
        .then(data => {
            hideLoading();
            if (data.success && data.redirectUrl) {
                showNotification('Logout berhasil', 'success');
                setTimeout(() => {
                    window.location.href = data.redirectUrl;
                }, 1000);
            } else {
                // Fallback redirect
                window.location.href = '/Account/Login';
            }
        })
        .catch(error => {
            hideLoading();
            console.error('Error during logout:', error);
            // Even if there's an error, redirect to login
            window.location.href = '/Account/Login';
        });
}

// Utility Functions
function showModal(modalId) {
    const modal = document.getElementById(modalId);
    if (modal) {
        modal.classList.remove('hidden');
        modal.classList.add('flex');
        document.body.style.overflow = 'hidden';
    }
}

function hideModal(modalId) {
    const modal = document.getElementById(modalId);
    if (modal) {
        modal.classList.add('hidden');
        modal.classList.remove('flex');
        document.body.style.overflow = 'auto';
    }
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

function getAntiForgeryToken() {
    const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
    return tokenElement ? tokenElement.value : '';
}

// Event Listeners
document.addEventListener('DOMContentLoaded', function () {
    // Initialize settings
    initializeSettings();

    // Create User form submit handler
    const createUserForm = document.getElementById('createUserForm');
    if (createUserForm) {
        createUserForm.addEventListener('submit', function (e) {
            e.preventDefault();
            console.log('Create user form submitted');

            if (!validateCreateUserForm()) {
                return;
            }

            const formData = new FormData();
            formData.append('fullName', document.getElementById('newUserFullName').value);
            formData.append('username', document.getElementById('newUserUsername').value);
            formData.append('email', document.getElementById('newUserEmail').value);
            formData.append('password', document.getElementById('newUserPassword').value);
            formData.append('role', document.getElementById('newUserRole').value);
            formData.append('__RequestVerificationToken', getAntiForgeryToken());

            // Debug: Log form data
            console.log('Form data being sent:');
            for (let [key, value] of formData.entries()) {
                if (key !== 'password' && key !== '__RequestVerificationToken') {
                    console.log(`${key}: ${value}`);
                }
            }

            console.log('Sending create user request...');
            showLoading();

            fetch('/Settings/CreateUser', {
                method: 'POST',
                body: formData
            })
                .then(response => {
                    console.log('Create user response status:', response.status);
                    console.log('Create user response headers:', response.headers);

                    // Check if response is JSON
                    const contentType = response.headers.get('content-type');
                    if (contentType && contentType.includes('application/json')) {
                        return response.json();
                    } else {
                        // If not JSON, get text to see what's returned
                        return response.text().then(text => {
                            console.error('Non-JSON response:', text);
                            throw new Error('Server returned non-JSON response: ' + text.substring(0, 200));
                        });
                    }
                })
                .then(data => {
                    hideLoading();
                    console.log('Create user response data:', data);

                    if (data.success) {
                        showNotification(data.message || 'Pengguna berhasil dibuat', 'success');
                        hideCreateUserModal();
                        // Reset form
                        createUserForm.reset();
                        document.getElementById('newUserRole').value = 'Cashier';
                    } else {
                        console.error('Server error:', data);
                        showNotification(data.message || 'Terjadi kesalahan pada server', 'error');
                    }
                })
                .catch(error => {
                    hideLoading();
                    console.error('Error creating user:', error);
                    console.error('Error stack:', error.stack);
                    showNotification('Terjadi kesalahan saat membuat pengguna: ' + error.message, 'error');
                });
        });
    } else {
        console.error('Create user form not found!');
    }

    // Edit Current User form submit handler
    const editCurrentUserForm = document.getElementById('editCurrentUserForm');
    if (editCurrentUserForm) {
        editCurrentUserForm.addEventListener('submit', function (e) {
            e.preventDefault();

            if (!validateCurrentUserForm()) {
                return;
            }

            const formData = new FormData();
            formData.append('fullName', document.getElementById('editCurrentUserFullName').value);
            formData.append('email', document.getElementById('editCurrentUserEmail').value);
            formData.append('currentPassword', document.getElementById('editCurrentUserCurrentPassword').value);
            formData.append('newPassword', document.getElementById('editCurrentUserNewPassword').value);
            formData.append('__RequestVerificationToken', getAntiForgeryToken());

            showLoading();

            fetch('/Settings/UpdateCurrentUser', {
                method: 'POST',
                body: formData
            })
                .then(response => response.json())
                .then(data => {
                    hideLoading();
                    if (data.success) {
                        showNotification(data.message, 'success');
                        hideEditCurrentUserModal();
                        // Reload current user data to reflect changes
                        setTimeout(() => {
                            loadCurrentUserData();
                            // Update the profile card display
                            document.getElementById('currentUserFullName').textContent =
                                document.getElementById('editCurrentUserFullName').value;
                        }, 500);
                    } else {
                        showNotification(data.message || 'Terjadi kesalahan', 'error');
                    }
                })
                .catch(error => {
                    hideLoading();
                    console.error('Error updating current user:', error);
                    showNotification('Terjadi kesalahan saat mengupdate akun', 'error');
                });
        });
    }

    // Modal click outside to close
    document.addEventListener('click', function (e) {
        const createUserModal = document.getElementById('createUserModal');
        const editCurrentUserModal = document.getElementById('editCurrentUserModal');

        if (e.target === createUserModal) {
            hideCreateUserModal();
        }
        if (e.target === editCurrentUserModal) {
            hideEditCurrentUserModal();
        }
    });

    // Keyboard shortcuts
    document.addEventListener('keydown', function (e) {
        // Only process shortcuts if not typing in input fields
        if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA' || e.target.tagName === 'SELECT') {
            return;
        }

        switch (e.key) {
            case 'Escape':
                e.preventDefault();
                hideCreateUserModal();
                hideEditCurrentUserModal();
                break;
            case 'n':
            case 'N':
                if (e.ctrlKey) {
                    e.preventDefault();
                    showCreateUserModal();
                }
                break;
        }
    });

    // Auto-filter on Enter key press for date inputs
    const dateInputs = ['stockStartDate', 'stockEndDate', 'activityStartDate', 'activityEndDate', 'cashierStartDate', 'cashierEndDate'];
    dateInputs.forEach(inputId => {
        const input = document.getElementById(inputId);
        if (input) {
            input.addEventListener('keypress', function (e) {
                if (e.key === 'Enter') {
                    if (inputId.startsWith('stock')) {
                        filterStockHistory();
                    } else if (inputId.startsWith('activity')) {
                        filterUserActivity();
                    } else if (inputId.startsWith('cashier')) {
                        filterCashierDashboard();
                    }
                }
            });
        }
    });
});