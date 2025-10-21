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

function initializeSettings() {
    console.log('Menginisialisasi halaman pengaturan...');

    setDefaultDates();

    const urlParams = new URLSearchParams(window.location.search);
    const section = urlParams.get('section');

    if (section === 'cashier-dashboard') {
        setTimeout(() => {
            filterCashierDashboard(false, false);
        }, 500);
    } else if (section === 'stock-history') {
            filterStockHistory(false, false);
    } else if (section === 'user-activity') {
        setTimeout(() => {
            filterUserActivity(false, false);
        }, 500);
    }
}

function setDefaultDates() {
    const today = new Date();
    const oneWeekAgo = new Date(today.getTime() - 7 * 24 * 60 * 60 * 1000);


    setDateValue('stockStartDate', today);
    setDateValue('stockEndDate', today);

    setDateValue('activityStartDate', oneWeekAgo);
    setDateValue('activityEndDate', today);

    setDateValue('cashierSelectedDate', today);

    console.log('Tanggal default berhasil diatur');
}

function setDateValue(elementId, date) {
    const element = document.getElementById(elementId);
    if (element) {
        element.value = formatDateForInput(date);
        console.log(`${elementId} diatur ke: ${element.value}`);
    }
}

function formatDateForInput(date) {
    return date.toISOString().split('T')[0];
}

function formatDateIndonesian(dateString) {
    if (!dateString) return '';

    const date = new Date(dateString);
    const options = {
        year: 'numeric',
        month: 'long',
        day: 'numeric',
        timeZone: 'Asia/Jakarta'
    };

    return date.toLocaleDateString('id-ID', options);
}

function isAccountSection() {
    const urlParams = new URLSearchParams(window.location.search);
    const section = urlParams.get('section');
    return section === 'account' || !section;
}

function loadCurrentUserData(showNotificationOnSuccess = true) {
    console.log('Memuat data pengguna saat ini...');

    fetch('/Settings/GetCurrentUser')
        .then(response => {
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            return response.json();
        })
        .then(data => {
            if (data.success) {
                updateCurrentUserUI(data.data);
            }
        })
        .catch(error => {
            console.error('Error loading user data:', error);
            showNotification('Gagal memuat data pengguna: ' + error.message, 'error');
            updateCurrentUserUIError();
        });
}

function updateCurrentUserUI(user) {
    const elements = [
        { id: 'currentUserEmail', content: user.email || 'Tidak ada email' },
        { id: 'currentUserLastLogin', content: formatLoginTime(user.lastLogin) },
        { id: 'currentUserCreatedAt', content: formatCreatedTime(user.createdAt) }
    ];

    elements.forEach(({ id, content }) => {
        const element = document.getElementById(id);
        if (element) {
            element.innerHTML = `<span class="font-semibold text-gray-800 bg-white px-4 py-2 rounded-lg shadow-sm border">${content}</span>`;
        }
    });
}

function formatLoginTime(lastLogin) {
    if (!lastLogin) return 'Belum pernah login';

    const date = new Date(lastLogin);
    return date.toLocaleDateString('id-ID', {
        year: 'numeric',
        month: 'long',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
        timeZone: 'Asia/Jakarta'
    }) + ' WIB';
}

function formatCreatedTime(createdAt) {
    if (!createdAt) return 'Data tidak tersedia';

    const date = new Date(createdAt);
    return date.toLocaleDateString('id-ID', {
        year: 'numeric',
        month: 'long',
        day: 'numeric',
        timeZone: 'Asia/Jakarta'
    });
}

function updateCurrentUserUIError() {
    const errorElements = ['currentUserEmail', 'currentUserLastLogin', 'currentUserCreatedAt'];
    errorElements.forEach(id => {
        const element = document.getElementById(id);
        if (element) {
            element.innerHTML = '<span class="text-red-600 bg-red-50 px-3 py-2 rounded-lg border border-red-200">Gagal memuat data</span>';
        }
    });
}

