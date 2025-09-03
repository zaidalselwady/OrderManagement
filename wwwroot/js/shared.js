// Shared JavaScript Functions for Order Management System

class OrderManagementSystem {
    constructor() {
        this.isLoading = false;
        this.toastContainer = null;
        this.init();
    }

    init() {
        this.setupThemeToggle();
        this.setupToastContainer();
        this.setupGlobalEventHandlers();
        this.applyStoredTheme();
    }

    // Toast Notification System
    setupToastContainer() {
        this.toastContainer = document.getElementById('toast-container');
        if (!this.toastContainer) {
            this.toastContainer = document.createElement('div');
            this.toastContainer.className = 'toast-container';
            this.toastContainer.id = 'toast-container';
            document.body.appendChild(this.toastContainer);
        }
    }

    showToast(message, type = 'info', duration = 3000) {
        const toast = document.createElement('div');
        toast.className = `toast toast-${type} animate__animated animate__slideInRight`;
        
        const icon = this.getToastIcon(type);
        toast.innerHTML = `
            <div class="toast-content">
                <div class="toast-icon">${icon}</div>
                <div class="toast-message">${message}</div>
                <button class="toast-close" aria-label="Close">&times;</button>
            </div>
        `;

        this.toastContainer.appendChild(toast);

        // Auto remove after duration
        const autoRemove = setTimeout(() => {
            this.removeToast(toast);
        }, duration);

        // Manual close
        const closeBtn = toast.querySelector('.toast-close');
        closeBtn.addEventListener('click', () => {
            clearTimeout(autoRemove);
            this.removeToast(toast);
        });

        // Click to dismiss
        toast.addEventListener('click', () => {
            clearTimeout(autoRemove);
            this.removeToast(toast);
        });
    }

    removeToast(toast) {
        toast.classList.remove('animate__slideInRight');
        toast.classList.add('animate__slideOutRight');
        setTimeout(() => {
            if (toast.parentNode) {
                toast.parentNode.removeChild(toast);
            }
        }, 300);
    }

    getToastIcon(type) {
        const icons = {
            success: '<i class="fas fa-check-circle"></i>',
            error: '<i class="fas fa-exclamation-circle"></i>',
            warning: '<i class="fas fa-exclamation-triangle"></i>',
            info: '<i class="fas fa-info-circle"></i>'
        };
        return icons[type] || icons.info;
    }

    // Loading Spinner
    showSpinner() {
        if (this.isLoading) return;
        
        this.isLoading = true;
        const spinner = document.getElementById('loading-spinner');
        if (spinner) {
            spinner.style.display = 'flex';
        }

        // Fallback timeout
        setTimeout(() => {
            if (this.isLoading) {
                console.warn('Spinner timeout - hiding spinner');
                this.hideSpinner();
            }
        }, 30000);
    }

    hideSpinner() {
        this.isLoading = false;
        const spinner = document.getElementById('loading-spinner');
        if (spinner) {
            spinner.style.display = 'none';
        }
    }

    // Theme Management
    setupThemeToggle() {
        const themeToggle = document.getElementById('theme-toggle');
        if (themeToggle) {
            themeToggle.addEventListener('click', () => {
                this.toggleTheme();
            });
        }
    }

    toggleTheme() {
        const body = document.body;
        const isDarkMode = body.classList.contains('dark-mode');
        const themeToggle = document.getElementById('theme-toggle');

        if (isDarkMode) {
            body.classList.remove('dark-mode');
            if (themeToggle) themeToggle.textContent = '🌙';
            localStorage.setItem('theme', 'light');
        } else {
            body.classList.add('dark-mode');
            if (themeToggle) themeToggle.textContent = '☀️';
            localStorage.setItem('theme', 'dark');
        }

        // Trigger theme change event
        window.dispatchEvent(new CustomEvent('themeChanged', {
            detail: { isDarkMode: !isDarkMode }
        }));
    }

    applyStoredTheme() {
        const storedTheme = localStorage.getItem('theme');
        const themeToggle = document.getElementById('theme-toggle');
        
        if (storedTheme === 'dark') {
            document.body.classList.add('dark-mode');
            if (themeToggle) themeToggle.textContent = '☀️';
        } else {
            document.body.classList.remove('dark-mode');
            if (themeToggle) themeToggle.textContent = '🌙';
        }
    }

