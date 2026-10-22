<?php

namespace Tests\Feature;

use App\Models\Customer;
use App\Models\User;
use Illuminate\Foundation\Testing\RefreshDatabase;
use Laravel\Sanctum\Sanctum;
use Tests\TestCase;

class CustomerManagementTest extends TestCase
{
    use RefreshDatabase;

    public function test_owner_can_perform_all_crud_operations_on_customers(): void
    {
        $owner = User::factory()->create(['role' => 'owner']);
        Sanctum::actingAs($owner);

        // 1. Create customer
        $createResponse = $this->postJson('/api/customers', [
            'name' => 'John Doe',
            'email' => 'john@example.com',
            'phone' => '+1234567890',
            'address' => '100 Main St',
        ]);

        $createResponse->assertStatus(201)
            ->assertJsonPath('data.name', 'John Doe');

        $customerId = $createResponse->json('data.id');

        // 2. View customer
        $viewResponse = $this->getJson("/api/customers/{$customerId}");
        $viewResponse->assertStatus(200)
            ->assertJsonPath('data.email', 'john@example.com');

        // 3. Update customer
        $updateResponse = $this->putJson("/api/customers/{$customerId}", [
            'name' => 'Johnathan Doe',
            'phone' => '+1987654321',
        ]);
        $updateResponse->assertStatus(200)
            ->assertJsonPath('data.name', 'Johnathan Doe');

        // 4. Delete customer
        $deleteResponse = $this->deleteJson("/api/customers/{$customerId}");
        $deleteResponse->assertStatus(200);

        $this->assertDatabaseMissing('customers', ['id' => $customerId]);
    }

    public function test_manager_can_create_view_and_edit_customers_but_cannot_delete(): void
    {
        $manager = User::factory()->create(['role' => 'manager']);
        Sanctum::actingAs($manager);

        // Create customer (allowed)
        $createResponse = $this->postJson('/api/customers', [
            'name' => 'Jane Smith',
            'email' => 'jane@example.com',
            'phone' => '+1234567899',
        ]);
        $createResponse->assertStatus(201);
        $customerId = $createResponse->json('data.id');

        // Edit customer (allowed)
        $updateResponse = $this->patchJson("/api/customers/{$customerId}", [
            'address' => '200 Elm St',
        ]);
        $updateResponse->assertStatus(200);

        // Delete customer (forbidden)
        $deleteResponse = $this->deleteJson("/api/customers/{$customerId}");
        $deleteResponse->assertStatus(403);

        $this->assertDatabaseHas('customers', ['id' => $customerId]);
    }

    public function test_cashier_can_create_view_and_edit_customers_but_cannot_delete(): void
    {
        $customer = Customer::create([
            'name' => 'Sam Wilson',
            'email' => 'sam@example.com',
            'phone' => '+1122334455',
        ]);

        $cashier = User::factory()->create(['role' => 'cashier']);
        Sanctum::actingAs($cashier);

        // Create customer (allowed)
        $createResponse = $this->postJson('/api/customers', [
            'name' => 'Peter Parker',
            'email' => 'peter@example.com',
        ]);
        $createResponse->assertStatus(201);

        // View customer (allowed)
        $viewResponse = $this->getJson("/api/customers/{$customer->id}");
        $viewResponse->assertStatus(200);

        // Edit customer (allowed)
        $updateResponse = $this->putJson("/api/customers/{$customer->id}", [
            'name' => 'Samuel Wilson',
        ]);
        $updateResponse->assertStatus(200);

        // Delete customer (forbidden)
        $deleteResponse = $this->deleteJson("/api/customers/{$customer->id}");
        $deleteResponse->assertStatus(403);
    }
}
