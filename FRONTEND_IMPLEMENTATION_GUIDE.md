# ?? Frontend Implementation Guide - Multi-Tenancy & Branch-Wise Data Separation

## ?? Table of Contents
1. [Overview](#overview)
2. [Authentication & Context](#authentication--context)
3. [API Integration](#api-integration)
4. [Component Examples](#component-examples)
5. [State Management](#state-management)
6. [Routing & Guards](#routing--guards)
7. [UI/UX Guidelines](#uiux-guidelines)
8. [Testing](#testing)

---

## ?? Overview

This guide provides complete frontend implementation instructions for integrating with the multi-tenant POS backend that has 100% branch-wise data separation.

### **Key Features**
- ? JWT-based authentication with tenant context
- ? Automatic branch filtering (handled by backend)
- ? Role-based access control (Admin, Manager, Cashier, Customer)
- ? Branch/Company selector for admins
- ? Real-time data isolation between branches

### **Backend API URL**
```typescript
const API_BASE_URL = 'https://your-api-url.com/api';
```

---

## ?? Authentication & Context

### **1. Auth Service (TypeScript/Angular)**

```typescript
// services/auth.service.ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';
import { tap } from 'rxjs/operators';

export interface LoginRequest {
  userId: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  user: {
    id: number;
    userId: string;
    firstName: string;
    lastName: string;
    email?: string;
    role: string;
    companyId?: number;
    branchId?: number;
  };
}

export interface CurrentUser {
  userId: string;
  role: string;
  companyId?: number;
  branchId?: number;
  isBranchUser: boolean;
  isCompanyAdmin: boolean;
  isSuperAdmin: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly API_URL = 'https://your-api-url.com/api';
  private currentUserSubject = new BehaviorSubject<CurrentUser | null>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(private http: HttpClient) {
    this.loadUserFromStorage();
  }

  /**
   * Login user and store tenant context
   */
  login(credentials: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.API_URL}/auth/login`, credentials)
      .pipe(
        tap(response => {
          // Store token
          localStorage.setItem('access_token', response.token);
          
          // Store user context (extract from response.user)
          const user: CurrentUser = {
            userId: response.user.userId,
            role: response.user.role,
            companyId: response.user.companyId,
            branchId: response.user.branchId,
            isBranchUser: !!response.user.branchId,
            isCompanyAdmin: !!response.user.companyId && !response.user.branchId,
            isSuperAdmin: !response.user.companyId && !response.user.branchId
          };
          
          localStorage.setItem('current_user', JSON.stringify(user));
          this.currentUserSubject.next(user);
        })
      );
  }

  /**
   * Logout user and clear context
   */
  logout(): void {
    localStorage.removeItem('access_token');
    localStorage.removeItem('current_user');
    this.currentUserSubject.next(null);
  }

  /**
   * Get current user context
   */
  getCurrentUser(): CurrentUser | null {
    return this.currentUserSubject.value;
  }

  /**
   * Check if user is authenticated
   */
  isAuthenticated(): boolean {
    return !!localStorage.getItem('access_token');
  }

  /**
   * Get JWT token for API requests
   */
  getToken(): string | null {
    return localStorage.getItem('access_token');
  }

  /**
   * Check if user has specific role
   */
  hasRole(role: string): boolean {
    const user = this.getCurrentUser();
    return user?.role === role;
  }

  /**
   * Check if user can access branch data
   */
  canAccessBranch(branchId: number): boolean {
    const user = this.getCurrentUser();
    
    // Super admin can access all branches
    if (user?.isSuperAdmin) return true;
    
    // Company admin can access all branches in their company
    if (user?.isCompanyAdmin) return true;
    
    // Branch user can only access their assigned branch
    return user?.branchId === branchId;
  }

  /**
   * Load user from localStorage on app init
   */
  private loadUserFromStorage(): void {
    const userStr = localStorage.getItem('current_user');
    if (userStr) {
      const user = JSON.parse(userStr) as CurrentUser;
      this.currentUserSubject.next(user);
    }
  }
}
```

---

### **2. HTTP Interceptor (Automatic Token Injection)**

```typescript
// interceptors/auth.interceptor.ts
import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuthService } from '../services/auth.service';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  constructor(private authService: AuthService) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    const token = this.authService.getToken();
    
    if (token) {
      // Clone request and add Authorization header
      const cloned = req.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      });
      return next.handle(cloned);
    }
    
    return next.handle(req);
  }
}
```

**Register in app.module.ts**:
```typescript
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { AuthInterceptor } from './interceptors/auth.interceptor';

@NgModule({
  providers: [
    {
      provide: HTTP_INTERCEPTORS,
      useClass: AuthInterceptor,
      multi: true
    }
  ]
})
export class AppModule { }
```

---

### **3. React/Next.js Auth Hook**

```typescript
// hooks/useAuth.ts
import { useState, useEffect, createContext, useContext } from 'react';
import axios from 'axios';