    // Global Event Handlers
    setupGlobalEventHandlers() {
        // Handle logout confirmation
        window.confirmLogout = (event) => {
            event.preventDefault();
            this.showConfirmDialog(
                'Logout Confirmation',
                'Are you sure you want to log out?',
                () => {
                    window.location.href = '/Home/Logout';
                }
            );
        };

        // Handle escape key to close modals
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                const openModals = document.querySelectorAll('.modal.show');
                openModals.forEach(modal => {
                    const modalInstance = bootstrap.Modal.getInstance(modal);
                    if (modalInstance) {
                        modalInstance.hide();
                    }
                });
            }
        });
    }

    // Confirmation Dialog
    showConfirmDialog(title, message, onConfirm, onCancel = null) {
        const confirmed = confirm(`${title}\n\n${message}`);
        if (confirmed && onConfirm) {
            onConfirm();
        } else if (!confirmed && onCancel) {
            onCancel();
        }
    }

    // API Helper Methods
    async makeApiCall(url, options = {}) {
        try {
            this.showSpinner();
            
            const defaultOptions = {
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'application/json'
                }
            };

            const mergedOptions = { ...defaultOptions, ...options };
            
            // Add anti-forgery token if it's a POST, PUT, or DELETE request
            if (['POST', 'PUT', 'DELETE'].includes(mergedOptions.method)) {
                const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
                if (token) {
                    mergedOptions.headers['RequestVerificationToken'] = token;
                }
            }

            const response = await fetch(url, mergedOptions);
            
            if (!response.ok) {
                let errorMessage = `HTTP ${response.status}: ${response.statusText}`;
                try {
                    const errorData = await response.json();
                    errorMessage = errorData.error || errorData.message || errorMessage;
                } catch {
                    // Use default error message
                }
                throw new Error(errorMessage);
            }

            const contentType = response.headers.get('content-type');
            if (contentType && contentType.includes('application/json')) {
                return await response.json();
            } else {
                return await response.text();
            }
        } catch (error) {
            console.error('API call failed:', error);
            this.showToast(`API Error: ${error.message}`, 'error');
            throw error;
        } finally {
            this.hideSpinner();
        }
    }

    // Form Validation Helpers
    validateForm(formElement) {
        const requiredFields = formElement.querySelectorAll('[required]');
        let isValid = true;

        requiredFields.forEach(field => {
            field.classList.remove('is-invalid');
            
            if (!field.value.trim()) {
                field.classList.add('is-invalid');
                isValid = false;
            }
        });

        // Validate email fields
        const emailFields = formElement.querySelectorAll('input[type="email"]');
        emailFields.forEach(field => {
            if (field.value && !this.isValidEmail(field.value)) {
                field.classList.add('is-invalid');
                isValid = false;
            }
        });

        // Validate number fields
        const numberFields = formElement.querySelectorAll('input[type="number"]');
        numberFields.forEach(field => {
            if (field.value && isNaN(field.value)) {
                field.classList.add('is-invalid');
                isValid = false;
            }
        });

        return isValid;
    }

    isValidEmail(email) {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return emailRegex.test(email);
    }

    // Utility Functions
    formatCurrency(amount) {
        return new Intl.NumberFormat('en-US', {
            style: 'currency',
            currency: 'USD'
        }).format(amount);
    }

    formatDate(date, locale = 'en-US') {
        if (!date) return '';
        const dateObj = typeof date === 'string' ? new Date(date) : date;
        return dateObj.toLocaleDateString(locale);
    }

    debounce(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    }

    // Export functionality
    exportToCSV(data, filename = 'export.csv') {
        if (!data || !data.length) {
            this.showToast('No data to export', 'warning');
            return;
        }

        const headers = Object.keys(data[0]);
        const csvContent = [
            headers.join(','),
            ...data.map(row => 
                headers.map(header => {
                    const value = row[header] || '';
                    return `"${String(value).replace(/"/g, '""')}"`;
                }).join(',')
            )
        ].join('\n');

        this.downloadFile(csvContent, filename, 'text/csv');
        this.showToast('Data exported successfully', 'success');
    }

    downloadFile(content, filename, mimeType) {
        const blob = new Blob([content], { type: mimeType });
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = filename;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        window.URL.revokeObjectURL(url);
    }

    // Session Management
    checkSession() {
        return fetch('/Home/CheckLogin')
            .then(response => response.json())
            .then(data => data.isLoggedIn)
            .catch(() => false);
    }

    async ensureAuthenticated() {
        const isLoggedIn = await this.checkSession();
        if (!isLoggedIn) {
            this.showToast('Session expired. Redirecting to login...', 'warning');
            setTimeout(() => {
                window.location.href = '/Home/Login';
            }, 2000);
            return false;
        }
        return true;
    }
}

// Initialize the global instance
window.oms = new OrderManagementSystem();

// Global utility functions for backward compatibility
window.showToast = (message, type, duration) => window.oms.showToast(message, type, duration);
window.showSpinner = () => window.oms.showSpinner();
window.hideSpinner = () => window.oms.hideSpinner();
window.formatCurrency = (amount) => window.oms.formatCurrency(amount);
window.formatDate = (date) => window.oms.formatDate(date);

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    console.log('Order Management System initialized');
});

// Handle page unload
window.addEventListener('beforeunload', () => {
    if (window.oms.isLoading) {
        return 'An operation is in progress. Are you sure you want to leave?';
    }
});