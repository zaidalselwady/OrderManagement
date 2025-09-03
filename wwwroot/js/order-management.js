// Order Management JavaScript for Add Order Page

class OrderManager {
    constructor() {
        this.customers = [];
        this.inventoryItems = [];
        this.units = [];
        this.orderDetails = [];
        this.isOrderSaved = false;
        
        this.init();
    }

    async init() {
        try {
            this.setupForm();
            await this.loadData();
            this.setupEventListeners();
            this.addOrderDetailRow();
        } catch (error) {
            console.error('Order management initialization failed:', error);
            window.oms.showToast('Failed to initialize order management', 'error');
        }
    }

    setupForm() {
        // Set today's date
        const orderDateInput = document.getElementById('orderDate');
        if (orderDateInput) {
            orderDateInput.value = new Date().toISOString().split('T')[0];
        }

        // Set order number placeholder
        const orderNumberInput = document.getElementById('orderNumber');
        if (orderNumberInput) {
            this.loadNextOrderNumber();
        }
    }

    async loadData() {
        try {
            // Load customers, inventory items, and units in parallel
            const [customersResponse, inventoryResponse, unitsResponse] = await Promise.all([
                window.oms.makeApiCall('/Data/GetCustomers'),
                window.oms.makeApiCall('/Data/GetInventoryItems'),
                window.oms.makeApiCall('/Data/GetUnits')
            ]);

            this.customers = Array.isArray(customersResponse) ? customersResponse : [];
            this.inventoryItems = Array.isArray(inventoryResponse) ? inventoryResponse : [];
            this.units = Array.isArray(unitsResponse) ? unitsResponse : [];

            this.populateCustomerDropdown();
        } catch (error) {
            console.error('Failed to load data:', error);
            window.oms.showToast('Failed to load required data', 'error');
        }
    }

    async loadNextOrderNumber() {
        try {
            const response = await window.oms.makeApiCall('/Order/GetNextOrderNumber');
            const orderNumberInput = document.getElementById('orderNumber');
            if (orderNumberInput && response.orderNumber) {
                orderNumberInput.value = `#${response.orderNumber} (Auto-generated)`;
            }
        } catch (error) {
            console.error('Failed to load next order number:', error);
        }
    }

    populateCustomerDropdown() {
        const customerSelect = document.getElementById('customerId');
        if (!customerSelect) return;

        customerSelect.innerHTML = '<option value="" disabled selected>Select a Customer</option>';
        
        this.customers.forEach(customer => {
            const option = document.createElement('option');
            option.value = customer.custSupId;
            option.textContent = customer.nameEnglish;
            option.dataset.name = customer.nameEnglish;
            customerSelect.appendChild(option);
        });

        // Initialize Select2 if available
        if (typeof $ !== 'undefined' && $.fn.select2) {
            $(customerSelect).select2({
                theme: 'bootstrap-5',
                placeholder: 'Select a Customer',
                allowClear: true,
                width: '100%'
            });

            $(customerSelect).on('select2:select', (e) => {
                const selectedOption = e.params.data.element;
                this.onCustomerSelected(selectedOption.value, selectedOption.dataset.name);
            });
        } else {
            customerSelect.addEventListener('change', (e) => {
                const selectedOption = e.target.options[e.target.selectedIndex];
                this.onCustomerSelected(selectedOption.value, selectedOption.dataset.name);
            });
        }
    }

    onCustomerSelected(customerId, customerName) {
        // Update hidden customer fields if they exist
        const customerIdHidden = document.getElementById('customerIdHidden');
        const customerNameHidden = document.getElementById('customerNameHidden');
        
        if (customerIdHidden) customerIdHidden.value = customerId;
        if (customerNameHidden) customerNameHidden.value = customerName;
    }

    setupEventListeners() {
        // Form submission
        const orderForm = document.getElementById('orderForm');
        if (orderForm) {
            orderForm.addEventListener('submit', (e) => {
                e.preventDefault();
                this.saveOrder();
            });
        }

        // Preview order
        window.previewOrder = () => this.previewOrder();
        
        // Reset form
        window.resetForm = () => this.resetForm();

        // Save customer
        window.saveCustomer = () => this.saveCustomer();

        // Add order detail row
        window.addOrderDetailRow = () => this.addOrderDetailRow();
    }