interface LoginRequest {
  userId: string;
  password: string;
}

interface CurrentUser {
  userId: string;
  role: string;
  companyId?: number;
  companyName?: string;
  branchId?: number;
  branchName?: string;
  isBranchUser: boolean;
  isCompanyAdmin: boolean;
  isSuperAdmin: boolean;
}

interface AuthContextType {
  user: CurrentUser | null;
  login: (credentials: LoginRequest) => Promise<void>;
  logout: () => void;
  isAuthenticated: boolean;
  hasRole: (role: string) => boolean;
  canAccessBranch: (branchId: number) => boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<CurrentUser | null>(null);

  useEffect(() => {
    // Load user from localStorage
    const userStr = localStorage.getItem('current_user');
    if (userStr) {
      setUser(JSON.parse(userStr));
    }
  }, []);

  const login = async (credentials: LoginRequest) => {
    const response = await axios.post('/api/auth/login', credentials);
    const { token, user } = response.data;

    // Store token
    localStorage.setItem('access_token', token);

    // Create user context from response.user
    const currentUser: CurrentUser = {
      userId: user.userId,
      role: user.role,
      companyId: user.companyId,
      branchId: user.branchId,
      isBranchUser: !!user.branchId,
      isCompanyAdmin: !!user.companyId && !user.branchId,
      isSuperAdmin: !user.companyId && !user.branchId
    };

    localStorage.setItem('current_user', JSON.stringify(currentUser));
    setUser(currentUser);
  };

  const logout = () => {
    localStorage.removeItem('access_token');
    localStorage.removeItem('current_user');
    setUser(null);
  };

  const hasRole = (role: string): boolean => {
    return user?.role === role;
  };

  const canAccessBranch = (branchId: number): boolean => {
    if (user?.isSuperAdmin) return true;
    if (user?.isCompanyAdmin) return true;
    return user?.branchId === branchId;
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        login,
        logout,
        isAuthenticated: !!user,
        hasRole,
        canAccessBranch
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider');
  }
  return context;
};
```

---

## ?? API Integration

### **1. Base API Service**

```typescript
// services/api.service.ts
import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private readonly BASE_URL = 'https://your-api-url.com/api';

  constructor(private http: HttpClient) {}

  /**
   * Generic GET request
   */
  get<T>(endpoint: string, params?: any): Observable<T> {
    let httpParams = new HttpParams();
    if (params) {
      Object.keys(params).forEach(key => {
        if (params[key] !== null && params[key] !== undefined) {
          httpParams = httpParams.set(key, params[key].toString());
        }
      });
    }
    return this.http.get<T>(`${this.BASE_URL}/${endpoint}`, { params: httpParams });
  }

  /**
   * Generic POST request
   */
  post<T>(endpoint: string, body: any): Observable<T> {
    return this.http.post<T>(`${this.BASE_URL}/${endpoint}`, body);
  }

  /**
   * Generic PUT request
   */
  put<T>(endpoint: string, body: any): Observable<T> {
    return this.http.put<T>(`${this.BASE_URL}/${endpoint}`, body);
  }

  /**
   * Generic DELETE request
   */
  delete<T>(endpoint: string): Observable<T> {
    return this.http.delete<T>(`${this.BASE_URL}/${endpoint}`);
  }
}
```

---

### **2. Orders Service Example**

```typescript
// services/orders.service.ts
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';

export interface Order {
  orderId: number;
  customerId: number;
  customerName: string;
  date: Date;
  status: string;
  totalAmount: number;
  orderStatus: string;
  paymentMethod: string;
  items: OrderItem[];
  invoiceId?: number;
}

export interface OrderItem {
  productId: number;
  productName: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
}

export interface CreateOrderRequest {
  customerFullName: string;
  customerPhone?: string;
  customerEmail?: string;
  customerAddress?: string;
  paymentMethod: string;
  items: {
    productId: number;
    quantity: number;
    unitPrice: number;
  }[];
}

@Injectable({
  providedIn: 'root'
})
export class OrdersService {
  constructor(private api: ApiService) {}

  /**
   * Get all orders (automatically filtered by branch in backend)
   */
  getAllOrders(): Observable<Order[]> {
    return this.api.get<Order[]>('orders');
  }

  /**
   * Get order by ID
   */
  getOrderById(orderId: number): Observable<Order> {
    return this.api.get<Order>(`orders/${orderId}`);
  }

  /**
   * Create new order (automatically assigned to user's branch)
   */
  createOrder(order: CreateOrderRequest): Observable<Order> {
    return this.api.post<Order>('orders', order);
  }

  /**
   * Update order status
   */
  updateOrderStatus(orderId: number, status: string, orderStatus: string): Observable<boolean> {
    return this.api.put<boolean>(`orders/${orderId}/status`, { status, orderStatus });
  }

  /**
   * Bulk update order statuses
   */
  bulkUpdateOrderStatus(orderIds: number[], status: string, orderStatus: string): Observable<{ updatedCount: number }> {
    return this.api.put<{ updatedCount: number }>('orders/bulk-status', { orderIds, status, orderStatus });
  }

