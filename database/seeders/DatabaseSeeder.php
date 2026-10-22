<?php

namespace Database\Seeders;

use App\Models\Customer;
use App\Models\Item;
use App\Models\User;
use Illuminate\Database\Seeder;
use Illuminate\Support\Facades\Hash;

class DatabaseSeeder extends Seeder
{
    public function run(): void
    {
        // 1. Initial Users for each Role
        User::updateOrCreate(
            ['email' => 'owner@icecream.com'],
            [
                'name' => 'Shop Owner',
                'password' => Hash::make('password123'),
                'role' => 'owner',
            ]
        );

        User::updateOrCreate(
            ['email' => 'manager@icecream.com'],
            [
                'name' => 'Shop Manager',
                'password' => Hash::make('password123'),
                'role' => 'manager',
            ]
        );

        User::updateOrCreate(
            ['email' => 'cashier@icecream.com'],
            [
                'name' => 'Shop Cashier',
                'password' => Hash::make('password123'),
                'role' => 'cashier',
            ]
        );

        // 2. Initial Inventory Items
        $items = [
            [
                'name' => 'Dark Chocolate Fudge Scoop',
                'category' => 'Chocolate',
                'price' => 3.50,
                'quantity' => 120,
                'description' => 'Rich Belgian dark chocolate scoop with hot fudge swirl.',
            ],
            [
                'name' => 'Classic Vanilla Bean Pint',
                'category' => 'Vanilla',
                'price' => 5.00,
                'quantity' => 80,
                'description' => 'Creamy Madagascar vanilla bean infused artisan pint.',
            ],
            [
                'name' => 'Fresh Strawberry Delight',
                'category' => 'Strawberry',
                'price' => 4.25,
                'quantity' => 95,
                'description' => 'Fresh garden strawberry puree mixed with sweet cream.',
            ],
            [
                'name' => 'Chocolate Chip Cookie Dough',
                'category' => 'Chocolate',
                'price' => 4.75,
                'quantity' => 60,
                'description' => 'Chewy chunks of chocolate chip cookie dough.',
            ],
            [
                'name' => 'French Vanilla Custard Tub',
                'category' => 'Vanilla',
                'price' => 6.50,
                'quantity' => 40,
                'description' => 'Thick and silky French vanilla custard tub.',
            ],
            [
                'name' => 'Wild Strawberry Sorbet',
                'category' => 'Strawberry',
                'price' => 3.75,
                'quantity' => 75,
                'description' => 'Dairy-free refreshing wild strawberry sorbet.',
            ],
        ];

        foreach ($items as $item) {
            Item::updateOrCreate(['name' => $item['name']], $item);
        }

        // 3. Initial Customer Records
        $customers = [
            [
                'name' => 'Alice Johnson',
                'email' => 'alice@example.com',
                'phone' => '+1-555-0101',
                'address' => '124 Maple Street, Springfield',
            ],
            [
                'name' => 'Bob Smith',
                'email' => 'bob@example.com',
                'phone' => '+1-555-0102',
                'address' => '456 Oak Avenue, Springfield',
            ],
            [
                'name' => 'Charlie Davis',
                'email' => 'charlie@example.com',
                'phone' => '+1-555-0103',
                'address' => '789 Pine Road, Springfield',
            ],
        ];

        foreach ($customers as $customer) {
            Customer::updateOrCreate(['email' => $customer['email']], $customer);
        }
    }
}
