// Dashboard JavaScript for Order Management System

class DashboardManager {
    constructor() {
        this.orders = [];
        this.filteredOrders = [];
        this.currentView = 'grid';
        this.chart = null;
        this.searchTerm = '';
        this.statusFilter = '';
        this.dateFromFilter = '';
        this.dateToFilter = '';
        
        this.init();
    }

    async init() {
        try {
            await this.loadOrders();
            this.setupEventListeners();
            this.initializeChart();
            this.setupViewToggle();
            this.applyFilters();
        } catch (error) {
            console.error('Dashboard initialization failed:', error);
            window.oms.showToast('Failed to initialize dashboard', 'error');
        }
    }

    async loadOrders() {
        try {
            const response = await window.oms.makeApiCall('/Data/GetOrders');
            this.orders = Array.isArray(response) ? response : [];
            this.filteredOrders = [...this.orders];
            this.updateStatistics();
        } catch (error) {
            console.error('Failed to load orders:', error);
            this.orders = [];
            this.filteredOrders = [];
        }
    }

    setupEventListeners() {
        // Search functionality
        const searchInput = document.getElementById('search');
        if (searchInput) {
            searchInput.addEventListener('input', window.oms.debounce((e) => {
                this.searchTerm = e.target.value.toLowerCase();
                this.applyFilters();
            }, 300));
        }

        // Filter functionality
        const statusFilter = document.getElementById('status-filter');
        if (statusFilter) {
            statusFilter.addEventListener('change', (e) => {
                this.statusFilter = e.target.value;
                this.applyFilters();
            });
        }

        const dateFromFilter = document.getElementById('date-from');
        if (dateFromFilter) {
            dateFromFilter.addEventListener('change', (e) => {
                this.dateFromFilter = e.target.value;
                this.applyFilters();
            });
        }

        const dateToFilter = document.getElementById('date-to');
        if (dateToFilter) {
            dateToFilter.addEventListener('change', (e) => {
                this.dateToFilter = e.target.value;
                this.applyFilters();
            });
        }

        // Apply filters button
        const applyFiltersBtn = document.getElementById('apply-filters');
        if (applyFiltersBtn) {
            applyFiltersBtn.addEventListener('click', () => {
                this.applyFilters();
            });
        }

        // Refresh button
        const refreshBtn = document.getElementById('refresh-btn');
        if (refreshBtn) {
            refreshBtn.addEventListener('click', () => {
                this.refreshDashboard();
            });
        }

        // Export button
        const exportBtn = document.getElementById('export-btn');
        if (exportBtn) {
            exportBtn.addEventListener('click', () => {
                this.exportOrders();
            });
        }

        // Theme change listener for chart updates
        window.addEventListener('themeChanged', () => {
            this.updateChart();
        });
    }

    setupViewToggle() {
        const gridViewBtn = document.getElementById('grid-view-btn');
        const tableViewBtn = document.getElementById('table-view-btn');
        const gridContainer = document.getElementById('data-grid');
        const tableContainer = document.getElementById('data-table');

        if (gridViewBtn && tableViewBtn && gridContainer && tableContainer) {
            gridViewBtn.addEventListener('click', () => {
                this.currentView = 'grid';
                gridViewBtn.classList.add('active');
                tableViewBtn.classList.remove('active');
                gridContainer.style.display = 'grid';
                tableContainer.style.display = 'none';
                this.renderOrders();
            });

            tableViewBtn.addEventListener('click', () => {
                this.currentView = 'table';
                tableViewBtn.classList.add('active');
                gridViewBtn.classList.remove('active');
                gridContainer.style.display = 'none';
                tableContainer.style.display = 'block';
                this.renderOrders();
            });
        }
    }

    applyFilters() {
        let filtered = [...this.orders];

        // Apply search filter
        if (this.searchTerm) {
            filtered = filtered.filter(order => 
                Object.values(order).some(value => 
                    String(value).toLowerCase().includes(this.searchTerm)
                )
            );
        }

        // Apply status filter
        if (this.statusFilter) {
            filtered = filtered.filter(order => 
                order.status && order.status.toLowerCase() === this.statusFilter.toLowerCase()
            );
        }

        // Apply date range filter
        if (this.dateFromFilter) {
            filtered = filtered.filter(order => {
                const orderDate = new Date(order.orderDate);
                const fromDate = new Date(this.dateFromFilter);
                return orderDate >= fromDate;
            });
        }

        if (this.dateToFilter) {
            filtered = filtered.filter(order => {
                const orderDate = new Date(order.orderDate);
                const toDate = new Date(this.dateToFilter);
                return orderDate <= toDate;
            });
        }

        this.filteredOrders = filtered;
        this.renderOrders();
        this.updateStatistics();
    }