    addOrderDetailRow() {
        const tbody = document.getElementById('orderDetailsBody');
        if (!tbody) return;

        const row = document.createElement('tr');
        const rowIndex = tbody.children.length;
        
        row.innerHTML = `
            <td>
                <select class="form-control item-barcode" data-row="${rowIndex}" required>
                    <option value="">Select Bar Code</option>
                    ${this.inventoryItems.map(item => 
                        `<option value="${item.barCode}" data-item-id="${item.itemChildId}" 
                                 data-description="${item.itemDescription}" 
                                 data-price="${item.price}" 
                                 data-unit-id="${item.unitId}">
                            ${item.barCode}
                        </option>`
                    ).join('')}
                </select>
            </td>
            <td>
                <select class="form-control item-id" data-row="${rowIndex}" required>
                    <option value="">Select Item</option>
                    ${this.inventoryItems.map(item => 
                        `<option value="${item.itemChildId}" data-barcode="${item.barCode}" 
                                 data-description="${item.itemDescription}" 
                                 data-price="${item.price}" 
                                 data-unit-id="${item.unitId}">
                            ${item.itemChildId}
                        </option>`
                    ).join('')}
                </select>
            </td>
            <td>
                <input type="text" class="form-control item-description" data-row="${rowIndex}" readonly>
            </td>
            <td>
                <select class="form-control item-unit" data-row="${rowIndex}" required>
                    <option value="">Select Unit</option>
                    ${this.units.map(unit => 
                        `<option value="${unit.unitId}">${unit.unitDescriptionEnglish}</option>`
                    ).join('')}
                </select>
            </td>
            <td>
                <input type="number" class="form-control item-quantity" data-row="${rowIndex}" 
                       min="0" step="1" value="1" required>
            </td>
            <td>
                <input type="number" class="form-control item-bonus" data-row="${rowIndex}" 
                       min="0" step="1" value="0">
            </td>
            <td>
                <input type="number" class="form-control item-price" data-row="${rowIndex}" 
                       min="0" step="0.01" value="0" readonly>
            </td>
            <td>
                <input type="number" class="form-control item-discount" data-row="${rowIndex}" 
                       min="0" max="100" step="0.01" value="0">
            </td>
            <td>
                <textarea class="form-control item-notes" data-row="${rowIndex}" rows="1"></textarea>
            </td>
            <td class="item-total" data-row="${rowIndex}">0.00</td>
            <td>
                <button type="button" class="btn btn-danger btn-sm" onclick="orderManager.removeOrderDetailRow(this)">
                    <i class="fas fa-trash"></i>
                </button>
            </td>
        `;

        tbody.appendChild(row);
        this.setupRowEventListeners(row, rowIndex);
        this.initializeRowSelect2(row);
    }

    setupRowEventListeners(row, rowIndex) {
        const barcodeSelect = row.querySelector('.item-barcode');
        const itemSelect = row.querySelector('.item-id');
        const descriptionInput = row.querySelector('.item-description');
        const unitSelect = row.querySelector('.item-unit');
        const priceInput = row.querySelector('.item-price');
        const quantityInput = row.querySelector('.item-quantity');
        const discountInput = row.querySelector('.item-discount');

        // Auto-fill when barcode is selected
        barcodeSelect.addEventListener('change', (e) => {
            const option = e.target.options[e.target.selectedIndex];
            if (option.value) {
                itemSelect.value = option.dataset.itemId;
                descriptionInput.value = option.dataset.description;
                priceInput.value = option.dataset.price;
                unitSelect.value = option.dataset.unitId;
                this.calculateRowTotal(rowIndex);
                this.triggerSelect2Update(itemSelect);
                this.triggerSelect2Update(unitSelect);
            }
        });

        // Auto-fill when item is selected
        itemSelect.addEventListener('change', (e) => {
            const option = e.target.options[e.target.selectedIndex];
            if (option.value) {
                barcodeSelect.value = option.dataset.barcode;
                descriptionInput.value = option.dataset.description;
                priceInput.value = option.dataset.price;
                unitSelect.value = option.dataset.unitId;
                this.calculateRowTotal(rowIndex);
                this.triggerSelect2Update(barcodeSelect);
                this.triggerSelect2Update(unitSelect);
            }
        });

        // Recalculate on quantity or discount change
        quantityInput.addEventListener('input', () => this.calculateRowTotal(rowIndex));
        discountInput.addEventListener('input', () => this.calculateRowTotal(rowIndex));

        // Check if we need to add a new row
        [barcodeSelect, itemSelect, quantityInput].forEach(element => {
            element.addEventListener('change', () => {
                this.checkAndAddNewRow(rowIndex);
            });
        });
    }

