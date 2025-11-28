# ?? Company & Branch Management API Guide

## ?? Base Information

**Base URL**: `https://your-api-url.com/api`  
**Authentication**: JWT Bearer Token (except Company Registration)  
**Content-Type**: `application/json`

---

## ?? COMPANIES API

### **1. Get All Companies**

Returns list of all companies in the system.

```http
GET /api/companies
Authorization: Bearer {token}
```

**Access**: Super Admin only

**Response 200 OK:**
```json
[
  {
    "companyId": 1,
    "companyName": "ABC Corporation",
    "email": "info@abc.com",
    "phone": "+1234567890",
    "address": "123 Main Street",
    "city": "New York",
    "country": "USA",
    "postalCode": "10001",
    "taxNumber": "TAX-12345",
    "registrationNumber": "REG-67890",
    "isActive": true,
    "createdDate": "2024-01-15T00:00:00Z",
    "subscriptionEndDate": "2025-01-15T00:00:00Z",
    "subscriptionPlan": "Premium",
    "totalBranches": 5,
    "totalUsers": 25
  }
]
```

---

### **2. Get Company by ID**

Returns details of a specific company.

```http
GET /api/companies/{id}
Authorization: Bearer {token}
```

**Parameters:**
- `id` (path, required) - Company ID

**Access**: Any authenticated user

**Response 200 OK:**
```json
{
  "companyId": 1,
  "companyName": "ABC Corporation",
  "email": "info@abc.com",
  "phone": "+1234567890",
  "address": "123 Main Street",
  "city": "New York",
  "country": "USA",
  "postalCode": "10001",
  "taxNumber": "TAX-12345",
  "registrationNumber": "REG-67890",
  "isActive": true,
  "createdDate": "2024-01-15T00:00:00Z",
  "subscriptionEndDate": "2025-01-15T00:00:00Z",
  "subscriptionPlan": "Premium",
  "totalBranches": 5,
  "totalUsers": 25
}
```

**Response 404 Not Found:**
```json
{
  "message": "Company not found"
}
```

---

### **3. Create Company (Self-Registration)**

Register a new company. This is a **public endpoint** - no authentication required.

**?? Important:** This endpoint automatically creates:
1. Company record
2. Head Office branch
3. Admin user with CompanyAdmin role
4. All in a single transaction

```http
POST /api/companies
Content-Type: application/json
```

**Access**: Public (No authentication required)

**Request Body:**
```json
{
  "companyName": "ABC Corporation",
  "email": "info@abc.com",
  "phone": "+1234567890",
  "address": "123 Main Street",
  "city": "New York",
  "country": "USA",
  "postalCode": "10001",
  "taxNumber": "TAX-12345",
  "registrationNumber": "REG-67890",
  "subscriptionPlan": "Basic",
  "subscriptionEndDate": "2025-01-15T00:00:00Z",
  "adminUserId": "admin123",
  "adminFirstName": "John",
  "adminLastName": "Doe",
  "adminPassword": "SecurePassword123!",
  "adminEmail": "john@abc.com",
  "adminPhone": "+1234567890"
}
```

**Required Fields:**
- `companyName`
- `email`
- `subscriptionPlan` (e.g., "Basic", "Standard", "Premium")
- `adminUserId`
- `adminFirstName`
- `adminLastName`
- `adminPassword`
- `adminEmail`

**Response 201 Created:**
```json
{
  "companyId": 1,
  "companyName": "ABC Corporation",
  "email": "info@abc.com",
  "phone": "+1234567890",
  "address": "123 Main Street",
  "city": "New York",
  "country": "USA",
  "postalCode": "10001",
  "taxNumber": "TAX-12345",
  "registrationNumber": "REG-67890",
  "isActive": true,
  "createdDate": "2024-11-26T10:00:00Z",
  "subscriptionEndDate": "2025-01-15T00:00:00Z",
  "subscriptionPlan": "Basic",
  "totalBranches": 1,
  "totalUsers": 1
}
```