    renderOrders() {
        if (this.currentView === 'grid') {
            this.renderGridView();
        } else {
            this.renderTableView();
        }
    }

    renderGridView() {
        const gridContainer = document.getElementById('data-grid');
        if (!gridContainer) return;

        if (!this.filteredOrders.length) {
            gridContainer.innerHTML = '<div class="no-data">No orders found</div>';
            return;
        }

        gridContainer.innerHTML = this.filteredOrders.map(order => `
            <div class="order-card animate__animated animate__fadeIn" data-order-id="${order.orderId}">
                <div class="order-header">
                    <h5>Order #${order.orderNumber}</h5>
                    <span class="status-badge status-${order.status.toLowerCase()}">${order.status}</span>
                </div>
                <div class="order-body">
                    <p><strong>Customer:</strong> ${order.customerName || 'N/A'}</p>
                    <p><strong>Date:</strong> ${window.oms.formatDate(order.orderDate)}</p>
                    <p><strong>Amount:</strong> ${window.oms.formatCurrency(order.amount)}</p>
                </div>
                <div class="order-actions">
                    <button class="btn btn-sm btn-primary" onclick="dashboard.viewOrderDetails(${order.orderId})">
                        View Details
                    </button>
                </div>
            </div>
        `).join('');
    }

    renderTableView() {
        const tableBody = document.getElementById('orders-table-body');
        if (!tableBody) return;

        if (!this.filteredOrders.length) {
            tableBody.innerHTML = '<tr><td colspan="6" class="text-center">No orders found</td></tr>';
            return;
        }

        tableBody.innerHTML = this.filteredOrders.map(order => `
            <tr data-order-id="${order.orderId}" class="animate__animated animate__fadeIn">
                <td>${order.orderNumber}</td>
                <td>${order.customerName || 'N/A'}</td>
                <td>${window.oms.formatDate(order.orderDate)}</td>
                <td>${window.oms.formatCurrency(order.amount)}</td>
                <td><span class="status-badge status-${order.status.toLowerCase()}">${order.status}</span></td>
                <td>
                    <button class="btn btn-sm btn-primary" onclick="dashboard.viewOrderDetails(${order.orderId})">
                        Details
                    </button>
                </td>
            </tr>
        `).join('');
    }

    async viewOrderDetails(orderId) {
        try {
            const orderDetails = await window.oms.makeApiCall(`/Order/GetOrderDetails/${orderId}`);
            this.showOrderDetailsModal(orderDetails);
        } catch (error) {
            window.oms.showToast('Failed to load order details', 'error');
        }
    }

    showOrderDetailsModal(order) {
        const modal = document.getElementById('orderModal');
        const modalBody = document.getElementById('orderModalBody');
        
        if (!modal || !modalBody) return;

        modalBody.innerHTML = `
            <div class="order-details">
                <div class="row mb-3">
                    <div class="col-md-6">
                        <h6>Order Information</h6>
                        <p><strong>Order Number:</strong> ${order.orderNumber}</p>
                        <p><strong>Order Date:</strong> ${window.oms.formatDate(order.orderDate)}</p>
                        <p><strong>Customer:</strong> ${order.customerName || 'N/A'}</p>
                        <p><strong>Status:</strong> <span class="status-badge status-${order.status.toLowerCase()}">${order.status}</span></p>
                    </div>
                    <div class="col-md-6">
                        <h6>Additional Details</h6>
                        <p><strong>Amount:</strong> ${window.oms.formatCurrency(order.amount)}</p>
                        <p><strong>Received Date:</strong> ${order.receivedDate ? window.oms.formatDate(order.receivedDate) : 'N/A'}</p>
                        <p><strong>Delivery Terms:</strong> ${order.deliveryTerms || 'N/A'}</p>
                        <p><strong>Payment Terms:</strong> ${order.paymentTerms || 'N/A'}</p>
                    </div>
                </div>
                
                ${order.orderDetails && order.orderDetails.length > 0 ? `
                    <h6>Order Items</h6>
                    <div class="table-responsive">
                        <table class="table table-sm table-bordered">
                            <thead>
                                <tr>
                                    <th>Item</th>
                                    <th>Description</th>
                                    <th>Qty</th>
                                    <th>Price</th>
                                    <th>Discount</th>
                                    <th>Total</th>
                                </tr>
                            </thead>
                            <tbody>
                                ${order.orderDetails.map(detail => {
                                    const lineTotal = detail.orderQuantity * detail.price;
                                    const discount = lineTotal * (detail.discountPercent / 100);
                                    const finalTotal = lineTotal - discount;
                                    
                                    return `
                                        <tr>
                                            <td>${detail.itemChildId}</td>
                                            <td>${detail.itemDescription}</td>
                                            <td>${detail.orderQuantity}</td>
                                            <td>${window.oms.formatCurrency(detail.price)}</td>
                                            <td>${detail.discountPercent}%</td>
                                            <td>${window.oms.formatCurrency(finalTotal)}</td>
                                        </tr>
                                    `;
                                }).join('')}
                            </tbody>
                        </table>
                    </div>
                ` : '<p>No order details available.</p>'}
                
                ${order.notes ? `
                    <h6>Notes</h6>
                    <p>${order.notes}</p>
                ` : ''}
            </div>
        `;

        const modalInstance = new bootstrap.Modal(modal);
        modalInstance.show();
    }

