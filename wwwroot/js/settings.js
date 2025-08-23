// Settings JavaScript Functions

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

    if (stockStartDate) stockStartDate.value = formatDate(oneWeekAgo);
    if (stockEndDate) stockEndDate.value = formatDate(today);
    if (activityStartDate) activityStartDate.value = formatDate(oneWeekAgo);
    if (activityEndDate) activityEndDate.value = formatDate(today);
}

// Utility function to format date
function formatDate(date) {
    return date.toISOString().split('T')[0];
}

// User Management Functions
function showCreateUserModal() {
    document.getElementById('userModalTitle').textContent = 'Tambah Pengguna';
    document.getElementById('userSubmitBtn').textContent = 'Tambah Pengguna';
    document.getElementById('userForm').reset();
    document.getElementById('userId').value = '';
    document.getElementById('userIsActive').checked = true;

    // Show password fields for new user
    document.getElementById('passwordLabel').textContent = 'Password';
    document.getElementById('confirmPasswordLabel').textContent = 'Konfirmasi Password';
    document.getElementById('userPassword').required = true;
    document.getElementById('userConfirmPassword').required = true;
    document.getElementById('userPassword').name = 'Password';
    document.getElementById('userConfirmPassword').name = 'ConfirmPassword';

    showModal('userModal');
}

function showEditUserModal(userId) {
    document.getElementById('userModalTitle').textContent = 'Edit Pengguna';
    document.getElementById('userSubmitBtn').textContent = 'Update Pengguna';

    // Change password fields for edit
    document.getElementById('passwordLabel').textContent = 'Password Baru (Kosongkan jika tidak ingin mengubah)';
    document.getElementById('confirmPasswordLabel').textContent = 'Konfirmasi Password Baru';
    document.getElementById('userPassword').required = false;
    document.getElementById('userConfirmPassword').required = false;
    document.getElementById('userPassword').name = 'NewPassword';
    document.getElementById('userConfirmPassword').name = 'ConfirmNewPassword';

    // Load user data
    showLoading();

    fetch(`/Settings/GetUser?userId=${userId}`)
        .then(response => response.json())
        .then(data => {
            hideLoading();
            if (data.success) {
                const user = data.data;
                document.getElementById('userId').value = user.id;
                document.getElementById('userFullName').value = user.fullName;
                document.getElementById('userUsername').value = user.username;
                document.getElementById('userEmail').value = user.email;
                document.getElementById('userRole').value = user.role;
                document.getElementById('userIsActive').checked = user.isActive;

                showModal('userModal');
            } else {
                showNotification(data.message || 'Terjadi kesalahan', 'error');
            }
        })
        .catch(error => {
            hideLoading();
            console.error('Error loading user:', error);
            showNotification('Terjadi kesalahan saat memuat data pengguna', 'error');
        });
}

function hideUserModal() {
    hideModal('userModal');

    // Reset password field names
    document.getElementById('userPassword').name = 'Password';
    document.getElementById('userConfirmPassword').name = 'ConfirmPassword';
}

function deleteUser(userId, userName) {
    if (!confirm(`Apakah Anda yakin ingin menghapus pengguna "${userName}"?`)) {
        return;
    }

    const formData = new FormData();
    formData.append('userId', userId);
    formData.append('__RequestVerificationToken', getAntiForgeryToken());

    showLoading();

    fetch('/Settings/DeleteUser', {
        method: 'POST',
        body: formData
    })
        .then(response => response.json())
        .then(data => {
            hideLoading();
            if (data.success) {
                showNotification(data.message, 'success');
                loadUsers();
            } else {
                showNotification(data.message || 'Terjadi kesalahan', 'error');
            }
        })
        .catch(error => {
            hideLoading();
            console.error('Error deleting user:', error);
            showNotification('Terjadi kesalahan saat menghapus pengguna', 'error');
        });
}

function toggleUserStatus(userId, userName) {
    if (!confirm(`Apakah Anda yakin ingin mengubah status pengguna "${userName}"?`)) {
        return;
    }

    const formData = new FormData();
    formData.append('userId', userId);
    formData.append('__RequestVerificationToken', getAntiForgeryToken());

    showLoading();

    fetch('/Settings/ToggleUserStatus', {
        method: 'POST',
        body: formData
    })
        .then(response => response.json())
        .then(data => {
            hideLoading();
            if (data.success) {
                showNotification(data.message, 'success');
                loadUsers();
            } else {
                showNotification(data.message || 'Terjadi kesalahan', 'error');
            }
        })
        .catch(error => {
            hideLoading();
            console.error('Error toggling user status:', error);
            showNotification('Terjadi kesalahan saat mengubah status pengguna', 'error');
        });
}

function loadUsers() {
    fetch('/Settings/GetUsers')
        .then(response => response.text())
        .then(html => {
            document.getElementById('userManagementContainer').innerHTML = html;
        })
        .catch(error => {
            console.error('Error loading users:', error);
            showNotification('Terjadi kesalahan saat memuat data pengguna', 'error');
        });
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

// Logout function
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

// Form validation
function validateUserForm() {
    const fullName = document.getElementById('userFullName').value.trim();
    const username = document.getElementById('userUsername').value.trim();
    const email = document.getElementById('userEmail').value.trim();
    const password = document.getElementById('userPassword').value;
    const confirmPassword = document.getElementById('userConfirmPassword').value;
    const isEdit = document.getElementById('userId').value !== '';

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

    // Password validation for new users
    if (!isEdit) {
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
    } else {
        // Password validation for edit (only if password is provided)
        if (password && password.length < 6) {
            showNotification('Password minimal 6 karakter', 'warning');
            return false;
        }

        if (password !== confirmPassword) {
            showNotification('Password dan konfirmasi password tidak cocok', 'warning');
            return false;
        }
    }

    return true;
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
    // User form submit handler
    const userForm = document.getElementById('userForm');
    if (userForm) {
        userForm.addEventListener('submit', function (e) {
            e.preventDefault();

            if (!validateUserForm()) {
                return;
            }

            const formData = new FormData(e.target);
            formData.append('__RequestVerificationToken', getAntiForgeryToken());

            const userId = document.getElementById('userId').value;
            const isEdit = userId && userId !== '';

            const url = isEdit ? '/Settings/UpdateUser' : '/Settings/CreateUser';

            showLoading();

            fetch(url, {
                method: 'POST',
                body: formData
            })
                .then(response => response.json())
                .then(data => {
                    hideLoading();
                    if (data.success) {
                        showNotification(data.message, 'success');
                        hideUserModal();
                        loadUsers();
                    } else {
                        showNotification(data.message || 'Terjadi kesalahan', 'error');
                    }
                })
                .catch(error => {
                    hideLoading();
                    console.error('Error saving user:', error);
                    showNotification('Terjadi kesalahan saat menyimpan pengguna', 'error');
                });
        });
    }

    // Modal click outside to close
    document.addEventListener('click', function (e) {
        const userModal = document.getElementById('userModal');
        if (e.target === userModal) {
            hideUserModal();
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
                hideUserModal();
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
    const dateInputs = ['stockStartDate', 'stockEndDate', 'activityStartDate', 'activityEndDate'];
    dateInputs.forEach(inputId => {
        const input = document.getElementById(inputId);
        if (input) {
            input.addEventListener('keypress', function (e) {
                if (e.key === 'Enter') {
                    if (inputId.startsWith('stock')) {
                        filterStockHistory();
                    } else if (inputId.startsWith('activity')) {
                        filterUserActivity();
                    }
                }
            });
        }
    });
});