  /**
   * Delete order
   */
  deleteOrder(orderId: number): Observable<boolean> {
    return this.api.delete<boolean>(`orders/${orderId}`);
  }

  /**
   * Search customers
   */
  searchCustomers(searchTerm: string): Observable<any[]> {
    return this.api.get<any[]>('orders/search-customers', { searchTerm });
  }
}
```

---

### **3. Invoices Service Example**

```typescript
// services/invoices.service.ts
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';

export interface Invoice {
  invoiceId: number;
  invoiceNumber: string;
  orderId: number;
  customerId: number;
  customerName: string;
  invoiceDate: Date;
  dueDate: Date;
  totalAmount: number;
  paidAmount: number;
  balance: number;
  status: string;
  items: InvoiceItem[];
}

export interface InvoiceItem {
  productId: number;
  productName: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
}

@Injectable({
  providedIn: 'root'
})
export class InvoicesService {
  constructor(private api: ApiService) {}

  /**
   * Get all invoices (automatically filtered by branch)
   */
  getAllInvoices(): Observable<Invoice[]> {
    return this.api.get<Invoice[]>('invoices');
  }

  /**
   * Get filtered invoices
   */
  getFilteredInvoices(filters: {
    minAmount?: number;
    maxAmount?: number;
    startDate?: string;
    endDate?: string;
    customerAddress?: string;
    status?: string;
  }): Observable<Invoice[]> {
    return this.api.get<Invoice[]>('invoices', filters);
  }

  /**
   * Get invoice by ID
   */
  getInvoiceById(invoiceId: number): Observable<Invoice> {
    return this.api.get<Invoice>(`invoices/${invoiceId}`);
  }

  /**
   * Get invoice by order ID
   */
  getInvoiceByOrderId(orderId: number): Observable<Invoice> {
    return this.api.get<Invoice>(`invoices/order/${orderId}`);
  }

  /**
   * Print invoice (opens in new window)
   */
  printInvoice(invoiceId: number): void {
    window.open(`${this.api['BASE_URL']}/invoices/${invoiceId}/print`, '_blank');
  }

  /**
   * Bulk print invoices
   */
  bulkPrintInvoices(invoiceIds: number[]): void {
    const idsParam = invoiceIds.join(',');
    window.open(`${this.api['BASE_URL']}/invoices/bulk-print?invoiceIds=${idsParam}`, '_blank');
  }

  /**
   * Add payment to invoice
   */
  addPayment(invoiceId: number, payment: { amount: number; paymentMethod: string; referenceNumber?: string }): Observable<any> {
    return this.api.post(`invoices/${invoiceId}/payments`, payment);
  }

  /**
   * Get invoice payments
   */
  getInvoicePayments(invoiceId: number): Observable<any> {
    return this.api.get(`invoices/${invoiceId}/payments`);
  }
}
```

---

### **4. Expenses Service Example**

```typescript
// services/expenses.service.ts
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';

export interface Expense {
  expenseId: number;
  expenseName: string;
  expenseAmount: number;
  expenseDate: Date;
}

export interface CreateExpenseRequest {
  expenseName: string;
  expenseAmount: number;
  expenseDate: Date;
}

@Injectable({
  providedIn: 'root'
})
export class ExpensesService {
  constructor(private api: ApiService) {}

  /**
   * Get all expenses (automatically filtered by branch)
   */
  getAllExpenses(): Observable<Expense[]> {
    return this.api.get<Expense[]>('expenses');
  }

  /**
   * Get expense by ID (only if user has access to that branch)
   */
  getExpenseById(expenseId: number): Observable<Expense> {
    return this.api.get<Expense>(`expenses/${expenseId}`);
  }

  /**
   * Create expense (automatically assigned to user's branch)
   */
  createExpense(expense: CreateExpenseRequest): Observable<Expense> {
    return this.api.post<Expense>('expenses', expense);
  }

  /**
   * Update expense (only if user has access to that branch)
   */
  updateExpense(expenseId: number, expense: CreateExpenseRequest): Observable<boolean> {
    return this.api.put<boolean>(`expenses/${expenseId}`, expense);
  }

  /**
   * Delete expense (only if user has access to that branch)
   */
  deleteExpense(expenseId: number): Observable<boolean> {
    return this.api.delete<boolean>(`expenses/${expenseId}`);
  }
}
```

---

### **5. Accounting/Reports Service Example**

```typescript
// services/accounting.service.ts
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';

export interface FinancialSummary {
  totalIncome: number;
  totalExpenses: number;
  totalRefunds: number;
  netProfit: number;
  totalSales: number;
  totalPurchases: number;
  cashBalance: number;
  period: string;
}

export interface DailySales {
  date: string;
  totalSales: number;
  totalOrders: number;
  totalExpenses: number;
  totalRefunds: number;
  netProfit: number;
  cashSales: number;
  cardSales: number;
  averageOrderValue: number;
}