    initializeRowSelect2(row) {
        if (typeof $ === 'undefined' || !$.fn.select2) return;

        const selects = row.querySelectorAll('select');
        selects.forEach(select => {
            $(select).select2({
                theme: 'bootstrap-5',
                placeholder: select.options[0].textContent,
                allowClear: true,
                width: '100%'
            });
        });
    }

    triggerSelect2Update(selectElement) {
        if (typeof $ !== 'undefined' && $.fn.select2) {
            $(selectElement).trigger('change');
        }
    }

    calculateRowTotal(rowIndex) {
        const row = document.querySelector(`tr:has([data-row="${rowIndex}"])`);
        if (!row) return;

        const quantity = parseFloat(row.querySelector('.item-quantity').value) || 0;
        const price = parseFloat(row.querySelector('.item-price').value) || 0;
        const discount = parseFloat(row.querySelector('.item-discount').value) || 0;

        let total = quantity * price;
        total = total - (total * (discount / 100));

        const totalCell = row.querySelector('.item-total');
        totalCell.textContent = total.toFixed(2);

        this.updateOrderTotal();
    }

    updateOrderTotal() {
        const totalCells = document.querySelectorAll('.item-total');
        let orderTotal = 0;

        totalCells.forEach(cell => {
            orderTotal += parseFloat(cell.textContent) || 0;
        });

        const totalAmountInput = document.getElementById('totalAmount');
        if (totalAmountInput) {
            totalAmountInput.value = orderTotal.toFixed(2);
        }
    }

    checkAndAddNewRow(currentRowIndex) {
        const tbody = document.getElementById('orderDetailsBody');
        if (!tbody) return;

        const currentRow = tbody.children[currentRowIndex];
        if (!currentRow) return;

        const barcode = currentRow.querySelector('.item-barcode').value;
        const itemId = currentRow.querySelector('.item-id').value;
        const quantity = currentRow.querySelector('.item-quantity').value;

        // If current row has all required fields filled and it's the last row
        if (barcode && itemId && quantity && currentRowIndex === tbody.children.length - 1) {
            this.addOrderDetailRow();
        }
    }

    removeOrderDetailRow(button) {
        const row = button.closest('tr');
        const tbody = document.getElementById('orderDetailsBody');
        
        // Don't remove if it's the only row
        if (tbody.children.length <= 1) {
            window.oms.showToast('At least one order detail row is required', 'warning');
            return;
        }

        row.remove();
        this.updateOrderTotal();
    }

    async saveCustomer() {
        const form = document.getElementById('customerForm');
        if (!form || !window.oms.validateForm(form)) {
            window.oms.showToast('Please fill in all required customer fields', 'error');
            return;
        }

        try {
            const customerData = {
                nameEnglish: document.getElementById('customerNameEnglish').value.trim(),
                nameArabic: document.getElementById('customerNameArabic').value.trim() || null,
                customerNumber: document.getElementById('customerNumber').value.trim() || null,
                email: document.getElementById('customerEmail').value.trim() || null,
                phone1: document.getElementById('customerPhone1').value.trim() || null,
                phone2: document.getElementById('customerPhone2').value.trim() || null,
                address1: document.getElementById('customerAddress').value.trim() || null,
                discountPercent: parseFloat(document.getElementById('customerDiscountPercent').value) || null,
                contactPerson: document.getElementById('customerContactPerson').value.trim() || null
            };

            const response = await window.oms.makeApiCall('/Customer/Create', {
                method: 'POST',
                body: JSON.stringify(customerData)
            });

            if (response.success) {
                window.oms.showToast('Customer created successfully', 'success');
                
                // Add to customers list and update dropdown
                this.customers.push({
                    custSupId: response.customerId,
                    nameEnglish: customerData.nameEnglish
                });
                
                this.populateCustomerDropdown();
                
                // Select the new customer
                const customerSelect = document.getElementById('customerId');
                customerSelect.value = response.customerId;
                this.onCustomerSelected(response.customerId, customerData.nameEnglish);
                
                if (typeof $ !== 'undefined' && $.fn.select2) {
                    $(customerSelect).val(response.customerId).trigger('change');
                }

                // Close modal
                const modal = bootstrap.Modal.getInstance(document.getElementById('customerModal'));
                modal?.hide();
                
                // Reset form
                form.reset();
            } else {
                throw new Error(response.error || 'Failed to create customer');
            }
        } catch (error) {
            console.error('Error saving customer:', error);
            window.oms.showToast(`Failed to save customer: ${error.message}`, 'error');
        }
    }