function filterStockHistory(showNotificationOnSuccess = true, showLoadingIndicator = true) {
    const startDate = document.getElementById('stockStartDate')?.value;
    const endDate = document.getElementById('stockEndDate')?.value;

    if (!validateDateRange(startDate, endDate, 'Filter Riwayat Stok')) {
        return;
    }

    if (showLoadingIndicator) {
        showLoading();
    }
    console.log(`Filtering stock history: ${startDate} to ${endDate}`);

    const params = new URLSearchParams();
    if (startDate) params.append('startDate', startDate);
    if (endDate) params.append('endDate', endDate);

    fetch(`/Settings/GetStockHistory?${params.toString()}`)
        .then(response => {
            if (!response.ok) {
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }
            return response.text();
        })
        .then(html => {
            if (showLoadingIndicator) {
                hideLoading();
            }
            const container = document.getElementById('stockHistoryContainer');
            if (container) {
                container.innerHTML = html;
            }
        })
        .catch(error => {
            if (showLoadingIndicator) {
                hideLoading();
            }
            console.error('Error loading stock history:', error);
            showNotification('Gagal memuat riwayat stok: ' + error.message, 'error');
        });
}

function filterUserActivity(showNotificationOnSuccess = true, showLoadingIndicator = true) {
    const startDate = document.getElementById('activityStartDate')?.value;
    const endDate = document.getElementById('activityEndDate')?.value;

    if (!validateDateRange(startDate, endDate, 'Filter Aktivitas Pengguna')) {
        return;
    }

    if (showLoadingIndicator) {
        showLoading();
    }
    console.log(`Filtering user activity: ${startDate} to ${endDate}`);

    const params = new URLSearchParams();
    if (startDate) params.append('startDate', startDate);
    if (endDate) params.append('endDate', endDate);

    fetch(`/Settings/GetUserActivity?${params.toString()}`)
        .then(response => {
            if (!response.ok) {
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }
            return response.text();
        })
        .then(html => {
            if (showLoadingIndicator) {
                hideLoading();
            }
            const container = document.getElementById('userActivityContainer');
            if (container) {
                container.innerHTML = html;
            }
        })
        .catch(error => {
            if (showLoadingIndicator) {
                hideLoading();
            }
            console.error('Error loading user activity:', error);
            showNotification('Gagal memuat aktivitas pengguna: ' + error.message, 'error');
        });
}

function filterCashierDashboard(showNotificationOnSuccess = true, showLoadingIndicator = true) {
    console.log('=== filterCashierDashboard dipanggil ===');

    const selectedDate = document.getElementById('cashierSelectedDate')?.value;
    console.log('Tanggal yang dipilih:', selectedDate);

    if (!selectedDate || selectedDate.trim() === '') {
        console.log('Validasi gagal: Tanggal kosong');
        showNotification('Silakan pilih tanggal', 'warning');
        return;
    }

    const selected = new Date(selectedDate);
    if (isNaN(selected.getTime())) {
        console.log('Validasi gagal: Format tanggal tidak valid');
        showNotification('Format tanggal tidak valid', 'error');
        return;
    }

    const today = new Date();
    today.setHours(23, 59, 59, 999);

    if (selected > today) {
        console.log('Peringatan: Tanggal masa depan dipilih');
        showNotification('Peringatan: Tanggal yang dipilih di masa depan', 'warning');
    }

    if (showLoadingIndicator) {
        showLoading();
    }
    console.log(`Memfilter dashboard kasir untuk: ${selectedDate}`);

    const params = new URLSearchParams();
    params.append('selectedDate', selectedDate);

    fetch(`/Settings/GetCashierDashboard?${params.toString()}`)
        .then(response => {
            if (!response.ok) {
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }
            return response.text();
        })
        .then(html => {
            if (showLoadingIndicator) {
                hideLoading();
            }
            const container = document.getElementById('cashierDashboardContainer');
            if (container) {
                container.innerHTML = html;
            }
        })
        .catch(error => {
            if (showLoadingIndicator) {
                hideLoading();
            }
            console.error('Error loading cashier dashboard:', error);
            showNotification('Gagal memuat dashboard kasir: ' + error.message, 'error');
        });
}

