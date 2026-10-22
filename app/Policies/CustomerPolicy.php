<?php

namespace App\Policies;

use App\Models\Customer;
use App\Models\User;

class CustomerPolicy
{
    /**
     * Owner, Manager, Cashier can view customers.
     */
    public function viewAny(User $user): bool
    {
        return $user->hasRole(['owner', 'manager', 'cashier']);
    }

    /**
     * Owner, Manager, Cashier can view a single customer.
     */
    public function view(User $user, Customer $customer): bool
    {
        return $user->hasRole(['owner', 'manager', 'cashier']);
    }

    /**
     * Owner, Manager, Cashier can create customers.
     */
    public function create(User $user): bool
    {
        return $user->hasRole(['owner', 'manager', 'cashier']);
    }

    /**
     * Owner, Manager, Cashier can update customers.
     */
    public function update(User $user, Customer $customer): bool
    {
        return $user->hasRole(['owner', 'manager', 'cashier']);
    }

    /**
     * Only Owner can delete customer records.
     */
    public function delete(User $user, Customer $customer): bool
    {
        return $user->isOwner();
    }
}
