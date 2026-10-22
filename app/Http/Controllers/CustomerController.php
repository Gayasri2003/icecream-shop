<?php

namespace App\Http\Controllers;

use App\Models\Customer;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\Gate;
use Illuminate\Validation\Rule;

class CustomerController extends Controller
{
    /**
     * Retrieve all customer records with optional name/email search.
     */
    public function index(Request $request): JsonResponse
    {
        Gate::authorize('viewAny', Customer::class);

        $query = Customer::query();

        if ($search = $request->input('search') ?? $request->input('name')) {
            $query->where(function ($q) use ($search) {
                $q->where('name', 'like', "%{$search}%")
                  ->orWhere('email', 'like', "%{$search}%")
                  ->orWhere('phone', 'like', "%{$search}%");
            });
        }

        $customers = $query->orderBy('name')->get();

        return response()->json([
            'data' => $customers,
            'count' => $customers->count(),
        ]);
    }

    /**
     * Retrieve a single customer record.
     */
    public function show(Customer $customer): JsonResponse
    {
        Gate::authorize('view', $customer);

        return response()->json([
            'data' => $customer,
        ]);
    }

    /**
     * Create a new customer record.
     */
    public function store(Request $request): JsonResponse
    {
        Gate::authorize('create', Customer::class);

        $validated = $request->validate([
            'name' => ['required', 'string', 'max:255'],
            'email' => ['required', 'string', 'email', 'max:255', 'unique:customers,email'],
            'phone' => ['nullable', 'string', 'max:50'],
            'address' => ['nullable', 'string'],
        ]);

        $customer = Customer::create($validated);

        return response()->json([
            'message' => 'Customer created successfully.',
            'data' => $customer,
        ], 201);
    }

    /**
     * Update an existing customer record.
     */
    public function update(Request $request, Customer $customer): JsonResponse
    {
        Gate::authorize('update', $customer);

        $validated = $request->validate([
            'name' => ['sometimes', 'required', 'string', 'max:255'],
            'email' => [
                'sometimes',
                'required',
                'string',
                'email',
                'max:255',
                Rule::unique('customers', 'email')->ignore($customer->id),
            ],
            'phone' => ['nullable', 'string', 'max:50'],
            'address' => ['nullable', 'string'],
        ]);

        $customer->update($validated);

        return response()->json([
            'message' => 'Customer updated successfully.',
            'data' => $customer->fresh(),
        ]);
    }

    /**
     * Delete a customer record.
     */
    public function destroy(Customer $customer): JsonResponse
    {
        Gate::authorize('delete', $customer);

        $customer->delete();

        return response()->json([
            'message' => 'Customer deleted successfully.',
        ]);
    }
}
