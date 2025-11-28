# ?? API Quick Reference Guide for Frontend

## ?? Authentication

### **Login**
```http
POST /api/auth/login
Content-Type: application/json

{
  "userId": "user123",
  "password": "password123"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": 1,
    "userId": "user123",
    "firstName": "John",
    "lastName": "Doe",
    "email": "john@example.com",
    "age": 30,
    "gender": "Male",
    "role": "Manager",
    "salary": 50000.00,
    "joinDate": "2024-01-15T00:00:00Z",
    "birthdate": "1994-05-20T00:00:00Z",
    "phone": "+1234567890",
    "currentCity": "New York",
    "companyId": 1,
    "branchId": 5
  }
}
```

**Important**: The `companyId` and `branchId` are embedded in the JWT token claims. Frontend should extract user info from `response.user` object.

---

## ?? Orders

### **Get All Orders** (Auto-filtered by branch)
```http
GET /api/orders
Authorization: Bearer {token}
```

### **Get Order by ID**
```http
GET /api/orders/{orderId}
Authorization: Bearer {token}
```

### **Create Order** (Auto-assigned to user's branch)
```http
POST /api/orders
Authorization: Bearer {token}
Content-Type: application/json

{
  "customerFullName": "John Doe",
  "customerPhone": "+1234567890",
  "customerEmail": "john@example.com",
  "paymentMethod": "Cash",
  "items": [
    {
      "productId": 10,
      "quantity": 2,
      "unitPrice": 50.00
    }
  ]
}
```

### **Update Order Status**
```http
PUT /api/orders/{orderId}/status
Authorization: Bearer {token}
Content-Type: application/json

{
  "status": "Paid",
  "orderStatus": "Paid"
}
```

### **Bulk Update Order Status**
```http
PUT /api/orders/bulk-status
Authorization: Bearer {token}
Content-Type: application/json

{
  "orderIds": [1, 2, 3],
  "status": "Paid",
  "orderStatus": "Paid"
}
```

### **Search Customers**
```http
GET /api/orders/search-customers?searchTerm=john
Authorization: Bearer {token}
```

---

## ?? Invoices

### **Get All Invoices** (Auto-filtered by branch)
```http
GET /api/invoices
Authorization: Bearer {token}
```

### **Get Filtered Invoices**
```http
GET /api/invoices?minAmount=100&maxAmount=500&status=Unpaid
Authorization: Bearer {token}
```

**Query Parameters:**
- `minAmount` (optional): Minimum invoice amount
- `maxAmount` (optional): Maximum invoice amount
- `startDate` (optional): Start date (YYYY-MM-DD)
- `endDate` (optional): End date (YYYY-MM-DD)
- `customerAddress` (optional): Filter by customer address
- `status` (optional): Paid, Unpaid, Overdue, Cancelled

### **Get Invoice by ID**
```http
GET /api/invoices/{invoiceId}
Authorization: Bearer {token}
```

### **Get Invoice by Order ID**
```http
GET /api/invoices/order/{orderId}
Authorization: Bearer {token}
```

### **Print Invoice** (Opens in browser)
```http
GET /api/invoices/{invoiceId}/print
Authorization: Bearer {token}
```

### **Bulk Print Invoices**
```http
GET /api/invoices/bulk-print?invoiceIds=1,2,3
Authorization: Bearer {token}
```

### **Add Payment to Invoice**
```http
POST /api/invoices/{invoiceId}/payments
Authorization: Bearer {token}
Content-Type: application/json

{
  "amount": 100.00,
  "paymentMethod": "Cash",
  "referenceNumber": "CHK-12345"
}
```

### **Get Invoice Payments**
```http
GET /api/invoices/{invoiceId}/payments
Authorization: Bearer {token}
```

---

## ?? Expenses

### **Get All Expenses** (Auto-filtered by branch)
```http
GET /api/expenses
Authorization: Bearer {token}
```

### **Get Expense by ID**
```http
GET /api/expenses/{expenseId}
Authorization: Bearer {token}
```

### **Create Expense** (Auto-assigned to user's branch)
```http
POST /api/expenses
Authorization: Bearer {token}
Content-Type: application/json

{
  "expenseName": "Office Supplies",
  "expenseAmount": 250.00,
  "expenseDate": "2024-11-26T10:00:00Z"
}
```

### **Update Expense**
```http
PUT /api/expenses/{expenseId}
Authorization: Bearer {token}
Content-Type: application/json

{
  "expenseName": "Office Supplies (Updated)",
  "expenseAmount": 300.00,
  "expenseDate": "2024-11-26T10:00:00Z"
}
```

### **Delete Expense**
```http
DELETE /api/expenses/{expenseId}
Authorization: Bearer {token}
```

---

## ?? Accounting & Reports

### **Get Financial Summary** (Branch-specific)
```http
GET /api/accounting/summary?startDate=2024-01-01&endDate=2024-12-31
Authorization: Bearer {token}
```

