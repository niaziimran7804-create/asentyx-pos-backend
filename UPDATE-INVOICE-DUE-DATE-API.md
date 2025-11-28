# Update Invoice Due Date API

## Endpoint Added

### PUT `/api/invoices/{id}/due-date`
- **Auth**: Bearer, Roles = `Admin`
- **Body**: `UpdateInvoiceDueDateDto`
- **Response**: Success message or error

---

## Request DTO

```csharp
public class UpdateInvoiceDueDateDto
{
    public DateTime DueDate { get; set; }
}
```

---

## Example Request

### Update Due Date
```http
PUT /api/invoices/30/due-date
Authorization: Bearer YOUR_TOKEN
Content-Type: application/json

{
  "dueDate": "2025-12-31"
}
```

### Success Response (200 OK)
```json
{
  "message": "Invoice due date updated successfully"
}
```

### Error Responses

**404 Not Found** - Invoice doesn't exist
```json
{
  "message": "Invoice not found"
}
```

**400 Bad Request** - Due date in the past
```json
{
  "message": "Due date cannot be in the past"
}
```

**401 Unauthorized** - Not authenticated or not Admin role
```json
{
  "message": "User not authorized"
}
```

---

## Usage Examples

### JavaScript/Fetch
```javascript
async function updateInvoiceDueDate(invoiceId, dueDate) {
  const response = await fetch(`/api/invoices/${invoiceId}/due-date`, {
    method: 'PUT',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({ dueDate })
  });
  
  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.message);
  }
  
  return await response.json();
}

// Usage
try {
  const result = await updateInvoiceDueDate(30, '2025-12-31');
  console.log(result.message); // "Invoice due date updated successfully"
} catch (error) {
  console.error('Failed to update due date:', error.message);
}
```

### cURL
```bash
curl -X PUT "https://localhost:7000/api/invoices/30/due-date" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"dueDate": "2025-12-31"}'
```

### TypeScript Interface
```typescript
interface UpdateInvoiceDueDateDto {
  dueDate: string; // ISO 8601 format: "YYYY-MM-DD"
}

interface UpdateDueDateResponse {
  message: string;
}

async function updateInvoiceDueDate(
  invoiceId: number, 
  dto: UpdateInvoiceDueDateDto
): Promise<UpdateDueDateResponse> {
  const response = await fetch(`/api/invoices/${invoiceId}/due-date`, {
    method: 'PUT',
    headers: {
      'Authorization': `Bearer ${getToken()}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(dto)
  });
  
  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.message);
  }
  
  return response.json();
}
```

---

## Validation Rules

1. **Due Date Required**: Must provide a valid date
2. **Date Format**: Must be valid DateTime (ISO 8601 recommended: `YYYY-MM-DD`)
3. **Future Date**: Due date cannot be in the past (validated server-side)
4. **Admin Only**: Only users with Admin role can update due dates
5. **Invoice Exists**: Invoice must exist in database

---

## Business Logic

- Updates the `DueDate` field of the invoice
- Does NOT affect invoice status or payment calculations
- Validates date is not in the past
- Returns 404 if invoice doesn't exist
- Returns 400 with message if validation fails

---

## Database Changes

The endpoint updates the existing `Invoice` table:
- **Field**: `DueDate` (DateTime)
- **No migration needed** - uses existing schema

---

## Frontend Integration

### Use Cases
1. **Extend Payment Deadline** - Customer requests more time to pay
2. **Correct Data Entry Error** - Fix incorrect due date
3. **Adjust Terms** - Change payment terms after invoice creation

### Example React Component
```tsx
import { useState } from 'react';

function UpdateDueDateForm({ invoiceId, currentDueDate }) {
  const [dueDate, setDueDate] = useState(currentDueDate);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    setError(null);

    try {
      const response = await fetch(`/api/invoices/${invoiceId}/due-date`, {
        method: 'PUT',
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({ dueDate })
      });

      if (!response.ok) {
        const error = await response.json();
        throw new Error(error.message);
      }

      alert('Due date updated successfully!');
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <form onSubmit={handleSubmit}>
      <label>
        New Due Date:
        <input 
          type="date" 
          value={dueDate} 
          onChange={(e) => setDueDate(e.target.value)}
          min={new Date().toISOString().split('T')[0]}
          required
        />
      </label>
      {error && <div className="error">{error}</div>}
      <button type="submit" disabled={loading}>
        {loading ? 'Updating...' : 'Update Due Date'}
      </button>
    </form>
  );
}
```

---

## Testing

### Test Cases

1. **Valid Update**
   - Provide future date
   - Expect: 200 OK with success message

2. **Past Date**
   - Provide date before today
   - Expect: 400 Bad Request with error message

3. **Invoice Not Found**
   - Use non-existent invoice ID
   - Expect: 404 Not Found

4. **Unauthorized**
   - Call without token or with non-Admin role
   - Expect: 401 Unauthorized

5. **Invalid Date Format**
   - Provide malformed date string
   - Expect: 400 Bad Request

---

## Notes

- ? Admin role required
- ? Server-side validation prevents past dates
- ? Does not trigger accounting entries or ledger updates
- ? Can be called multiple times for same invoice
- ? Hot reload requires app restart (interface method added)

---

**Status**: ? Implemented  
**Build**: ? Successful (requires app restart)  
**Tested**: ? Pending  
**Documented**: ? Complete
