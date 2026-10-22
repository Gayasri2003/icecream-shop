<?php

namespace Tests\Feature;

use App\Models\Item;
use App\Models\User;
use Illuminate\Foundation\Testing\RefreshDatabase;
use Laravel\Sanctum\Sanctum;
use Tests\TestCase;

class ItemManagementTest extends TestCase
{
    use RefreshDatabase;

    public function test_owner_can_perform_all_crud_operations_on_items(): void
    {
        $owner = User::factory()->create(['role' => 'owner']);
        Sanctum::actingAs($owner);

        // 1. Create item
        $createResponse = $this->postJson('/api/items', [
            'name' => 'Belgian Dark Chocolate',
            'category' => 'Chocolate',
            'price' => 4.50,
            'quantity' => 100,
            'description' => 'Premium dark chocolate scoop.',
        ]);

        $createResponse->assertStatus(201)
            ->assertJsonPath('data.name', 'Belgian Dark Chocolate');

        $itemId = $createResponse->json('data.id');

        // 2. View item
        $viewResponse = $this->getJson("/api/items/{$itemId}");
        $viewResponse->assertStatus(200)
            ->assertJsonPath('data.id', $itemId);

        // 3. Update item
        $updateResponse = $this->putJson("/api/items/{$itemId}", [
            'price' => 5.25,
            'quantity' => 90,
        ]);
        $updateResponse->assertStatus(200)
            ->assertJsonPath('data.price', '5.25');

        // 4. Delete item
        $deleteResponse = $this->deleteJson("/api/items/{$itemId}");
        $deleteResponse->assertStatus(200);

        $this->assertDatabaseMissing('items', ['id' => $itemId]);
    }

    public function test_manager_can_create_view_and_edit_items_but_cannot_delete(): void
    {
        $manager = User::factory()->create(['role' => 'manager']);
        Sanctum::actingAs($manager);

        // Create item (allowed)
        $createResponse = $this->postJson('/api/items', [
            'name' => 'Strawberry Swirl',
            'category' => 'Strawberry',
            'price' => 3.75,
            'quantity' => 50,
        ]);
        $createResponse->assertStatus(201);
        $itemId = $createResponse->json('data.id');

        // Edit item (allowed)
        $updateResponse = $this->patchJson("/api/items/{$itemId}", [
            'quantity' => 45,
        ]);
        $updateResponse->assertStatus(200);

        // Delete item (forbidden)
        $deleteResponse = $this->deleteJson("/api/items/{$itemId}");
        $deleteResponse->assertStatus(403);

        $this->assertDatabaseHas('items', ['id' => $itemId]);
    }

    public function test_cashier_can_view_and_edit_items_but_cannot_create_or_delete(): void
    {
        $item = Item::create([
            'name' => 'Vanilla Bean Scoop',
            'category' => 'Vanilla',
            'price' => 3.00,
            'quantity' => 40,
        ]);

        $cashier = User::factory()->create(['role' => 'cashier']);
        Sanctum::actingAs($cashier);

        // View item (allowed)
        $viewResponse = $this->getJson("/api/items/{$item->id}");
        $viewResponse->assertStatus(200);

        // Edit item (allowed)
        $updateResponse = $this->putJson("/api/items/{$item->id}", [
            'quantity' => 38,
        ]);
        $updateResponse->assertStatus(200);

        // Create item (forbidden)
        $createResponse = $this->postJson('/api/items', [
            'name' => 'New Unauthorized Item',
            'category' => 'Vanilla',
            'price' => 3.50,
            'quantity' => 20,
        ]);
        $createResponse->assertStatus(403);

        // Delete item (forbidden)
        $deleteResponse = $this->deleteJson("/api/items/{$item->id}");
        $deleteResponse->assertStatus(403);
    }

    public function test_can_search_items_by_name(): void
    {
        $cashier = User::factory()->create(['role' => 'cashier']);
        Sanctum::actingAs($cashier);

        Item::create(['name' => 'Chocolate Truffle', 'category' => 'Chocolate', 'price' => 4.00, 'quantity' => 10]);
        Item::create(['name' => 'Vanilla Bean', 'category' => 'Vanilla', 'price' => 3.50, 'quantity' => 15]);
        Item::create(['name' => 'Strawberry Bliss', 'category' => 'Strawberry', 'price' => 4.20, 'quantity' => 12]);

        $searchResponse = $this->getJson('/api/items?search=Chocolate');

        $searchResponse->assertStatus(200)
            ->assertJsonPath('count', 1)
            ->assertJsonPath('data.0.name', 'Chocolate Truffle');
    }
}
