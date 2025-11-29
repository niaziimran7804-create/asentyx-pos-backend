# ?? Quick Reference: Invoice Payment ? Order Status Sync

## ?? What It Does

When you add a payment to an invoice:
- Invoice status updates (Pending ? PartiallyPaid ? Paid)
- **Order status automatically syncs** with invoice status

## ?? Key Behavior

| Payment Action | Result |
|---------------|---------|
| **First partial payment** | Invoice & Order ? `PartiallyPaid` |
| **Additional partial payment** | Invoice & Order ? stays `PartiallyPaid` |
| **Final payment (balance = 0)** | Invoice & Order ? `Paid` |

## ?? API Usage

### Add Payment

```http
POST /api/invoices/123/payments
Content-Type: application/json
Authorization: Bearer {token}

{
  "amount": 500.00,
  "paymentMethod": "Cash",
  "paymentDate": "2024-01-15T10:00:00Z",
  "notes": "Partial payment",
  "transactionReference": "TXN-12345"
}
```

### What Happens Automatically

1. ? Payment record created
2. ? Invoice amounts updated
3. ? Invoice status calculated
4. ? **Order status synced** ? NEW!
5. ? Accounting entry created

## ?? Verify It Worked

```http
# Check invoice
GET /api/invoices/123
? "status": "PartiallyPaid"

# Check order (should match!)
GET /api/orders/456
? "status": "PartiallyPaid"
? "orderStatus": "PartiallyPaid"
```

## ?? Important Notes

- ? **Always** use the payment API—don't manually update order status
- ? Order status updates automatically—no extra API call needed
- ? Both `Status` and `OrderStatus` fields are updated
- ? Works with partial and full payments

## ?? Debugging

### Check if sync happened:

```csharp
// Look for this in debug logs:
"Updated Order 456 status to 'PartiallyPaid' based on invoice payment"
```

### SQL to verify:

```sql
SELECT 
    o.OrderId,
    o.Status AS OrderStatus,
    i.InvoiceId,
    i.Status AS InvoiceStatus,
    i.AmountPaid,
    i.Balance
FROM Orders o
INNER JOIN Invoices i ON o.OrderId = i.OrderId
WHERE o.OrderId = 456;
```

Expected: Order status = Invoice status

## ?? Status Flow

```
Order Created
    ?
Status: Pending
    ?
Payment #1 ($500 / $1500)
    ?
Status: PartiallyPaid ?
    ?
Payment #2 ($500 / $1500)
    ?
Status: PartiallyPaid ?
    ?
Payment #3 ($500 / $1500)
    ?
Status: Paid ?
```

## ?? Related Endpoints

| Endpoint | Purpose |
|----------|---------|
| `POST /api/invoices/{id}/payments` | Add payment (triggers sync) |
| `GET /api/invoices/{id}/payments` | View payment history |
| `GET /api/invoices/{id}` | View invoice status |
| `GET /api/orders/{id}` | View order status |

## ? Testing Checklist

- [ ] Add partial payment ? Order becomes "PartiallyPaid"
- [ ] Add second payment ? Order stays "PartiallyPaid"
- [ ] Add final payment ? Order becomes "Paid"
- [ ] Try overpayment ? Error (validation works)
- [ ] Try payment on paid invoice ? Error (validation works)

## ?? Best Practices

### ? Do This:
- Use payment API for all invoice payments
- Let the system handle status updates
- Verify status after payment in UI

### ? Don't Do This:
- Manually update order status when receiving payment
- Update invoice status directly when order is paid
- Bypass the payment API

## ?? Code Example (C#)

```csharp
// Adding payment automatically syncs order status
var payment = await _invoiceService.AddPaymentAsync(
    invoiceId: 123,
    paymentDto: new CreateInvoicePaymentDto
    {
        Amount = 500.00m,
        PaymentMethod = "Cash",
        PaymentDate = DateTime.UtcNow
    },
    receivedBy: "John Doe"
);

// No need to manually update order!
// Order status is already synced
```

## ?? Frontend Example (TypeScript)

```typescript
// Add payment
this.invoiceService.addPayment(invoiceId, payment).subscribe({
  next: (response) => {
    this.toastr.success('Payment recorded');
    this.toastr.info('Order status updated automatically');
    
    // Refresh both invoice and order views
    this.refreshInvoice();
    this.refreshOrder();
  }
});
```

## ?? Support

- Check debug logs for sync confirmation
- Verify database: order status should match invoice status
- See full documentation: `ORDER_STATUS_SYNC_WITH_INVOICE_PAYMENTS.md`

---

**Last Updated:** January 29, 2025  
**Feature Status:** ? Active  
**Build Status:** ? Passing
