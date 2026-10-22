# Ice Cream Shop Backend

Backend API built with Laravel and MySQL for managing an ice cream shop's inventory items and customer records, featuring role-based access control (RBAC) and token authentication.

---

## 1. Features

- **Authentication & Roles:** Uses Laravel Sanctum tokens. Supports three distinct user roles:
  - **Owner:** Full CRUD control over both inventory items and customer records.
  - **Manager:** Can add, view, and edit items and customers, but cannot delete anything.
  - **Cashier:** Can view and edit existing items (no add/delete); can add, view, and edit customers (no delete).
- **Inventory Tracking:** CRUD operations for ice cream products categorized by flavor (such as Chocolate, Vanilla, and Strawberry) with search by name.
- **Customer Records:** CRUD operations to manage customer contact information.
- **Automated Tests:** Feature tests verifying login, endpoint access, and role permissions.

---

## 2. Seed Accounts

Pre-configured users available after seeding:

| Role | Email | Password |
|---|---|---|
| Owner | `owner@icecream.com` | `password123` |
| Manager | `manager@icecream.com` | `password123` |
| Cashier | `cashier@icecream.com` | `password123` |

---

## 3. Installation & Setup Instructions

### Prerequisites
- PHP 8.3 or higher
- Composer
- MySQL (e.g. XAMPP)

### Setup Steps

```bash
git clone <repository-url>
cd icecream-shop
composer install
```

### 3. Environment Setup
Copy the example environment file and configure your database:

```bash
cp .env.example .env
php artisan key:generate
```

Make sure your MySQL database credentials in `.env` match your local setup:
```env
DB_CONNECTION=mysql
DB_HOST=127.0.0.1
DB_PORT=3306
DB_DATABASE=shop_db
DB_USERNAME=root
DB_PASSWORD=
```

### 4. Run Migrations & Seeders
Create the database tables and seed sample data:

```bash
php artisan migrate --seed
```

### 5. Start the Server
```bash
php artisan serve
```
The API is available at `http://127.0.0.1:8000/api`.

---

## 4. Running Tests
Run the test suite using Artisan:

```bash
php artisan test
```

---

## 5. API Endpoints

All protected endpoints require the `Authorization: Bearer <token>` header.

### Authentication
- `POST /api/auth/login` - Authenticate and get Bearer token
- `POST /api/auth/logout` - Invalidate current token
- `GET /api/auth/me` - Get profile of logged-in user

### Inventory Management
- `GET /api/items` - List items (supports `?search=query` and `?category=Flavor`)
- `GET /api/items/{id}` - Get item by ID
- `POST /api/items` - Create item *(Owner, Manager)*
- `PUT/PATCH /api/items/{id}` - Update item *(Owner, Manager, Cashier)*
- `DELETE /api/items/{id}` - Delete item *(Owner only)*

### Customer Management
- `GET /api/customers` - List customers (supports `?search=query`)
- `GET /api/customers/{id}` - Get customer by ID
- `POST /api/customers` - Create customer *(Owner, Manager, Cashier)*
- `PUT/PATCH /api/customers/{id}` - Update customer *(Owner, Manager, Cashier)*
- `DELETE /api/customers/{id}` - Delete customer *(Owner only)*

---