**Response 400 Bad Request:**
```json
{
  "message": "User with ID 'admin123' already exists"
}
```

---

### **4. Update Company**

Update company details.

```http
PUT /api/companies/{id}
Authorization: Bearer {token}
Content-Type: application/json
```

**Parameters:**
- `id` (path, required) - Company ID

**Access**: Super Admin or Company Admin (own company only)

**Request Body:**
```json
{
  "companyName": "ABC Corporation (Updated)",
  "email": "newemail@abc.com",
  "phone": "+0987654321",
  "address": "456 New Street",
  "city": "Los Angeles",
  "country": "USA",
  "postalCode": "90001",
  "taxNumber": "TAX-54321",
  "registrationNumber": "REG-09876",
  "isActive": true,
  "subscriptionPlan": "Premium",
  "subscriptionEndDate": "2026-01-15T00:00:00Z"
}
```

**Required Fields:**
- `companyName`
- `email`
- `isActive`
- `subscriptionPlan`

**Response 204 No Content**

**Response 404 Not Found:**
```json
{
  "message": "Company not found"
}
```

---

### **5. Delete Company**

Soft delete a company (sets `isActive = false`).

```http
DELETE /api/companies/{id}
Authorization: Bearer {token}
```

**Parameters:**
- `id` (path, required) - Company ID

**Access**: Super Admin only

**Response 204 No Content**

**Response 404 Not Found**

---

## ?? BRANCHES API

### **1. Get All Branches**

Returns list of all branches. Company Admins only see their company's branches.

```http
GET /api/branches
Authorization: Bearer {token}
```

**Access**: Super Admin, Company Admin

**Response 200 OK:**
```json
[
  {
    "branchId": 1,
    "companyId": 1,
    "companyName": "ABC Corporation",
    "branchName": "Head Office",
    "branchCode": "HO",
    "email": "ho@abc.com",
    "phone": "+1234567890",
    "address": "123 Main Street",
    "city": "New York",
    "country": "USA",
    "postalCode": "10001",
    "isActive": true,
    "isHeadOffice": true,
    "createdDate": "2024-01-15T00:00:00Z",
    "totalUsers": 10,
    "totalProducts": 500
  },
  {
    "branchId": 2,
    "companyId": 1,
    "companyName": "ABC Corporation",
    "branchName": "Downtown Branch",
    "branchCode": "DT01",
    "email": "downtown@abc.com",
    "phone": "+1234567891",
    "address": "789 Downtown Ave",
    "city": "New York",
    "country": "USA",
    "postalCode": "10002",
    "isActive": true,
    "isHeadOffice": false,
    "createdDate": "2024-02-01T00:00:00Z",
    "totalUsers": 5,
    "totalProducts": 300
  }
]
```

---

### **2. Get Branches by Company**

Returns all branches for a specific company.

```http
GET /api/branches/company/{companyId}
Authorization: Bearer {token}
```

**Parameters:**
- `companyId` (path, required) - Company ID

**Access**: Any authenticated user

**Response 200 OK:**
```json
[
  {
    "branchId": 1,
    "companyId": 1,
    "companyName": "ABC Corporation",
    "branchName": "Head Office",
    "branchCode": "HO",
    "email": "ho@abc.com",
    "phone": "+1234567890",
    "address": "123 Main Street",
    "city": "New York",
    "country": "USA",
    "postalCode": "10001",
    "isActive": true,
    "isHeadOffice": true,
    "createdDate": "2024-01-15T00:00:00Z",
    "totalUsers": 10,
    "totalProducts": 500
  }
]
```

---

### **3. Get Branch by ID**

Returns details of a specific branch.

```http
GET /api/branches/{id}
Authorization: Bearer {token}
```

**Parameters:**
- `id` (path, required) - Branch ID

**Access**: Any authenticated user

