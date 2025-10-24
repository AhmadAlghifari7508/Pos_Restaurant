/**
 * Show receipt preview in new window/tab
 * @param {number} orderId
 */
function showReceiptPreview(orderId) {
    if (!orderId || orderId <= 0) {
        showNotification('Order ID tidak valid', 'error');
        return;
    }

    const previewUrl = `/Receipt/Preview?orderId=${orderId}`;
    window.open(previewUrl, '_blank', 'width=800,height=900,scrollbars=yes');
}

/**
 * Print receipt directly (opens print dialog immediately)
 * @param {number} orderId 
 */
function printReceiptDirect(orderId) {
    if (!orderId || orderId <= 0) {
        showNotification('Order ID tidak valid', 'error');
        return;
    }

    const printUrl = `/Receipt/Print?orderId=${orderId}`;
    const printWindow = window.open(printUrl, '_blank', 'width=400,height=600');

    if (!printWindow) {
        showNotification('Pop-up diblokir! Silakan izinkan pop-up untuk mencetak.', 'warning');
        return;
    }

    // Auto print when loaded
    printWindow.onload = function () {
        printWindow.print();
    };
}

/**
 * Get receipt data via AJAX
 * @param {number} orderId 
 * @returns {Promise}
 */
async function getReceiptData(orderId) {
    try {
        const response = await fetch(`/Receipt/GetReceiptData?orderId=${orderId}`);
        const data = await response.json();

        if (data.success) {
            return data.data;
        } else {
            throw new Error(data.message || 'Gagal mengambil data struk');
        }
    } catch (error) {
        console.error('Error getting receipt data:', error);
        showNotification(error.message, 'error');
        return null;
    }
}

/**
 * Print receipt with preview option
 * @param {number} orderId
 * @param {boolean} showPreview
 */
function printReceipt(orderId, showPreview = true) {
    if (!orderId || orderId <= 0) {
        showNotification('Order ID tidak valid', 'error');
        return;
    }

    if (showPreview) {
        showReceiptPreview(orderId);
    } else {
        printReceiptDirect(orderId);
    }
}

/**
 * Auto print receipt after payment success
 * Called from payment success callback
 * @param {number} orderId
 */
function autoPrintReceiptAfterPayment(orderId) {
    if (!orderId || orderId <= 0) {
        console.error('Invalid order ID for auto print');
        return;
    }

    if (confirm('Pembayaran berhasil! Cetak struk sekarang?')) {
        showReceiptPreview(orderId);
    } else {
        showNotification('Struk dapat dicetak dari menu Dashboard', 'info', 5000);
    }
}

/**
 * Format currency for receipt display
 * @param {number} amount
 * @returns {string}
 */
function formatReceiptCurrency(amount) {
    return new Intl.NumberFormat('id-ID', {
        style: 'currency',
        currency: 'IDR',
        minimumFractionDigits: 0,
        maximumFractionDigits: 0
    }).format(amount);
}

/**
 * Validate if receipt can be printed for order
 * @param {number} orderId
 * @returns {Promise<boolean>}
 */
async function canPrintReceipt(orderId) {
    try {
        const response = await fetch(`/Receipt/GetReceiptData?orderId=${orderId}`);
        const data = await response.json();
        return data.success;
    } catch (error) {
        console.error('Error validating receipt:', error);
        return false;
    }
}

/**
 * Print multiple receipts (batch print)
 * @param {Array<number>} orderIds
 */
function printMultipleReceipts(orderIds) {
    if (!orderIds || orderIds.length === 0) {
        showNotification('Tidak ada order yang dipilih', 'warning');
        return;
    }

    if (orderIds.length > 10) {
        if (!confirm(`Anda akan mencetak ${orderIds.length} struk. Lanjutkan?`)) {
            return;
        }
    }

    orderIds.forEach((orderId, index) => {
        setTimeout(() => {
            printReceiptDirect(orderId);
        }, index * 1000); 
    });

    showNotification(`Mencetak ${orderIds.length} struk...`, 'info', 3000);
}

