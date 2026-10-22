<?php

namespace App\Http\Controllers;

use App\Models\Item;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\Gate;

class ItemController extends Controller
{
    /**
     * Retrieve all inventory items with optional name/category search.
     */
    public function index(Request $request): JsonResponse
    {
        Gate::authorize('viewAny', Item::class);

        $query = Item::query();

        // Search by item name
        if ($search = $request->input('name') ?? $request->input('search')) {
            $query->where('name', 'like', "%{$search}%");
        }

        // Filter by category / flavor
        if ($category = $request->input('category')) {
            $query->where('category', $category);
        }

        $items = $query->orderBy('name')->get();

        return response()->json([
            'data' => $items,
            'count' => $items->count(),
        ]);
    }

    /**
     * Retrieve a single inventory item.
     */
    public function show(Item $item): JsonResponse
    {
        Gate::authorize('view', $item);

        return response()->json([
            'data' => $item,
        ]);
    }

    /**
     * Create a new inventory item.
     */
    public function store(Request $request): JsonResponse
    {
        Gate::authorize('create', Item::class);

        $validated = $request->validate([
            'name' => ['required', 'string', 'max:255'],
            'category' => ['required', 'string', 'max:100'],
            'price' => ['required', 'numeric', 'min:0'],
            'quantity' => ['required', 'integer', 'min:0'],
            'description' => ['nullable', 'string'],
        ]);

        $item = Item::create($validated);

        return response()->json([
            'message' => 'Inventory item created successfully.',
            'data' => $item,
        ], 201);
    }

    /**
     * Update an existing inventory item.
     */
    public function update(Request $request, Item $item): JsonResponse
    {
        Gate::authorize('update', $item);

        $validated = $request->validate([
            'name' => ['sometimes', 'required', 'string', 'max:255'],
            'category' => ['sometimes', 'required', 'string', 'max:100'],
            'price' => ['sometimes', 'required', 'numeric', 'min:0'],
            'quantity' => ['sometimes', 'required', 'integer', 'min:0'],
            'description' => ['nullable', 'string'],
        ]);

        $item->update($validated);

        return response()->json([
            'message' => 'Inventory item updated successfully.',
            'data' => $item->fresh(),
        ]);
    }

    /**
     * Delete an inventory item.
     */
    public function destroy(Item $item): JsonResponse
    {
        Gate::authorize('delete', $item);

        $item->delete();

        return response()->json([
            'message' => 'Inventory item deleted successfully.',
        ]);
    }
}