**Response:**
```json
{
  "totalIncome": 50000.00,
  "totalExpenses": 10000.00,
  "totalRefunds": 500.00,
  "netProfit": 39500.00,
  "totalSales": 48000.00,
  "totalPurchases": 8000.00,
  "cashBalance": 39500.00,
  "period": "2024-01-01 to 2024-12-31"
}
```

### **Get Daily Sales** (Branch-specific)
```http
GET /api/accounting/daily-sales?days=7
Authorization: Bearer {token}
```

### **Get Sales Graph** (Branch-specific)
```http
GET /api/accounting/sales-graph?startDate=2024-01-01&endDate=2024-01-31
Authorization: Bearer {token}
```

**Response:**
```json
{
  "labels": ["Jan 01", "Jan 02", "Jan 03"],
  "salesData": [1000, 1500, 2000],
  "expensesData": [200, 300, 250],
  "refundsData": [50, 0, 100],
  "profitData": [750, 1200, 1650],
  "ordersData": [10, 15, 20]
}
```

### **Get Payment Methods Summary** (Branch-specific)
```http
GET /api/accounting/payment-methods?startDate=2024-01-01&endDate=2024-12-31
Authorization: Bearer {token}
```

### **Get Top Products** (Branch-specific)
```http
GET /api/accounting/top-products?limit=10&startDate=2024-01-01&endDate=2024-12-31
Authorization: Bearer {token}
```

---

## ?? Customer Ledger

### **Get Customer Ledger** (Branch-specific)
```http
GET /api/ledger/customer/{customerId}?startDate=2024-01-01&endDate=2024-12-31
Authorization: Bearer {token}
```

### **Get Customer Balance** (Branch-specific)
```http
GET /api/ledger/customer/{customerId}/balance
Authorization: Bearer {token}
```

**Response:**
```json
{
  "customerId": 15,
  "currentBalance": 5000.00,
  "asOfDate": "2024-11-26T10:00:00Z"
}
```

### **Get Customer Statement** (Branch-specific)
```http
GET /api/ledger/customer/{customerId}/statement?startDate=2024-01-01&endDate=2024-12-31
Authorization: Bearer {token}
```

### **Get Aging Report** (Branch-specific)
```http
GET /api/ledger/aging-report?asOfDate=2024-11-26
Authorization: Bearer {token}
```

**Response:**
```json
{
  "reportDate": "2024-11-26T10:00:00Z",
  "asOfDate": "2024-11-26",
  "customers": [
    {
      "customerId": 15,
      "customerName": "John Doe",
      "customerPhone": "+1234567890",
      "currentBalance": 5000.00,
      "days0To30": 2000.00,
      "days31To60": 1500.00,
      "days61To90": 1000.00,
      "days91Plus": 500.00,
      "totalOutstanding": 5000.00
    }
  ],
  "totalDays0To30": 10000.00,
  "totalDays31To60": 5000.00,
  "totalDays61To90": 3000.00,
  "totalDays91Plus": 2000.00,
  "grandTotal": 20000.00,
  "totalCustomers": 50,
  "customersWithBalance": 30
}
```

### **Get Customer Aging** (Branch-specific)
```http
GET /api/ledger/customer/{customerId}/aging?asOfDate=2024-11-26
Authorization: Bearer {token}
```

### **Record Payment**
```http
POST /api/ledger/payment
Authorization: Bearer {token}
Content-Type: application/json

{
  "customerId": 15,
  "amount": 1000.00,
  "paymentMethod": "Cash",
  "referenceNumber": "PMT-001",
  "invoiceId": 25
}
```

---

## ?? Companies (Super Admin Only)

### **Get All Companies**
```http
GET /api/companies
Authorization: Bearer {token}
```

### **Get Company by ID**
```http
GET /api/companies/{companyId}
Authorization: Bearer {token}
```

### **Create Company**
```http
POST /api/companies
Authorization: Bearer {token}
Content-Type: application/json

{
  "companyName": "ABC Corp",
  "address": "123 Main St",
  "phone": "+1234567890",
  "email": "contact@abccorp.com"
}
```

### **Update Company**
```http
PUT /api/companies/{companyId}
Authorization: Bearer {token}
Content-Type: application/json

{
  "companyName": "ABC Corp (Updated)",
  "address": "456 New St",
  "phone": "+0987654321",
  "email": "new@abccorp.com"
}
```

---

## ?? Branches (Company Admin Only)

### **Get All Branches** (Company-specific)
```http
GET /api/branches
Authorization: Bearer {token}
```

### **Get Branch by ID**
```http
GET /api/branches/{branchId}
Authorization: Bearer {token}
```

### **Create Branch**
```http
POST /api/branches
Authorization: Bearer {token}
Content-Type: application/json

{
  "branchName": "Downtown Branch",
  "companyId": 1,
  "address": "789 Downtown Ave",
  "phone": "+1122334455",
  "email": "downtown@abccorp.com"
}
```