    async saveOrder() {
        if (this.isOrderSaved) {
            window.oms.showToast('Order already saved. Click Reset to create a new order.', 'warning');
            return;
        }

        const form = document.getElementById('orderForm');
        if (!form || !window.oms.validateForm(form)) {
            window.oms.showToast('Please fill in all required fields correctly', 'error');
            return;
        }

        try {
            // Collect order data
            const orderData = this.collectOrderData();
            if (!orderData) return;

            const response = await window.oms.makeApiCall('/Order/Create', {
                method: 'POST',
                body: JSON.stringify(orderData)
            });

            if (response.success) {
                this.isOrderSaved = true;
                document.getElementById('orderNumber').value = `#${response.orderNumber}`;
                document.getElementById('saveOrderBtn').disabled = true;
                
                window.oms.showToast('Order created successfully!', 'success');
                
                // Scroll to top
                window.scrollTo({ top: 0, behavior: 'smooth' });
            } else {
                throw new Error(response.error || 'Failed to create order');
            }
        } catch (error) {
            console.error('Error saving order:', error);
            window.oms.showToast(`Failed to save order: ${error.message}`, 'error');
        }
    }

    collectOrderData() {
        const customerId = document.getElementById('customerId').value;
        if (!customerId) {
            window.oms.showToast('Please select a customer', 'error');
            return null;
        }

        // Collect order details
        const orderDetails = [];
        const tbody = document.getElementById('orderDetailsBody');
        
        for (const row of tbody.children) {
            const barcode = row.querySelector('.item-barcode').value;
            const itemId = row.querySelector('.item-id').value;
            const description = row.querySelector('.item-description').value;
            const unitId = row.querySelector('.item-unit').value;
            const quantity = parseInt(row.querySelector('.item-quantity').value) || 0;
            const bonus = parseInt(row.querySelector('.item-bonus').value) || 0;
            const price = parseFloat(row.querySelector('.item-price').value) || 0;
            const discount = parseFloat(row.querySelector('.item-discount').value) || 0;
            const notes = row.querySelector('.item-notes').value.trim();

            // Only include rows with required data
            if (barcode && itemId && quantity > 0) {
                orderDetails.push({
                    barCode: barcode,
                    itemChildId: parseInt(itemId),
                    itemDescription: description,
                    unitId: parseInt(unitId),
                    quantity: quantity,
                    bonusQuantity: bonus,
                    price: price,
                    discountPercent: discount,
                    itemNotes: notes || null
                });
            }
        }

        if (orderDetails.length === 0) {
            window.oms.showToast('Please add at least one order item', 'error');
            return null;
        }

        return {
            customerId: parseInt(customerId),
            orderDate: document.getElementById('orderDate').value,
            receivedDate: document.getElementById('receivedDate').value || null,
            deliveryTerms: document.getElementById('deliveryTerms').value.trim() || null,
            paymentTerms: document.getElementById('paymentTerms').value.trim() || null,
            notes: document.getElementById('notes').value.trim() || null,
            orderDetails: orderDetails
        };
    }