export interface SalesGraph {
  labels: string[];
  salesData: number[];
  expensesData: number[];
  refundsData: number[];
  profitData: number[];
  ordersData: number[];
}

@Injectable({
  providedIn: 'root'
})
export class AccountingService {
  constructor(private api: ApiService) {}

  /**
   * Get financial summary (branch-specific)
   */
  getFinancialSummary(startDate?: string, endDate?: string): Observable<FinancialSummary> {
    return this.api.get<FinancialSummary>('accounting/summary', { startDate, endDate });
  }

  /**
   * Get daily sales data (branch-specific)
   */
  getDailySales(days: number = 7): Observable<DailySales[]> {
    return this.api.get<DailySales[]>('accounting/daily-sales', { days });
  }

  /**
   * Get sales graph data (branch-specific)
   */
  getSalesGraph(startDate: string, endDate: string): Observable<SalesGraph> {
    return this.api.get<SalesGraph>('accounting/sales-graph', { startDate, endDate });
  }

  /**
   * Get payment methods summary (branch-specific)
   */
  getPaymentMethodsSummary(startDate?: string, endDate?: string): Observable<any[]> {
    return this.api.get<any[]>('accounting/payment-methods', { startDate, endDate });
  }

  /**
   * Get top products (branch-specific)
   */
  getTopProducts(limit: number = 10, startDate?: string, endDate?: string): Observable<any[]> {
    return this.api.get<any[]>('accounting/top-products', { limit, startDate, endDate });
  }
}
```

---

### **6. Customer Ledger Service Example**

```typescript
// services/ledger.service.ts
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';

export interface CustomerAging {
  customerId: number;
  customerName: string;
  customerPhone: string;
  customerEmail: string;
  currentBalance: number;
  days0To30: number;
  days31To60: number;
  days61To90: number;
  days91Plus: number;
  totalOutstanding: number;
}

export interface AgingReport {
  reportDate: Date;
  asOfDate: Date;
  customers: CustomerAging[];
  totalDays0To30: number;
  totalDays31To60: number;
  totalDays61To90: number;
  totalDays91Plus: number;
  grandTotal: number;
  totalCustomers: number;
  customersWithBalance: number;
}

@Injectable({
  providedIn: 'root'
})
export class LedgerService {
  constructor(private api: ApiService) {}

  /**
   * Get customer ledger (branch-specific)
   */
  getCustomerLedger(customerId: number, startDate?: string, endDate?: string): Observable<any[]> {
    return this.api.get<any[]>(`ledger/customer/${customerId}`, { startDate, endDate });
  }

  /**
   * Get customer balance (branch-specific)
   */
  getCustomerBalance(customerId: number): Observable<{ customerId: number; currentBalance: number; asOfDate: Date }> {
    return this.api.get(`ledger/customer/${customerId}/balance`);
  }

  /**
   * Get customer statement (branch-specific)
   */
  getCustomerStatement(customerId: number, startDate: string, endDate: string): Observable<any> {
    return this.api.get(`ledger/customer/${customerId}/statement`, { startDate, endDate });
  }

  /**
   * Get aging report (branch-specific)
   */
  getAgingReport(asOfDate?: string): Observable<AgingReport> {
    return this.api.get<AgingReport>('ledger/aging-report', { asOfDate });
  }

  /**
   * Get customer aging (branch-specific)
   */
  getCustomerAging(customerId: number, asOfDate?: string): Observable<CustomerAging> {
    return this.api.get<CustomerAging>(`ledger/customer/${customerId}/aging`, { asOfDate });
  }

  /**
   * Record payment
   */
  recordPayment(payment: {
    customerId: number;
    amount: number;
    paymentMethod: string;
    referenceNumber?: string;
    invoiceId?: number;
  }): Observable<any> {
    return this.api.post('ledger/payment', payment);
  }
}
```

---

## ?? Component Examples

### **1. Login Component (Angular)**

```typescript
// components/login/login.component.ts
import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent {
  credentials = {
    userId: '',
    password: ''
  };
  errorMessage = '';
  loading = false;

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  onSubmit(): void {
    this.loading = true;
    this.errorMessage = '';

    this.authService.login(this.credentials).subscribe({
      next: (response) => {
        console.log('Login successful', response);
        
        // Redirect based on role
        if (response.role === 'Admin') {
          this.router.navigate(['/dashboard']);
        } else if (response.role === 'Manager') {
          this.router.navigate(['/orders']);
        } else {
          this.router.navigate(['/pos']);
        }
      },
      error: (error) => {
        this.errorMessage = error.error?.message || 'Invalid credentials';
        this.loading = false;
      }
    });
  }
}
```

```html
<!-- login.component.html -->
<div class="login-container">
  <div class="login-card">
    <h2>POS System Login</h2>
    
    <form (ngSubmit)="onSubmit()" #loginForm="ngForm">
      <div class="form-group">
        <label for="userId">User ID</label>
        <input
          type="text"
          id="userId"
          name="userId"
          [(ngModel)]="credentials.userId"
          required
          class="form-control"
          placeholder="Enter your user ID"
        />
      </div>

      <div class="form-group">
        <label for="password">Password</label>
        <input
          type="password"
          id="password"
          name="password"
          [(ngModel)]="credentials.password"
          required
          class="form-control"
          placeholder="Enter your password"
        />
      </div>

      <div *ngIf="errorMessage" class="alert alert-danger">
        {{ errorMessage }}
      </div>

      <button
        type="submit"
        class="btn btn-primary btn-block"
        [disabled]="!loginForm.valid || loading"
      >
        <span *ngIf="loading">Logging in...</span>
        <span *ngIf="!loading">Login</span>
      </button>
    </form>
  </div>
