# Customer Creation Logic - Order Service

## ? Implementation Summary

The `OrderService.CreateOrderAsync` method now properly handles customer creation and association with comprehensive validation and logging.

---

## ?? Customer Creation Flow

### **Step-by-Step Process:**

1. **Try to find existing customer by phone** (most reliable identifier)
   ```csharp
   if (!string.IsNullOrWhiteSpace(createOrderDto.CustomerPhone))
   {
       customer = await _context.Users
           .FirstOrDefaultAsync(u => u.Role == "Customer" && u.Phone == createOrderDto.CustomerPhone);
   }
   ```

2. **If not found, try to find by email**
   ```csharp
   if (customer == null && !string.IsNullOrWhiteSpace(createOrderDto.CustomerEmail))
   {
       customer = await _context.Users
           .FirstOrDefaultAsync(u => u.Role == "Customer" && u.Email == createOrderDto.CustomerEmail);
   }
   ```

3. **If still not found, create new customer**
   ```csharp
   if (customer == null)
   {
       // Validate name is provided
       if (string.IsNullOrWhiteSpace(createOrderDto.CustomerFullName))
       {
           throw new InvalidOperationException("Customer full name is required");
       }
       
       // Split name into first and last
       var nameParts = createOrderDto.CustomerFullName.Trim().Split(' ', 2);
       
       // Create new customer with Role="Customer"
       customer = new User {
           UserId = $"CUST_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid()}",
           FirstName = nameParts[0],
           LastName = nameParts.Length > 1 ? nameParts[1] : "",
           Phone = createOrderDto.CustomerPhone,
           Email = createOrderDto.CustomerEmail,
           CurrentCity = createOrderDto.CustomerAddress,
           Role = "Customer",
           // ... other defaults
       };
       
       _context.Users.Add(customer);
       await _context.SaveChangesAsync();
   }
   ```

4. **Use customer ID for order**
   ```csharp
   customerId = customer.Id;
   ```

---

## ? Enhanced Features

### **1. Robust Validation**
- ? Validates customer name is provided
- ? Validates customer ID is valid after creation/lookup
- ? Throws clear error messages

### **2. Comprehensive Logging**
```csharp
// When found by phone
System.Diagnostics.Debug.WriteLine($"Found existing customer by phone: {customer.Id}");

// When found by email  
System.Diagnostics.Debug.WriteLine($"Found existing customer by email: {customer.Id}");

// When creating new
System.Diagnostics.Debug.WriteLine($"No existing customer found. Creating new customer for: {name}");
System.Diagnostics.Debug.WriteLine($"Created new customer: {customer.Id} (Phone: {phone}, Email: {email})");

// Order created
System.Diagnostics.Debug.WriteLine($"Created order {orderId} for customer {customerId} with total {amount}");
```

### **3. Smart Matching**
- **Priority 1:** Match by phone number (most reliable)
- **Priority 2:** Match by email address
- **Priority 3:** Create new customer

### **4. Data Quality**
- ? Trims whitespace from names
- ? Handles single-word names
- ? Generates unique customer IDs
- ? Sets proper defaults for non-login customers

---

## ?? Test Scenarios

### **Scenario 1: New Customer (No Phone/Email in System)**

**Request:**
```json
POST /api/orders
{
  "customerFullName": "John Doe",
  "customerPhone": "+1234567890",
  "customerEmail": "john@example.com",
  "customerAddress": "123 Main St",
  "items": [{"productId": 1, "quantity": 2, "unitPrice": 50}]
}
```

**Result:**
- ? New customer created in Users table
- ? Customer has Role="Customer"
- ? Order created with CustomerId
- ? Order linked to new customer

**Log Output:**
```
No existing customer found. Creating new customer for: John Doe
Created new customer: 15 - John Doe (Phone: +1234567890, Email: john@example.com)
Created order 45 for customer 15 with total amount 100.00
```

---

### **Scenario 2: Existing Customer (Phone Match)**

**Existing Customer:**
```sql
SELECT * FROM Users WHERE Phone = '+1234567890' AND Role = 'Customer'
-- Returns: Id=15, FirstName=John, LastName=Doe
```

**Request:**
```json
POST /api/orders
{
  "customerFullName": "John Smith",  // Different name
  "customerPhone": "+1234567890",    // Same phone
  "customerEmail": "different@example.com",
  "items": [{"productId": 2, "quantity": 1, "unitPrice": 75}]
}
```

**Result:**
- ? Existing customer found by phone
- ? No new customer created
- ? Order created with existing CustomerId (15)
- ? Customer name NOT updated (preserves original)

**Log Output:**
```
Found existing customer by phone: 15 - John Doe
Created order 46 for customer 15 with total amount 75.00
```

---

### **Scenario 3: Existing Customer (Email Match)**

**Existing Customer:**
```sql
SELECT * FROM Users WHERE Email = 'jane@example.com' AND Role = 'Customer'
-- Returns: Id=20, FirstName=Jane, LastName=Smith
```

**Request:**
```json
POST /api/orders
{
  "customerFullName": "Jane Doe",
  "customerPhone": "+9876543210",     // Different phone
  "customerEmail": "jane@example.com", // Same email
  "items": [{"productId": 3, "quantity": 3, "unitPrice": 25}]
}
```

**Result:**
- ? Existing customer found by email
- ? No new customer created
- ? Order created with existing CustomerId (20)

**Log Output:**
```
Found existing customer by email: 20 - Jane Smith
Created order 47 for customer 20 with total amount 75.00
```