    previewOrder() {
        const orderData = this.collectOrderData();
        if (!orderData) return;

        const modal = document.getElementById('orderPreviewModal');
        const modalBody = document.getElementById('orderPreviewBody');
        
        if (!modal || !modalBody) return;

        const customer = this.customers.find(c => c.custSupId == orderData.customerId);
        const totalAmount = orderData.orderDetails.reduce((sum, detail) => {
            const lineTotal = detail.quantity * detail.price;
            const discount = lineTotal * (detail.discountPercent / 100);
            return sum + (lineTotal - discount);
        }, 0);

        modalBody.innerHTML = `
            <div class="order-preview">
                <div class="row mb-4">
                    <div class="col-md-6">
                        <h5>Order Information</h5>
                        <p><strong>Customer:</strong> ${customer?.nameEnglish || 'Unknown'}</p>
                        <p><strong>Order Date:</strong> ${window.oms.formatDate(orderData.orderDate)}</p>
                        <p><strong>Received Date:</strong> ${orderData.receivedDate ? window.oms.formatDate(orderData.receivedDate) : 'N/A'}</p>
                        <p><strong>Total Amount:</strong> ${window.oms.formatCurrency(totalAmount)}</p>
                    </div>
                    <div class="col-md-6">
                        <h5>Terms & Notes</h5>
                        <p><strong>Delivery Terms:</strong> ${orderData.deliveryTerms || 'N/A'}</p>
                        <p><strong>Payment Terms:</strong> ${orderData.paymentTerms || 'N/A'}</p>
                        <p><strong>Notes:</strong> ${orderData.notes || 'N/A'}</p>
                    </div>
                </div>
                
                <h5>Order Items</h5>
                <div class="table-responsive">
                    <table class="table table-bordered">
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
                            ${orderData.orderDetails.map(detail => {
                                const lineTotal = detail.quantity * detail.price;
                                const discount = lineTotal * (detail.discountPercent / 100);
                                const finalTotal = lineTotal - discount;
                                
                                return `
                                    <tr>
                                        <td>${detail.itemChildId}</td>
                                        <td>${detail.itemDescription}</td>
                                        <td>${detail.quantity}</td>
                                        <td>${window.oms.formatCurrency(detail.price)}</td>
                                        <td>${detail.discountPercent}%</td>
                                        <td>${window.oms.formatCurrency(finalTotal)}</td>
                                    </tr>
                                `;
                            }).join('')}
                        </tbody>
                        <tfoot>
                            <tr class="table-dark">
                                <th colspan="5">Total</th>
                                <th>${window.oms.formatCurrency(totalAmount)}</th>
                            </tr>
                        </tfoot>
                    </table>
                </div>
            </div>
        `;

        const modalInstance = new bootstrap.Modal(modal);
        modalInstance.show();
    }

    resetForm() {
        if (this.isOrderSaved) {
            const confirmed = confirm('Are you sure you want to start a new order? Current order data will be cleared.');
            if (!confirmed) return;
        }

        // Reset form fields
        const form = document.getElementById('orderForm');
        if (form) {
            form.reset();
        }

        // Reset date to today
        const orderDateInput = document.getElementById('orderDate');
        if (orderDateInput) {
            orderDateInput.value = new Date().toISOString().split('T')[0];
        }

        // Clear customer selection
        const customerSelect = document.getElementById('customerId');
        if (customerSelect) {
            customerSelect.value = '';
            if (typeof $ !== 'undefined' && $.fn.select2) {
                $(customerSelect).val(null).trigger('change');
            }
        }

        // Reset order details table
        const tbody = document.getElementById('orderDetailsBody');
        if (tbody) {
            tbody.innerHTML = '';
            this.addOrderDetailRow();
        }

        // Reset order number
        this.loadNextOrderNumber();

        // Reset flags and buttons
        this.isOrderSaved = false;
        document.getElementById('saveOrderBtn').disabled = false;

        window.oms.showToast('Form reset successfully', 'success');
    }

    exportToPDF() {
        const orderData = this.collectOrderData();
        if (!orderData) return;

        // This would implement PDF export functionality
        window.oms.showToast('PDF export functionality would be implemented here', 'info');
    }
}

// Initialize order manager when DOM is ready
let orderManager;
document.addEventListener('DOMContentLoaded', () => {
    orderManager = new OrderManager();
});