</div>
```

---

### **2. Dashboard Component with Branch Context**

```typescript
// components/dashboard/dashboard.component.ts
import { Component, OnInit } from '@angular/core';
import { AuthService, CurrentUser } from '../../services/auth.service';
import { AccountingService, FinancialSummary } from '../../services/accounting.service';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html'
})
export class DashboardComponent implements OnInit {
  currentUser: CurrentUser | null = null;
  financialSummary: FinancialSummary | null = null;
  loading = true;

  constructor(
    private authService: AuthService,
    private accountingService: AccountingService
  ) {}

  ngOnInit(): void {
    // Get current user context
    this.currentUser = this.authService.getCurrentUser();
    
    // Load branch-specific financial data
    this.loadFinancialSummary();
  }

  loadFinancialSummary(): void {
    this.accountingService.getFinancialSummary().subscribe({
      next: (summary) => {
        this.financialSummary = summary;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading financial summary', error);
        this.loading = false;
      }
    });
  }

  getBranchDisplayName(): string {
    if (this.currentUser?.branchName) {
      return `Branch: ${this.currentUser.branchName}`;
    } else if (this.currentUser?.companyName) {
      return `Company: ${this.currentUser.companyName} (All Branches)`;
    } else {
      return 'All Companies & Branches';
    }
  }
}
```

```html
<!-- dashboard.component.html -->
<div class="dashboard">
  <!-- Header with Branch Context -->
  <div class="dashboard-header">
    <h1>Dashboard</h1>
    <div class="branch-context">
      <i class="fa fa-building"></i>
      {{ getBranchDisplayName() }}
    </div>
  </div>

  <!-- Financial Summary Cards -->
  <div class="summary-cards" *ngIf="!loading && financialSummary">
    <div class="card">
      <div class="card-icon bg-success">
        <i class="fa fa-dollar-sign"></i>
      </div>
      <div class="card-content">
        <h3>{{ financialSummary.totalIncome | currency }}</h3>
        <p>Total Income</p>
      </div>
    </div>

    <div class="card">
      <div class="card-icon bg-danger">
        <i class="fa fa-money-bill-wave"></i>
      </div>
      <div class="card-content">
        <h3>{{ financialSummary.totalExpenses | currency }}</h3>
        <p>Total Expenses</p>
      </div>
    </div>

    <div class="card">
      <div class="card-icon bg-primary">
        <i class="fa fa-chart-line"></i>
      </div>
      <div class="card-content">
        <h3>{{ financialSummary.netProfit | currency }}</h3>
        <p>Net Profit</p>
      </div>
    </div>

    <div class="card">
      <div class="card-icon bg-info">
        <i class="fa fa-shopping-cart"></i>
      </div>
      <div class="card-content">
        <h3>{{ financialSummary.totalSales | currency }}</h3>
        <p>Total Sales</p>
      </div>
    </div>
  </div>

  <!-- Loading State -->
  <div *ngIf="loading" class="loading">
    <div class="spinner"></div>
    <p>Loading dashboard data...</p>
  </div>
</div>
```

---

### **3. Orders List Component**

```typescript
// components/orders/orders-list.component.ts
import { Component, OnInit } from '@angular/core';
import { OrdersService, Order } from '../../services/orders.service';
import { AuthService, CurrentUser } from '../../services/auth.service';

@Component({
  selector: 'app-orders-list',
  templateUrl: './orders-list.component.html'
})
export class OrdersListComponent implements OnInit {
  orders: Order[] = [];
  filteredOrders: Order[] = [];
  currentUser: CurrentUser | null = null;
  loading = true;
  searchTerm = '';
  statusFilter = 'All';

  constructor(
    private ordersService: OrdersService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.currentUser = this.authService.getCurrentUser();
    this.loadOrders();
  }

  loadOrders(): void {
    // Backend automatically filters by branch
    this.ordersService.getAllOrders().subscribe({
      next: (orders) => {
        this.orders = orders;
        this.filteredOrders = orders;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading orders', error);
        this.loading = false;
      }
    });
  }