---

### **Scenario 4: Customer with Only Name**

**Request:**
```json
POST /api/orders
{
  "customerFullName": "Guest Customer",
  "customerPhone": null,
  "customerEmail": null,
  "items": [{"productId": 1, "quantity": 1, "unitPrice": 100}]
}
```

**Result:**
- ? New customer created (no phone/email to match)
- ? Customer created with only name
- ? Order created successfully

**Log Output:**
```
No existing customer found. Creating new customer for: Guest Customer
Created new customer: 21 - Guest Customer (Phone: , Email: )
Created order 48 for customer 21 with total amount 100.00
```

---

### **Scenario 5: Missing Customer Name (Error)**

**Request:**
```json
POST /api/orders
{
  "customerFullName": "",
  "customerPhone": "+1111111111",
  "items": [{"productId": 1, "quantity": 1, "unitPrice": 50}]
}
```

**Result:**
- ? Error: "Customer full name is required to create a new order"
- ? No customer created
- ? No order created

---

## ?? Database Queries

### **Find Customer by Phone**
```sql
SELECT * FROM Users 
WHERE Role = 'Customer' 
AND Phone = '+1234567890'
```

### **Find Customer by Email**
```sql
SELECT * FROM Users 
WHERE Role = 'Customer' 
AND Email = 'john@example.com'
```

### **Create New Customer**
```sql
INSERT INTO Users (UserId, FirstName, LastName, Password, Email, Phone, CurrentCity, Role, Salary, Age, JoinDate, Birthdate)
VALUES ('CUST_20251126155500_abc123', 'John', 'Doe', '', 'john@example.com', '+1234567890', '123 Main St', 'Customer', 0, 0, GETUTCDATE(), GETUTCDATE())
```

### **Create Order with Customer**
```sql
INSERT INTO Orders (CustomerId, Date, TotalAmount, OrderStatus, PaymentMethod)
VALUES (15, GETUTCDATE(), 100.00, 'Pending', 'Cash')
```

---

## ? Benefits

### **1. No Duplicate Customers**
- Phone number is primary identifier
- Email is secondary identifier
- Only creates if both don't match

### **2. Data Integrity**
- All orders have valid customer references
- Foreign key constraints enforced
- Customer history preserved

### **3. Flexibility**
- Works with phone only
- Works with email only
- Works with both
- Works with just name (creates new)

### **4. Traceability**
- Comprehensive logging
- Debug output for troubleshooting
- Clear error messages

---

## ?? Testing Checklist

- [ ] **Create order with new customer (phone + email)**
  - [ ] Verify customer created in Users table
  - [ ] Verify customer has Role="Customer"
  - [ ] Verify order references customer

- [ ] **Create order with existing phone**
  - [ ] Verify no duplicate customer created
  - [ ] Verify order uses existing customer ID

- [ ] **Create order with existing email**
  - [ ] Verify no duplicate customer created
  - [ ] Verify order uses existing customer ID

- [ ] **Create order with only name**
  - [ ] Verify customer created without phone/email
  - [ ] Verify order created successfully

- [ ] **Create order without name**
  - [ ] Verify error thrown
  - [ ] Verify no customer/order created

- [ ] **Create multiple orders for same phone**
  - [ ] Verify all orders link to same customer
  - [ ] Verify customer order count increases

---

## ?? API Documentation

### **POST /api/orders**

**Request Body:**
```json
{
  "customerFullName": "string (required)",
  "customerPhone": "string (optional)",
  "customerEmail": "string (optional)",
  "customerAddress": "string (optional)",
  "paymentMethod": "string (default: Cash)",
  "items": [
    {
      "productId": "number (required)",
      "quantity": "number (required)",
      "unitPrice": "number (required)"
    }
  ]
}
```

**Response (201 Created):**
```json
{
  "orderId": 45,
  "customerId": 15,
  "customerName": "John Doe",
  "customerPhone": "+1234567890",
  "customerEmail": "john@example.com",
  "customerAddress": "123 Main St",
  "totalAmount": 100.00,
  "items": [
    {
      "productId": 1,
      "productName": "Product A",
      "quantity": 2,
      "unitPrice": 50.00,
      "totalPrice": 100.00
    }
  ],
  "invoiceId": 30
}
```

---

## ?? Troubleshooting

### **Issue: Duplicate customers being created**

**Check:**
1. Phone numbers match exactly (including country code)
2. Email addresses match exactly (case-insensitive)
3. Search is querying `Role = 'Customer'` correctly

**Debug:**
```csharp
var existingByPhone = await _context.Users
    .Where(u => u.Role == "Customer" && u.Phone == phone)
    .ToListAsync();
Console.WriteLine($"Found {existingByPhone.Count} customers with phone {phone}");
```

### **Issue: Customer not found when should exist**

**Check:**
1. Customer has `Role = "Customer"` in database
2. Phone/Email format matches exactly
3. No extra whitespace in phone/email

**Fix:**
```sql
-- Update existing users to Customer role
UPDATE Users 
SET Role = 'Customer' 
WHERE Role IS NULL 
AND Id IN (SELECT DISTINCT CustomerId FROM Orders)
```

---

**Status:** ? **IMPLEMENTED**  
**Validation:** ? **COMPLETE**  
**Logging:** ? **COMPREHENSIVE**  
**Testing:** ? **READY FOR TESTING**

?? **Customer creation logic is fully implemented and ready for use!** ??