function resetCashierDashboard() {
    console.log('Reset dashboard kasir ke hari ini');
    const today = new Date();
    setDateValue('cashierSelectedDate', today);

    setTimeout(() => {
        filterCashierDashboard();
    }, 300);
}

function closeShift() {
    console.log('=== closeShift function called ===');

    if (!confirm('Apakah Anda yakin ingin menutup shift?\n\nWaktu kerja akan dihitung dari login pertama hingga sekarang.')) {
        console.log('User cancelled Tutup shift');
        return;
    }

    const formData = new FormData();
    formData.append('__RequestVerificationToken', getAntiForgeryToken());

    console.log('Sending tutup shift request...');
    showLoading();

    fetch('/Settings/CloseShift', {
        method: 'POST',
        body: formData
    })
        .then(response => {
            console.log('tutup shift response received:', response.status);
            if (!response.ok) {
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }
            return response.json();
        })
        .then(data => {
            hideLoading();
            console.log('Tutup shift data:', data);

            if (data.success) {
                showNotification(data.message || 'Shift berhasil ditutup', 'success');

                console.log('Reloading cashier dashboard...');
                setTimeout(() => {
                    filterCashierDashboard(false, true);
                }, 1000);
            } else {
                showNotification(data.message || 'Gagal menutup shift', 'error');
            }
        })
        .catch(error => {
            hideLoading();
            console.error('Error during tutup shift:', error);
            showNotification('Terjadi kesalahan saat menutup shift: ' + error.message, 'error');
        });
}

function validateDateRange(startDate, endDate, context = 'Filter') {
    if (!startDate || !endDate) {
        showNotification('Silakan pilih tanggal mulai dan tanggal akhir', 'warning');
        return false;
    }

    const start = new Date(startDate);
    const end = new Date(endDate);
    const today = new Date();

    if (start > end) {
        showNotification('Tanggal mulai tidak boleh lebih besar dari tanggal akhir', 'error');
        return false;
    }

    const diffTime = Math.abs(end - start);
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));

    if (diffDays > 365) {
        showNotification('Rentang tanggal terlalu besar. Maksimal 1 tahun.', 'warning');
        return false;
    }

    if (end > today) {
        showNotification('Peringatan: Tanggal akhir di masa depan dipilih', 'warning');
    }

    return true;
}

function resetAllFilters() {
    setDefaultDates();

    const urlParams = new URLSearchParams(window.location.search);
    const section = urlParams.get('section');

    switch (section) {
        case 'stock-history':
            setTimeout(() => filterStockHistory(), 300);
            break;
        case 'user-activity':
            setTimeout(() => filterUserActivity(), 300);
            break;
        case 'cashier-dashboard':
            setTimeout(() => filterCashierDashboard(), 300);
            break;
        default:
            showNotification('Filter berhasil direset ke default', 'success');
    }
}

function showEditCurrentUserModal() {
    console.log('Membuka modal edit pengguna...');
    showLoading();

    fetch('/Settings/GetCurrentUser')
        .then(response => response.json())
        .then(data => {
            hideLoading();
            if (data.success) {
                populateEditUserForm(data.data);
                showModal('editCurrentUserModal');
            } else {
                showNotification(data.message || 'Gagal memuat data pengguna', 'error');
            }
        })
        .catch(error => {
            hideLoading();
            console.error('Error:', error);
            showNotification('Terjadi kesalahan saat memuat data pengguna', 'error');
        });
}

function populateEditUserForm(user) {
    const fields = {
        'editCurrentUserFullName': user.fullName || '',
        'editCurrentUserUsername': user.username || '',
        'editCurrentUserEmail': user.email || ''
    };

    Object.entries(fields).forEach(([id, value]) => {
        const element = document.getElementById(id);
        if (element) element.value = value;
    });

    ['editCurrentUserCurrentPassword', 'editCurrentUserNewPassword', 'editCurrentUserConfirmPassword'].forEach(id => {
        const element = document.getElementById(id);
        if (element) element.value = '';
    });
}

function hideEditCurrentUserModal() {
    hideModal('editCurrentUserModal');
}