  filterOrders(): void {
    this.filteredOrders = this.orders.filter(order => {
      const matchesSearch = 
        order.customerName.toLowerCase().includes(this.searchTerm.toLowerCase()) ||
        order.orderId.toString().includes(this.searchTerm);
      
      const matchesStatus = 
        this.statusFilter === 'All' || 
        order.status === this.statusFilter;

      return matchesSearch && matchesStatus;
    });
  }

  printInvoice(orderId: number): void {
    // This will automatically use the order's invoice if it exists
    this.ordersService.getOrderById(orderId).subscribe({
      next: (order) => {
        if (order.invoiceId) {
          window.open(`/api/invoices/${order.invoiceId}/print`, '_blank');
        } else {
          alert('Invoice not found for this order');
        }
      },
      error: (error) => console.error('Error fetching order', error)
    });
  }

  getBranchInfo(): string {
    return this.currentUser?.branchName || 'All Branches';
  }
}
```

```html
<!-- orders-list.component.html -->
<div class="orders-container">
  <div class="orders-header">
    <h2>Orders - {{ getBranchInfo() }}</h2>
    <button class="btn btn-primary" routerLink="/orders/new">
      <i class="fa fa-plus"></i> New Order
    </button>
  </div>

  <!-- Filters -->
  <div class="filters">
    <input
      type="text"
      [(ngModel)]="searchTerm"
      (input)="filterOrders()"
      placeholder="Search by customer name or order ID..."
      class="form-control search-input"
    />
    
    <select [(ngModel)]="statusFilter" (change)="filterOrders()" class="form-control status-filter">
      <option value="All">All Status</option>
      <option value="Pending">Pending</option>
      <option value="Paid">Paid</option>
      <option value="Cancelled">Cancelled</option>
    </select>
  </div>

  <!-- Orders Table -->
  <div class="table-responsive">
    <table class="table table-striped" *ngIf="!loading && filteredOrders.length > 0">
      <thead>
        <tr>
          <th>Order ID</th>
          <th>Customer</th>
          <th>Date</th>
          <th>Total Amount</th>
          <th>Status</th>
          <th>Payment Method</th>
          <th>Actions</th>
        </tr>
      </thead>
      <tbody>
        <tr *ngFor="let order of filteredOrders">
          <td>#{{ order.orderId }}</td>
          <td>{{ order.customerName }}</td>
          <td>{{ order.date | date:'short' }}</td>
          <td>{{ order.totalAmount | currency }}</td>
          <td>
            <span class="badge" [ngClass]="{
              'badge-success': order.status === 'Paid',
              'badge-warning': order.status === 'Pending',
              'badge-danger': order.status === 'Cancelled'
            }">
              {{ order.status }}
            </span>
          </td>
          <td>{{ order.paymentMethod }}</td>
          <td>
            <button class="btn btn-sm btn-info" [routerLink]="['/orders', order.orderId]">
              <i class="fa fa-eye"></i> View
            </button>
            <button class="btn btn-sm btn-secondary" (click)="printInvoice(order.orderId)">
              <i class="fa fa-print"></i> Print
            </button>
          </td>
        </tr>
      </tbody>
    </table>

    <div *ngIf="!loading && filteredOrders.length === 0" class="no-data">
      <p>No orders found</p>
    </div>

    <div *ngIf="loading" class="loading">
      <div class="spinner"></div>
      <p>Loading orders...</p>
    </div>
  </div>
</div>
```

---

### **4. Aging Report Component**

```typescript
// components/reports/aging-report.component.ts
import { Component, OnInit } from '@angular/core';
import { LedgerService, AgingReport } from '../../services/ledger.service';
import { AuthService, CurrentUser } from '../../services/auth.service';

@Component({
  selector: 'app-aging-report',
  templateUrl: './aging-report.component.html'
})
export class AgingReportComponent implements OnInit {
  agingReport: AgingReport | null = null;
  currentUser: CurrentUser | null = null;
  loading = true;
  asOfDate: string = new Date().toISOString().split('T')[0];

  constructor(
    private ledgerService: LedgerService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.currentUser = this.authService.getCurrentUser();
    this.loadAgingReport();
  }

  loadAgingReport(): void {
    this.loading = true;
    // Backend automatically filters by branch
    this.ledgerService.getAgingReport(this.asOfDate).subscribe({
      next: (report) => {
        this.agingReport = report;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading aging report', error);
        this.loading = false;
      }
    });
  }

