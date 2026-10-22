<?php

namespace App\Policies;

use App\Models\Item;
use App\Models\User;

class ItemPolicy
{
    /**
     * Owner, Manager, Cashier can view inventory items.
     */
    public function viewAny(User $user): bool
    {
        return $user->hasRole(['owner', 'manager', 'cashier']);
    }

    /**
     * Owner, Manager, Cashier can view a single item.
     */
    public function view(User $user, Item $item): bool
    {
        return $user->hasRole(['owner', 'manager', 'cashier']);
    }

    /**
     * Owner and Manager can create inventory items.
     */
    public function create(User $user): bool
    {
        return $user->hasRole(['owner', 'manager']);
    }

    /**
     * Owner, Manager, and Cashier can update inventory items.
     */
    public function update(User $user, Item $item): bool
    {
        return $user->hasRole(['owner', 'manager', 'cashier']);
    }

    /**
     * Only Owner can delete inventory items.
     */
    public function delete(User $user, Item $item): bool
    {
        return $user->isOwner();
    }
}