### **Update Branch**
```http
PUT /api/branches/{branchId}
Authorization: Bearer {token}
Content-Type: application/json

{
  "branchName": "Downtown Branch (Updated)",
  "address": "789 Downtown Ave (Updated)",
  "phone": "+5566778899",
  "email": "downtown-new@abccorp.com"
}
```

---

## ?? Users

### **Get All Users** (Filtered by company/branch)
```http
GET /api/users
Authorization: Bearer {token}
```

### **Get User by ID**
```http
GET /api/users/{userId}
Authorization: Bearer {token}
```

### **Create User**
```http
POST /api/users
Authorization: Bearer {token}
Content-Type: application/json

{
  "userId": "user123",
  "firstName": "John",
  "lastName": "Doe",
  "password": "password123",
  "email": "john@example.com",
  "phone": "+1234567890",
  "role": "Cashier",
  "companyId": 1,
  "branchId": 5
}
```

---

## ?? Products

### **Get All Products** (Filtered by company/branch)
```http
GET /api/products
Authorization: Bearer {token}
```

### **Get Product by ID**
```http
GET /api/products/{productId}
Authorization: Bearer {token}
```

### **Create Product**
```http
POST /api/products
Authorization: Bearer {token}
Content-Type: application/json

{
  "productName": "Product Name",
  "description": "Product description",
  "price": 99.99,
  "stockQuantity": 100,
  "categoryId": 1
}
```

---

## ?? Returns

### **Create Whole Return**
```http
POST /api/returns/whole
Authorization: Bearer {token}
Content-Type: application/json

{
  "invoiceId": 25,
  "returnReason": "Customer not satisfied",
  "refundMethod": "Cash"
}
```

### **Create Partial Return**
```http
POST /api/returns/partial
Authorization: Bearer {token}
Content-Type: application/json

{
  "invoiceId": 25,
  "returnItems": [
    {
      "productId": 10,
      "quantityReturned": 2
    }
  ],
  "returnReason": "Defective items",
  "refundMethod": "Cash"
}
```

---

## ?? Error Responses

### **401 Unauthorized**
```json
{
  "message": "Unauthorized access"
}
```
**Action**: Token expired or invalid ? Redirect to login

### **403 Forbidden**
```json
{
  "message": "You do not have permission to access this resource"
}
```
**Action**: User lacks required role/permissions

### **404 Not Found**
```json
{
  "message": "Resource not found"
}
```
**Action**: Resource doesn't exist OR user trying to access another branch's data

### **400 Bad Request**
```json
{
  "message": "Invalid input data",
  "errors": {
    "fieldName": ["Error message"]
  }
}
```
**Action**: Validation failed - show error messages to user

---

## ?? Important Headers

All authenticated requests must include:
```
Authorization: Bearer {your-jwt-token}
Content-Type: application/json
```

---

## ?? Key Points

1. **Automatic Filtering**: All `GET` endpoints automatically filter by branch based on JWT token
2. **Automatic Assignment**: All `POST` endpoints automatically assign `CompanyId` and `BranchId` based on JWT token
3. **Ownership Verification**: All `PUT` and `DELETE` endpoints verify branch ownership before allowing modifications
4. **JWT Token Required**: All endpoints (except login) require valid JWT token in `Authorization` header
5. **Date Format**: Use ISO 8601 format: `YYYY-MM-DDTHH:mm:ssZ` or `YYYY-MM-DD`

---

## ?? Testing Examples (using fetch)

### **Login**
```javascript
fetch('https://api.example.com/api/auth/login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    userId: 'user123',
    password: 'password123'
  })
})
.then(res => res.json())
.then(data => {
  localStorage.setItem('token', data.token);
  console.log('Logged in:', data);
});
```

### **Get Orders**
```javascript
const token = localStorage.getItem('token');

fetch('https://api.example.com/api/orders', {
  headers: {
    'Authorization': `Bearer ${token}`
  }
})
.then(res => res.json())
.then(orders => console.log('Orders:', orders));
```

### **Create Order**
```javascript
const token = localStorage.getItem('token');

fetch('https://api.example.com/api/orders', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    customerFullName: 'John Doe',
    customerPhone: '+1234567890',
    paymentMethod: 'Cash',
    items: [
      { productId: 10, quantity: 2, unitPrice: 50.00 }
    ]
  })
})
.then(res => res.json())
.then(order => console.log('Order created:', order));
```

---

## ?? Related Documentation

- `FRONTEND_IMPLEMENTATION_GUIDE.md` - Detailed frontend integration guide
- `MULTI_TENANCY_IMPLEMENTATION.md` - Backend multi-tenancy setup
- `BRANCH_DATA_SEPARATION_ANALYSIS.md` - Security analysis
- `IMPLEMENTATION_COMPLETE.md` - Complete feature list

---

**Quick Tip**: Use browser's Network tab (F12) to inspect actual API requests and responses during development! ??