  exportToCSV(): void {
    if (!this.agingReport) return;

    const headers = ['Customer', 'Phone', '0-30 Days', '31-60 Days', '61-90 Days', '91+ Days', 'Total Outstanding'];
    const rows = this.agingReport.customers.map(c => [
      c.customerName,
      c.customerPhone,
      c.days0To30.toFixed(2),
      c.days31To60.toFixed(2),
      c.days61To90.toFixed(2),
      c.days91Plus.toFixed(2),
      c.totalOutstanding.toFixed(2)
    ]);

    const csv = [headers, ...rows].map(row => row.join(',')).join('\n');
    const blob = new Blob([csv], { type: 'text/csv' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `aging-report-${this.asOfDate}.csv`;
    a.click();
  }

  getBranchInfo(): string {
    return this.currentUser?.branchName || 'All Branches';
  }
}
```

```html
<!-- aging-report.component.html -->
<div class="aging-report-container">
  <div class="report-header">
    <h2>Customer Account Balance - Aging Report</h2>
    <div class="report-info">
      <span class="branch-badge">{{ getBranchInfo() }}</span>
      <span class="date-badge">As of: {{ asOfDate | date:'mediumDate' }}</span>
    </div>
  </div>

  <div class="report-filters">
    <div class="form-group">
      <label for="asOfDate">As of Date:</label>
      <input
        type="date"
        id="asOfDate"
        [(ngModel)]="asOfDate"
        (change)="loadAgingReport()"
        class="form-control"
      />
    </div>
    <button class="btn btn-success" (click)="exportToCSV()" [disabled]="!agingReport">
      <i class="fa fa-download"></i> Export to CSV
    </button>
  </div>

  <div class="table-responsive" *ngIf="!loading && agingReport">
    <table class="table table-bordered aging-table">
      <thead>
        <tr>
          <th>Customer</th>
          <th>Phone</th>
          <th class="text-right">0-30 Days</th>
          <th class="text-right">31-60 Days</th>
          <th class="text-right">61-90 Days</th>
          <th class="text-right">91+ Days</th>
          <th class="text-right">Account Balance</th>
        </tr>
      </thead>
      <tbody>
        <tr *ngFor="let customer of agingReport.customers">
          <td>{{ customer.customerName }}</td>
          <td>{{ customer.customerPhone }}</td>
          <td class="text-right">{{ customer.days0To30 | currency }}</td>
          <td class="text-right">{{ customer.days31To60 | currency }}</td>
          <td class="text-right">{{ customer.days61To90 | currency }}</td>
          <td class="text-right">{{ customer.days91Plus | currency }}</td>
          <td class="text-right font-weight-bold">{{ customer.totalOutstanding | currency }}</td>
        </tr>
      </tbody>
      <tfoot>
        <tr class="table-totals">
          <td colspan="2" class="text-right font-weight-bold">TOTAL:</td>
          <td class="text-right font-weight-bold">{{ agingReport.totalDays0To30 | currency }}</td>
          <td class="text-right font-weight-bold">{{ agingReport.totalDays31To60 | currency }}</td>
          <td class="text-right font-weight-bold">{{ agingReport.totalDays61To90 | currency }}</td>
          <td class="text-right font-weight-bold">{{ agingReport.totalDays91Plus | currency }}</td>
          <td class="text-right font-weight-bold">{{ agingReport.grandTotal | currency }}</td>
        </tr>
      </tfoot>
    </table>

    <div class="report-summary">
      <p><strong>Total Customers:</strong> {{ agingReport.totalCustomers }}</p>
      <p><strong>Customers with Balance:</strong> {{ agingReport.customersWithBalance }}</p>
      <p><strong>Grand Total Outstanding:</strong> {{ agingReport.grandTotal | currency }}</p>
    </div>
  </div>

  <div *ngIf="loading" class="loading">
    <div class="spinner"></div>
    <p>Loading aging report...</p>
  </div>
</div>
```

---

## ?? Routing & Guards

### **Auth Guard**

```typescript
// guards/auth.guard.ts
import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Injectable({
  providedIn: 'root'
})
export class AuthGuard implements CanActivate {
  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): boolean {
    if (this.authService.isAuthenticated()) {
      // Check if route requires specific role
      const requiredRole = route.data['role'];
      if (requiredRole) {
        const user = this.authService.getCurrentUser();
        if (user?.role !== requiredRole) {
          this.router.navigate(['/unauthorized']);
          return false;
        }
      }
      return true;
    }

    // Not authenticated, redirect to login
    this.router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
    return false;
  }
}
```

### **Route Configuration**

```typescript
// app-routing.module.ts
import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from './guards/auth.guard';

const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { 
    path: 'dashboard', 
    component: DashboardComponent, 
    canActivate: [AuthGuard] 
  },
  { 
    path: 'orders', 
    component: OrdersListComponent, 
    canActivate: [AuthGuard] 
  },
  { 
    path: 'orders/new', 
    component: CreateOrderComponent, 
    canActivate: [AuthGuard] 
  },
  { 
    path: 'orders/:id', 
    component: OrderDetailsComponent, 
    canActivate: [AuthGuard] 
  },
  { 
    path: 'expenses', 
    component: ExpensesListComponent, 
    canActivate: [AuthGuard] 
  },
  { 
    path: 'reports/aging', 
    component: AgingReportComponent, 
    canActivate: [AuthGuard],
    data: { role: 'Admin' } // Only admins can access
  },
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  { path: '**', redirectTo: '/dashboard' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
```

---

## ?? UI/UX Guidelines

### **1. Branch Context Display**

Always show the current branch context to users:

```html
<!-- navbar.component.html -->
<nav class="navbar">
  <div class="navbar-brand">POS System</div>
  <div class="navbar-context">
    <i class="fa fa-building"></i>
    <span *ngIf="currentUser?.branchName">{{ currentUser.branchName }}</span>
    <span *ngIf="currentUser?.isCompanyAdmin">{{ currentUser.companyName }} (All Branches)</span>
    <span *ngIf="currentUser?.isSuperAdmin">Super Admin (All Companies)</span>
  </div>
  <div class="navbar-user">
    <span>{{ currentUser?.userId }}</span>
    <button (click)="logout()">Logout</button>
  </div>
</nav>
```

---

### **2. Data Scope Indicators**

Add visual indicators showing data scope:

```html
<div class="data-scope-badge">
  <i class="fa fa-filter"></i>
  Showing data for: <strong>{{ getBranchName() }}</strong>
</div>
```

---

### **3. Error Handling**

Handle 401/403 errors gracefully:

```typescript
// interceptors/error.interceptor.ts
import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Injectable()
export class ErrorInterceptor implements HttpInterceptor {
  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    return next.handle(req).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.status === 401) {
          // Unauthorized - token expired or invalid
          this.authService.logout();
          this.router.navigate(['/login']);
        } else if (error.status === 403) {
          // Forbidden - user doesn't have access to this resource
          alert('You do not have permission to access this resource');
        } else if (error.status === 404) {
          // Not found - or user trying to access data from another branch
          console.warn('Resource not found or not accessible');
        }
        
        return throwError(() => error);
      })
    );
  }
}
```

---

## ?? Testing

### **Unit Test Example**

```typescript
// orders.service.spec.ts
import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { OrdersService } from './orders.service';