function validateCurrentUserForm() {
    const fullName = document.getElementById('editCurrentUserFullName')?.value.trim();
    const email = document.getElementById('editCurrentUserEmail')?.value.trim();
    const currentPassword = document.getElementById('editCurrentUserCurrentPassword')?.value;
    const newPassword = document.getElementById('editCurrentUserNewPassword')?.value;
    const confirmPassword = document.getElementById('editCurrentUserConfirmPassword')?.value;

    if (!fullName) {
        showNotification('Nama lengkap tidak boleh kosong', 'warning');
        return false;
    }

    if (!email) {
        showNotification('Email tidak boleh kosong', 'warning');
        return false;
    }

    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(email)) {
        showNotification('Format email tidak valid', 'warning');
        return false;
    }

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

function showCreateUserModal() {
    const form = document.getElementById('createUserForm');
    if (form) form.reset();

    const roleSelect = document.getElementById('newUserRole');
    if (roleSelect) roleSelect.value = 'Cashier';

    showModal('createUserModal');
}

function hideCreateUserModal() {
    hideModal('createUserModal');
}

function validateCreateUserForm() {
    const fields = {
        fullName: document.getElementById('newUserFullName')?.value.trim(),
        username: document.getElementById('newUserUsername')?.value.trim(),
        email: document.getElementById('newUserEmail')?.value.trim(),
        password: document.getElementById('newUserPassword')?.value,
        confirmPassword: document.getElementById('newUserConfirmPassword')?.value
    };

    if (!fields.fullName) {
        showNotification('Nama lengkap tidak boleh kosong', 'warning');
        return false;
    }

    if (!fields.username) {
        showNotification('Username tidak boleh kosong', 'warning');
        return false;
    }

    if (!fields.email) {
        showNotification('Email tidak boleh kosong', 'warning');
        return false;
    }

    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(fields.email)) {
        showNotification('Format email tidak valid', 'warning');
        return false;
    }

    if (!fields.password) {
        showNotification('Password tidak boleh kosong', 'warning');
        return false;
    }

    if (fields.password.length < 6) {
        showNotification('Password minimal 6 karakter', 'warning');
        return false;
    }

    if (fields.password !== fields.confirmPassword) {
        showNotification('Password dan konfirmasi password tidak cocok', 'warning');
        return false;
    }

    return true;
}

function logout() {
    if (!confirm('Apakah Anda yakin ingin keluar dari sistem?')) {
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
                showNotification('Berhasil keluar dari sistem', 'success');
                setTimeout(() => {
                    window.location.href = data.redirectUrl;
                }, 1000);
            } else {
                window.location.href = '/Account/Login';
            }
        })
        .catch(error => {
            hideLoading();
            console.error('Error during logout:', error);
            window.location.href = '/Account/Login';
        });
}

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