/**
 * Download receipt as text file (for backup/email)
 * @param {number} orderId 
 */
async function downloadReceiptAsText(orderId) {
    try {
        const receiptData = await getReceiptData(orderId);
        if (!receiptData) return;

        let textContent = '';
        textContent += `${receiptData.restaurantName}\n`;
        textContent += `${receiptData.restaurantAddress}\n`;
        textContent += `Telp: ${receiptData.restaurantPhone}\n`;
        textContent += `${'='.repeat(40)}\n\n`;
        textContent += `Order No: ${receiptData.orderNumber}\n`;
        textContent += `Tanggal: ${new Date(receiptData.orderDate).toLocaleDateString('id-ID')}\n`;
        textContent += `Kasir: ${receiptData.cashierName}\n`;
        textContent += `Tipe: ${receiptData.orderType}\n\n`;
        textContent += `${'='.repeat(40)}\n`;
        textContent += `ITEMS:\n`;
        textContent += `${'='.repeat(40)}\n`;

        receiptData.items.forEach(item => {
            textContent += `${item.itemName}\n`;
            textContent += `  ${item.quantity} x ${formatReceiptCurrency(item.unitPrice)} = ${formatReceiptCurrency(item.subtotal)}\n`;
            if (item.orderNote) {
                textContent += `  Note: ${item.orderNote}\n`;
            }
        });

        textContent += `\n${'='.repeat(40)}\n`;
        textContent += `Subtotal: ${formatReceiptCurrency(receiptData.subtotal)}\n`;
        if (receiptData.menuDiscountTotal > 0) {
            textContent += `Diskon Menu: -${formatReceiptCurrency(receiptData.menuDiscountTotal)}\n`;
        }
        if (receiptData.orderDiscount > 0) {
            textContent += `Diskon Order: -${formatReceiptCurrency(receiptData.orderDiscount)}\n`;
        }
        textContent += `PPN (11%): ${formatReceiptCurrency(receiptData.ppn)}\n`;
        textContent += `TOTAL: ${formatReceiptCurrency(receiptData.total)}\n`;
        textContent += `\nPembayaran: ${formatReceiptCurrency(receiptData.amountPaid)}\n`;
        textContent += `Kembalian: ${formatReceiptCurrency(receiptData.changeAmount)}\n`;

        if (receiptData.totalSavings > 0) {
            textContent += `\nTOTAL HEMAT: ${formatReceiptCurrency(receiptData.totalSavings)}\n`;
        }

        textContent += `\n${receiptData.receiptFooter}\n`;

      
        const blob = new Blob([textContent], { type: 'text/plain' });
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `Struk_${receiptData.orderNumber}.txt`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);

        showNotification('Struk berhasil didownload', 'success');
    } catch (error) {
        console.error('Error downloading receipt:', error);
        showNotification('Gagal mendownload struk', 'error');
    }
}

function setupReceiptKeyboardShortcuts() {
    document.addEventListener('keydown', function (e) {
  
        if ((e.ctrlKey || e.metaKey) && e.key === 'p') {
            const receiptContent = document.getElementById('receiptContent');
            if (receiptContent) {
                e.preventDefault();
                window.print();
            }
        }
    });
}


function initializeReceiptSystem() {
    console.log('Receipt system initialized');
    setupReceiptKeyboardShortcuts();
}


if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initializeReceiptSystem);
} else {
    initializeReceiptSystem();
}


window.Receipt = {
    showPreview: showReceiptPreview,
    printDirect: printReceiptDirect,
    print: printReceipt,
    autoPrintAfterPayment: autoPrintReceiptAfterPayment,
    getData: getReceiptData,
    canPrint: canPrintReceipt,
    printMultiple: printMultipleReceipts,
    downloadAsText: downloadReceiptAsText,
    formatCurrency: formatReceiptCurrency
};

window.showReceiptPreview = showReceiptPreview;
window.printReceiptDirect = printReceiptDirect;
window.printReceipt = printReceipt;
window.autoPrintReceiptAfterPayment = autoPrintReceiptAfterPayment;