describe('OrdersService', () => {
  let service: OrdersService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [OrdersService]
    });
    service = TestBed.inject(OrdersService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should fetch orders filtered by branch', () => {
    const mockOrders = [
      { orderId: 1, customerName: 'John Doe', totalAmount: 100 },
      { orderId: 2, customerName: 'Jane Smith', totalAmount: 200 }
    ];

    service.getAllOrders().subscribe(orders => {
      expect(orders.length).toBe(2);
      expect(orders).toEqual(mockOrders);
    });

    const req = httpMock.expectOne('/api/orders');
    expect(req.request.method).toBe('GET');
    expect(req.request.headers.has('Authorization')).toBe(true);
    req.flush(mockOrders);
  });
});
```

---

## ?? Important Notes

### **Backend Handles All Filtering** ?
- You don't need to manually filter data by branch in the frontend
- The backend automatically filters based on the JWT token
- Just call the API endpoints normally

### **JWT Token is Everything** ??
- Always include the JWT token in API requests (use interceptor)
- Token contains: `CompanyId`, `BranchId`, and `Role`
- Backend reads token and filters data automatically

### **No Branch Selection Needed** ??
- Branch users see only their branch data (automatic)
- Company admins see all branches in their company (automatic)
- Super admins see all data (automatic)

### **Error Handling** ??
- 401 = Token expired ? Redirect to login
- 403 = No permission ? Show error message
- 404 = Not found OR trying to access other branch's data

---

## ?? Additional Resources

### **API Endpoints Summary**

| Endpoint | Method | Description | Auto-Filtered |
|----------|--------|-------------|---------------|
| `/api/auth/login` | POST | User login | N/A |
| `/api/orders` | GET | Get all orders | ? Yes |
| `/api/orders/{id}` | GET | Get order by ID | ? Yes |
| `/api/orders` | POST | Create order | ? Auto-assigned |
| `/api/invoices` | GET | Get all invoices | ? Yes |
| `/api/expenses` | GET | Get all expenses | ? Yes |
| `/api/accounting/summary` | GET | Financial summary | ? Yes |
| `/api/ledger/aging-report` | GET | Aging report | ? Yes |

### **Sample .env File**

```
API_BASE_URL=https://your-api-url.com/api
ENABLE_DEBUG_MODE=false
```

---

## ? Implementation Checklist

- [ ] Set up authentication service with JWT
- [ ] Configure HTTP interceptor for automatic token injection
- [ ] Create auth guard for protected routes
- [ ] Display branch context in UI
- [ ] Implement error handling for 401/403
- [ ] Test with different user roles (Admin, Manager, Cashier)
- [ ] Verify branch-specific data filtering
- [ ] Add loading states for API calls
- [ ] Implement proper error messages
- [ ] Test logout functionality

---

**?? You're all set! The backend handles 100% of the branch-wise data separation automatically. Just focus on building a great user interface!** ??

**Questions?** Refer to:
- `MULTI_TENANCY_IMPLEMENTATION.md`
- `BRANCH_DATA_SEPARATION_ANALYSIS.md`
- `IMPLEMENTATION_COMPLETE.md`