function showNotification(message, type = 'info', duration = 4000) {
    const existingNotifications = document.querySelectorAll('.notification');
    existingNotifications.forEach(notification => notification.remove());

    const notification = document.createElement('div');
    notification.className = 'notification fixed top-4 right-4 px-6 py-4 rounded-xl shadow-2xl z-50 transform transition-all duration-300 translate-x-full max-w-md';

    const configs = {
        success: {
            bgColor: 'bg-gradient-to-r from-green-500 to-green-600',
            textColor: 'text-white',
            icon: 'fas fa-check-circle',
            borderColor: 'border-l-4 border-green-700'
        },
        error: {
            bgColor: 'bg-gradient-to-r from-red-500 to-red-600',
            textColor: 'text-white',
            icon: 'fas fa-exclamation-circle',
            borderColor: 'border-l-4 border-red-700'
        },
        warning: {
            bgColor: 'bg-gradient-to-r from-yellow-500 to-yellow-600',
            textColor: 'text-white',
            icon: 'fas fa-exclamation-triangle',
            borderColor: 'border-l-4 border-yellow-700'
        },
        info: {
            bgColor: 'bg-gradient-to-r from-blue-500 to-blue-600',
            textColor: 'text-white',
            icon: 'fas fa-info-circle',
            borderColor: 'border-l-4 border-blue-700'
        }
    };

    const config = configs[type] || configs.info;
    notification.className += ` ${config.bgColor} ${config.textColor} ${config.borderColor}`;

    notification.innerHTML = `
        <div class="flex items-center space-x-3">
            <div class="flex-shrink-0">
                <i class="${config.icon} text-2xl"></i>
            </div>
            <div class="flex-1">
                <p class="font-semibold text-sm">${message}</p>
            </div>
            <button onclick="this.parentElement.parentElement.remove()" class="flex-shrink-0 ml-4 text-white hover:text-gray-200 transition-colors">
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
            notification.remove();
        }, 300);
    }, duration);
}

function getAntiForgeryToken() {
    const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
    return tokenElement ? tokenElement.value : '';
}

document.addEventListener('DOMContentLoaded', function () {
    console.log('=== DOM Content Loaded - Settings.js ===');

    initializeSettings();

    const createUserForm = document.getElementById('createUserForm');
    if (createUserForm) {
        createUserForm.addEventListener('submit', function (e) {
            e.preventDefault();

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

            showLoading();

            fetch('/Settings/CreateUser', {
                method: 'POST',
                body: formData
            })
                .then(response => response.json())
                .then(data => {
                    hideLoading();
                    if (data.success) {
                        hideCreateUserModal();
                        createUserForm.reset();
                        document.getElementById('newUserRole').value = 'Cashier';
                    } else {
                        showNotification(data.message || 'Gagal membuat pengguna', 'error');
                    }
                })
                .catch(error => {
                    hideLoading();
                    console.error('Error:', error);
                    showNotification('Terjadi kesalahan saat membuat pengguna', 'error');
                });
        });
    }

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
                        hideEditCurrentUserModal();
                        const fullNameElement = document.getElementById('currentUserFullName');
                        if (fullNameElement) {
                            fullNameElement.textContent = document.getElementById('editCurrentUserFullName').value;
                        }
                        const emailElement = document.getElementById('currentUserEmail');
                        if (emailElement) {
                            emailElement.textContent = document.getElementById('editCurrentUserEmail').value;
                        }
                    } else {
                        showNotification(data.message || 'Gagal mengupdate akun', 'error');
                    }
                })
                .catch(error => {
                    hideLoading();
                    console.error('Error:', error);
                    showNotification('Terjadi kesalahan saat mengupdate akun', 'error');
                });
        });
    }

    document.addEventListener('click', function (e) {
        if (e.target.id === 'createUserModal') {
            hideCreateUserModal();
        }
        if (e.target.id === 'editCurrentUserModal') {
            hideEditCurrentUserModal();
        }
    });

    document.addEventListener('keydown', function (e) {
        if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA' || e.target.tagName === 'SELECT') {
            return;
        }

        if (e.key === 'Escape') {
            e.preventDefault();
            hideCreateUserModal();
            hideEditCurrentUserModal();
        }
    });

    const stockStartDate = document.getElementById('stockStartDate');
    const stockEndDate = document.getElementById('stockEndDate');
    if (stockStartDate) {
        stockStartDate.addEventListener('keypress', function (e) {
            if (e.key === 'Enter') filterStockHistory(true, true);
        });
    }
    if (stockEndDate) {
        stockEndDate.addEventListener('keypress', function (e) {
            if (e.key === 'Enter') filterStockHistory(true, true);
        });
    }

    const activityStartDate = document.getElementById('activityStartDate');
    const activityEndDate = document.getElementById('activityEndDate');
    if (activityStartDate) {
        activityStartDate.addEventListener('keypress', function (e) {
            if (e.key === 'Enter') filterUserActivity(true, true);
        });
    }
    if (activityEndDate) {
        activityEndDate.addEventListener('keypress', function (e) {
            if (e.key === 'Enter') filterUserActivity(true, true);
        });
    }

    const cashierSelectedDate = document.getElementById('cashierSelectedDate');
    if (cashierSelectedDate) {
        cashierSelectedDate.addEventListener('keypress', function (e) {
            if (e.key === 'Enter') {
                filterCashierDashboard(true, true);
            }
        });
    }

    console.log('=== Settings.js fully initialized ===');
});