**Response 200 OK:**
```json
{
  "branchId": 1,
  "companyId": 1,
  "companyName": "ABC Corporation",
  "branchName": "Head Office",
  "branchCode": "HO",
  "email": "ho@abc.com",
  "phone": "+1234567890",
  "address": "123 Main Street",
  "city": "New York",
  "country": "USA",
  "postalCode": "10001",
  "isActive": true,
  "isHeadOffice": true,
  "createdDate": "2024-01-15T00:00:00Z",
  "totalUsers": 10,
  "totalProducts": 500
}
```

**Response 404 Not Found**

---

### **4. Create Branch**

Create a new branch under a company.

```http
POST /api/branches
Authorization: Bearer {token}
Content-Type: application/json
```

**Access**: Super Admin, Company Admin (own company only)

**Request Body:**
```json
{
  "companyId": 1,
  "branchName": "Downtown Branch",
  "branchCode": "DT01",
  "email": "downtown@abc.com",
  "phone": "+1234567891",
  "address": "789 Downtown Ave",
  "city": "New York",
  "country": "USA",
  "postalCode": "10002",
  "isHeadOffice": false
}
```

**Required Fields:**
- `companyId`
- `branchName`
- `branchCode`

**Response 201 Created:**
```json
{
  "branchId": 2,
  "companyId": 1,
  "companyName": "ABC Corporation",
  "branchName": "Downtown Branch",
  "branchCode": "DT01",
  "email": "downtown@abc.com",
  "phone": "+1234567891",
  "address": "789 Downtown Ave",
  "city": "New York",
  "country": "USA",
  "postalCode": "10002",
  "isActive": true,
  "isHeadOffice": false,
  "createdDate": "2024-11-26T10:00:00Z",
  "totalUsers": 0,
  "totalProducts": 0
}
```

**Response 400 Bad Request:**
```json
{
  "message": "Company not found"
}
```

---

### **5. Update Branch**

Update branch details.

```http
PUT /api/branches/{id}
Authorization: Bearer {token}
Content-Type: application/json
```

**Parameters:**
- `id` (path, required) - Branch ID

**Access**: Super Admin, Company Admin (own company only)

**Request Body:**
```json
{
  "branchName": "Downtown Branch (Updated)",
  "branchCode": "DT01",
  "email": "downtown-new@abc.com",
  "phone": "+1234567891",
  "address": "789 Downtown Ave (Updated)",
  "city": "New York",
  "country": "USA",
  "postalCode": "10002",
  "isActive": true,
  "isHeadOffice": false
}
```

**Required Fields:**
- `branchName`
- `branchCode`
- `isActive`
- `isHeadOffice`

**Response 204 No Content**

**Response 404 Not Found**

---

### **6. Delete Branch**

Soft delete a branch (sets `isActive = false`).

```http
DELETE /api/branches/{id}
Authorization: Bearer {token}
```

**Parameters:**
- `id` (path, required) - Branch ID

**Access**: Super Admin, Company Admin (own company only)

**Response 204 No Content**

**Response 404 Not Found**

---

## ?? Access Control Matrix

| Role | Get All Companies | View Company | Create Company | Update Company | Delete Company |
|------|-------------------|--------------|----------------|----------------|----------------|
| **Public** | ? | ? | ? | ? | ? |
| **Branch User** | ? | ? | ? | ? | ? |
| **Company Admin** | ? | ? | ? | ? Own | ? |
| **Super Admin** | ? | ? | ? | ? | ? |

| Role | Get All Branches | Get Branches by Company | View Branch | Create Branch | Update Branch | Delete Branch |
|------|------------------|------------------------|-------------|---------------|---------------|---------------|
| **Branch User** | ? | ? | ? Assigned | ? | ? | ? |
| **Company Admin** | ? Own | ? Own | ? Own | ? Own | ? Own | ? Own |
| **Super Admin** | ? All | ? All | ? All | ? All | ? All | ? All |