    updateStatistics() {
        const totalOrdersEl = document.getElementById('total-orders');
        const totalAmountEl = document.getElementById('total-amount');
        const recentOrdersEl = document.getElementById('recent-orders');

        if (totalOrdersEl) {
            totalOrdersEl.textContent = this.filteredOrders.length;
        }

        if (totalAmountEl) {
            const totalAmount = this.filteredOrders.reduce((sum, order) => sum + (order.amount || 0), 0);
            totalAmountEl.textContent = window.oms.formatCurrency(totalAmount);
        }

        if (recentOrdersEl) {
            const sevenDaysAgo = new Date();
            sevenDaysAgo.setDate(sevenDaysAgo.getDate() - 7);
            const recentCount = this.filteredOrders.filter(order => 
                new Date(order.orderDate) >= sevenDaysAgo
            ).length;
            recentOrdersEl.textContent = recentCount;
        }
    }

    initializeChart() {
        const canvas = document.getElementById('orders-chart');
        if (!canvas) return;

        const ctx = canvas.getContext('2d');
        this.updateChart();
    }

    updateChart() {
        const canvas = document.getElementById('orders-chart');
        if (!canvas) return;

        const ctx = canvas.getContext('2d');
        const isDarkMode = document.body.classList.contains('dark-mode');

        // Calculate status counts
        const statusCounts = this.filteredOrders.reduce((acc, order) => {
            const status = order.status || 'Unknown';
            acc[status] = (acc[status] || 0) + 1;
            return acc;
        }, {});

        const labels = Object.keys(statusCounts);
        const data = Object.values(statusCounts);
        const colors = ['#f59e0b', '#3b82f6', '#10b981', '#ef4444'];

        if (this.chart) {
            this.chart.destroy();
        }

        this.chart = new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: labels,
                datasets: [{
                    data: data,
                    backgroundColor: colors.slice(0, labels.length),
                    borderColor: isDarkMode ? '#374151' : '#ffffff',
                    borderWidth: 2
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: {
                            color: isDarkMode ? '#d1d5db' : '#374151',
                            padding: 20
                        }
                    }
                }
            }
        });
    }

    async refreshDashboard() {
        window.oms.showToast('Refreshing dashboard...', 'info');
        await this.loadOrders();
        this.applyFilters();
        this.updateChart();
        window.oms.showToast('Dashboard refreshed successfully', 'success');
    }

    exportOrders() {
        if (!this.filteredOrders.length) {
            window.oms.showToast('No orders to export', 'warning');
            return;
        }

        const exportData = this.filteredOrders.map(order => ({
            'Order Number': order.orderNumber,
            'Customer': order.customerName || 'N/A',
            'Date': window.oms.formatDate(order.orderDate),
            'Amount': order.amount,
            'Status': order.status,
            'Delivery Terms': order.deliveryTerms || '',
            'Payment Terms': order.paymentTerms || '',
            'Notes': order.notes || ''
        }));

        const filename = `orders_export_${new Date().toISOString().split('T')[0]}.csv`;
        window.oms.exportToCSV(exportData, filename);
    }
}

// Initialize dashboard when DOM is ready
let dashboard;
document.addEventListener('DOMContentLoaded', () => {
    dashboard = new DashboardManager();
});

// Global functions for backward compatibility
window.refreshDashboard = () => dashboard?.refreshDashboard();
window.exportOrders = () => dashboard?.exportOrders();
window.viewOrderDetails = (orderId) => dashboard?.viewOrderDetails(orderId);