---

## ?? Typical Workflows

### **Workflow 1: Company Registration**

```
1. POST /api/companies (no auth)
   ??> Creates: Company + Head Office + Admin User

2. Admin logs in
   ??> POST /api/auth/login
       ??> Receives JWT token

3. Admin can now manage branches
   ??> GET /api/branches/company/{companyId}
   ??> POST /api/branches
```

### **Workflow 2: Add New Branch**

```
1. Company Admin authenticated
   ??> Has JWT token with CompanyId

2. Get existing branches
   ??> GET /api/branches/company/{companyId}

3. Create new branch
   ??> POST /api/branches
       Body: { companyId, branchName, branchCode, ... }

4. Assign users to branch
   ??> POST /api/users
       Body: { ..., companyId, branchId }
```

### **Workflow 3: View Branch Details**

```
1. Get all branches in company
   ??> GET /api/branches/company/{companyId}

2. Select specific branch
   ??> GET /api/branches/{branchId}

3. View branch users and products
   ??> Response includes:
       - totalUsers
       - totalProducts
```

---

## ?? Authentication Headers

### **For Public Endpoints**
```http
Content-Type: application/json
```

### **For Protected Endpoints**
```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json
```

---

## ?? Important Notes

### **Company Registration**
- ? Public endpoint (no authentication)
- ? Automatically creates Head Office branch
- ? Automatically creates Admin user
- ? Transaction ensures all-or-nothing
- ?? `adminUserId` must be unique
- ?? `adminPassword` minimum 8 characters

### **Branch Management**
- ? Company Admins can only manage their company's branches
- ? Super Admins can manage all branches
- ? Soft delete (sets `isActive = false`)
- ?? Avoid deleting Head Office branches
- ?? `branchCode` should be unique within company

### **Subscription Plans**
Available values: `"Basic"`, `"Standard"`, `"Premium"`

### **User Roles**
- `"Admin"` - Super Admin (system-wide)
- `"CompanyAdmin"` - Company Administrator
- `"Manager"` - Branch Manager
- `"Cashier"` - POS Cashier

---

## ?? HTTP Status Codes

| Code | Status | Meaning |
|------|--------|---------|
| 200 | OK | Request successful |
| 201 | Created | Resource created successfully |
| 204 | No Content | Update/Delete successful |
| 400 | Bad Request | Invalid request data |
| 401 | Unauthorized | Missing or invalid token |
| 403 | Forbidden | Insufficient permissions |
| 404 | Not Found | Resource not found |

---

## ?? Testing Examples

### **Example 1: Register Company with cURL**

```bash
curl -X POST https://your-api-url.com/api/companies \
  -H "Content-Type: application/json" \
  -d '{
    "companyName": "Test Company",
    "email": "test@company.com",
    "subscriptionPlan": "Basic",
    "adminUserId": "testadmin",
    "adminFirstName": "Test",
    "adminLastName": "Admin",
    "adminPassword": "Password123!",
    "adminEmail": "admin@company.com"
  }'
```

### **Example 2: Get Company's Branches with cURL**

```bash
curl -X GET https://your-api-url.com/api/branches/company/1 \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### **Example 3: Create Branch with cURL**

```bash
curl -X POST https://your-api-url.com/api/branches \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "companyId": 1,
    "branchName": "New Branch",
    "branchCode": "NB01",
    "email": "newbranch@company.com",
    "city": "New York"
  }'
```

---

## ?? Related Endpoints

After creating company and branches, you may need:

- **User Management**: `/api/users` - Create users and assign to branches
- **Product Management**: `/api/products` - Add products to branches
- **Orders**: `/api/orders` - Branch-specific order management
- **Reports**: `/api/accounting/*` - Branch-specific financial reports

---

**API Version**: 1.0  
**Last Updated**: November 2024  
**Status**: ? Production Ready
