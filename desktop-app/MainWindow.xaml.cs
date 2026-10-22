using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using IceCreamShop.Desktop.Models;
using IceCreamShop.Desktop.Services;

namespace IceCreamShop.Desktop;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly ApiService _apiService = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool CanDelete => _apiService.CurrentUser?.IsOwner == true;
    public bool CanAddItem => _apiService.CurrentUser?.IsCashier != true;

    private long? _editingItemId = null;
    private long? _editingCustomerId = null;

    public MainWindow()
    {
        InitializeComponent();
    }

    // =============================================================
    // AUTHENTICATION & LOGIN
    // =============================================================
    private async void OnLoginClicked(object sender, RoutedEventArgs e)
    {
        var email = LoginEmailTextBox.Text.Trim();
        var password = LoginPasswordBox.Password;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ShowLoginError("Please enter both email and password.");
            return;
        }

        LoginButton.IsEnabled = false;
        LoginButton.Content = "Signing in...";
        HideLoginError();

        var result = await _apiService.LoginAsync(email, password);

        LoginButton.IsEnabled = true;
        LoginButton.Content = "Sign In";

        if (!result.Success || result.Data == null)
        {
            ShowLoginError(result.Message ?? "Login failed. Please check your credentials.");
            return;
        }

        SetupAuthenticatedUserView(result.Data);
        LoginViewGrid.Visibility = Visibility.Collapsed;
        MainDashboardGrid.Visibility = Visibility.Visible;

        await LoadItemsAsync();
    }

    private void SetupAuthenticatedUserView(User user)
    {
        UserDisplayNameText.Text = user.Name;
        RoleBadgeText.Text = user.Role.ToUpper();

        RoleBadgeBorder.Background = user.Role.ToLower() switch
        {
            "owner" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8b5cf6")),
            "manager" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f59e0b")),
            "cashier" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#06b6d4")),
            _ => new SolidColorBrush(Colors.Gray)
        };

        AddItemButton.Visibility = CanAddItem ? Visibility.Visible : Visibility.Collapsed;

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanDelete)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanAddItem)));

        ShowToast($"Welcome, {user.Name}!", false);
    }

    private async void OnLogoutClicked(object sender, RoutedEventArgs e)
    {
        await _apiService.LogoutAsync();

        ItemsDataGrid.ItemsSource = null;
        CustomersDataGrid.ItemsSource = null;

        MainDashboardGrid.Visibility = Visibility.Collapsed;
        LoginViewGrid.Visibility = Visibility.Visible;
        LoginEmailTextBox.Text = string.Empty;
        LoginPasswordBox.Password = string.Empty;
        HideLoginError();
        DismissToast();

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanDelete)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanAddItem)));
    }

    private void OnDeleteButtonLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            btn.Visibility = CanDelete ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void ShowLoginError(string message)
    {
        LoginErrorMessage.Text = message;
        LoginErrorBanner.Visibility = Visibility.Visible;
    }

    private void HideLoginError()
    {
        LoginErrorBanner.Visibility = Visibility.Collapsed;
    }

    // =============================================================
    // TABS NAVIGATION
    // =============================================================
    private async void OnInventoryTabSelected(object sender, RoutedEventArgs e)
    {
        if (InventoryPanel == null || CustomersPanel == null) return;
        CustomersPanel.Visibility = Visibility.Collapsed;
        InventoryPanel.Visibility = Visibility.Visible;

        if (_apiService.IsAuthenticated)
        {
            await LoadItemsAsync();
        }
    }

    private async void OnCustomersTabSelected(object sender, RoutedEventArgs e)
    {
        if (InventoryPanel == null || CustomersPanel == null) return;
        InventoryPanel.Visibility = Visibility.Collapsed;
        CustomersPanel.Visibility = Visibility.Visible;

        if (_apiService.IsAuthenticated)
        {
            await LoadCustomersAsync();
        }
    }

    // =============================================================
    // INVENTORY MANAGEMENT
    // =============================================================
    private async Task LoadItemsAsync()
    {
        var search = ItemSearchBox.Text.Trim();
        var selectedItem = ItemCategoryFilterCombo.SelectedItem as ComboBoxItem;
        var category = selectedItem?.Content?.ToString();
        if (category != null && category.StartsWith("All", StringComparison.OrdinalIgnoreCase))
        {
            category = null;
        }

        var result = await _apiService.GetItemsAsync(search, category);
        if (result.Success && result.Data != null)
        {
            ItemsDataGrid.ItemsSource = null;
            ItemsDataGrid.ItemsSource = result.Data;
        }
        else
        {
            ShowToast(result.Message ?? "Failed to load inventory items.", true);
        }
    }

    private async void OnItemSearchClicked(object sender, RoutedEventArgs e)
    {
        await LoadItemsAsync();
    }

    private async void OnItemSearchKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            await LoadItemsAsync();
        }
    }

    private async void OnCategoryFilterChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_apiService.IsAuthenticated)
        {
            await LoadItemsAsync();
        }
    }

    private async void OnRefreshItemsClicked(object sender, RoutedEventArgs e)
    {
        ItemSearchBox.Text = string.Empty;
        ItemCategoryFilterCombo.SelectedIndex = 0;
        await LoadItemsAsync();
    }

    private void OnAddNewItemClicked(object sender, RoutedEventArgs e)
    {
        if (_apiService.CurrentUser?.IsCashier == true)
        {
            ShowToast("Permission denied: Cashiers cannot add items.", true);
            return;
        }

        _editingItemId = null;
        ItemModalTitle.Text = "Add New Inventory Item";
        ItemFormNameTextBox.Text = string.Empty;
        ItemFormCategoryCombo.SelectedIndex = 0;
        ItemFormPriceTextBox.Text = string.Empty;
        ItemFormQuantityTextBox.Text = string.Empty;
        ItemFormDescTextBox.Text = string.Empty;

        ItemModalOverlay.Visibility = Visibility.Visible;
    }

    private void OnEditItemRowClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Item item)
        {
            _editingItemId = item.Id;
            ItemModalTitle.Text = $"Edit Item #{item.Id}: {item.Name}";
            ItemFormNameTextBox.Text = item.Name;
            ItemFormPriceTextBox.Text = item.Price.ToString("0.00");
            ItemFormQuantityTextBox.Text = item.Quantity.ToString();
            ItemFormDescTextBox.Text = item.Description ?? string.Empty;

            foreach (ComboBoxItem cbItem in ItemFormCategoryCombo.Items)
            {
                if (string.Equals(cbItem.Content?.ToString(), item.Category, StringComparison.OrdinalIgnoreCase))
                {
                    ItemFormCategoryCombo.SelectedItem = cbItem;
                    break;
                }
            }

            ItemModalOverlay.Visibility = Visibility.Visible;
        }
    }

    private async void OnDeleteItemRowClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Item item)
        {
            if (_apiService.CurrentUser?.IsOwner != true)
            {
                ShowToast("Permission denied: Only Owner can delete items.", true);
                return;
            }

            var confirm = MessageBox.Show(
                $"Are you sure you want to delete '{item.Name}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm == MessageBoxResult.Yes)
            {
                var result = await _apiService.DeleteItemAsync(item.Id);
                if (result.Success)
                {
                    ShowToast($"Item '{item.Name}' deleted successfully.", false);
                    await LoadItemsAsync();
                }
                else
                {
                    ShowToast(result.Message ?? "Failed to delete item.", true);
                }
            }
        }
    }

    private async void OnSaveItemFormClicked(object sender, RoutedEventArgs e)
    {
        var name = ItemFormNameTextBox.Text.Trim();
        var selectedItem = ItemFormCategoryCombo.SelectedItem as ComboBoxItem;
        var category = selectedItem?.Content?.ToString() ?? "Chocolate";

        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Please enter an item name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(ItemFormPriceTextBox.Text.Trim(), out var price) || price < 0)
        {
            MessageBox.Show("Please enter a valid price (>= 0).", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(ItemFormQuantityTextBox.Text.Trim(), out var quantity) || quantity < 0)
        {
            MessageBox.Show("Please enter a valid quantity (>= 0).", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var description = ItemFormDescTextBox.Text.Trim();

        SaveItemButton.IsEnabled = false;

        if (_editingItemId == null)
        {
            var result = await _apiService.CreateItemAsync(name, category, price, quantity, description);
            SaveItemButton.IsEnabled = true;

            if (result.Success)
            {
                ItemModalOverlay.Visibility = Visibility.Collapsed;
                ShowToast($"Item '{name}' created successfully.", false);
                ItemSearchBox.Text = string.Empty;
                ItemCategoryFilterCombo.SelectedIndex = 0;
                await LoadItemsAsync();
            }
            else
            {
                ShowToast(result.Message ?? "Failed to create item.", true);
            }
        }
        else
        {
            var result = await _apiService.UpdateItemAsync(_editingItemId.Value, name, category, price, quantity, description);
            SaveItemButton.IsEnabled = true;

            if (result.Success)
            {
                ItemModalOverlay.Visibility = Visibility.Collapsed;
                ShowToast($"Item '{name}' updated successfully.", false);
                await LoadItemsAsync();
            }
            else
            {
                ShowToast(result.Message ?? "Failed to update item.", true);
            }
        }
    }

    private void OnCancelItemModalClicked(object sender, RoutedEventArgs e)
    {
        ItemModalOverlay.Visibility = Visibility.Collapsed;
    }

    // =============================================================
    // CUSTOMER MANAGEMENT
    // =============================================================
    private async Task LoadCustomersAsync()
    {
        var search = CustomerSearchBox.Text.Trim();
        var result = await _apiService.GetCustomersAsync(search);

        if (result.Success && result.Data != null)
        {
            CustomersDataGrid.ItemsSource = null;
            CustomersDataGrid.ItemsSource = result.Data;
        }
        else
        {
            ShowToast(result.Message ?? "Failed to load customers.", true);
        }
    }

    private async void OnCustomerSearchClicked(object sender, RoutedEventArgs e)
    {
        await LoadCustomersAsync();
    }

    private async void OnCustomerSearchKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            await LoadCustomersAsync();
        }
    }

    private async void OnRefreshCustomersClicked(object sender, RoutedEventArgs e)
    {
        CustomerSearchBox.Text = string.Empty;
        await LoadCustomersAsync();
    }

    private void OnAddNewCustomerClicked(object sender, RoutedEventArgs e)
    {
        _editingCustomerId = null;
        CustomerModalTitle.Text = "Add New Customer";
        CustomerFormNameTextBox.Text = string.Empty;
        CustomerFormEmailTextBox.Text = string.Empty;
        CustomerFormPhoneTextBox.Text = string.Empty;
        CustomerFormAddressTextBox.Text = string.Empty;

        CustomerModalOverlay.Visibility = Visibility.Visible;
    }

    private void OnEditCustomerRowClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Customer customer)
        {
            _editingCustomerId = customer.Id;
            CustomerModalTitle.Text = $"Edit Customer #{customer.Id}: {customer.Name}";
            CustomerFormNameTextBox.Text = customer.Name;
            CustomerFormEmailTextBox.Text = customer.Email;
            CustomerFormPhoneTextBox.Text = customer.Phone ?? string.Empty;
            CustomerFormAddressTextBox.Text = customer.Address ?? string.Empty;

            CustomerModalOverlay.Visibility = Visibility.Visible;
        }
    }

    private async void OnDeleteCustomerRowClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Customer customer)
        {
            if (_apiService.CurrentUser?.IsOwner != true)
            {
                ShowToast("Permission denied: Only Owner can delete customers.", true);
                return;
            }

            var confirm = MessageBox.Show(
                $"Are you sure you want to delete customer '{customer.Name}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm == MessageBoxResult.Yes)
            {
                var result = await _apiService.DeleteCustomerAsync(customer.Id);
                if (result.Success)
                {
                    ShowToast($"Customer '{customer.Name}' deleted successfully.", false);
                    await LoadCustomersAsync();
                }
                else
                {
                    ShowToast(result.Message ?? "Failed to delete customer.", true);
                }
            }
        }
    }

    private async void OnSaveCustomerFormClicked(object sender, RoutedEventArgs e)
    {
        var name = CustomerFormNameTextBox.Text.Trim();
        var email = CustomerFormEmailTextBox.Text.Trim();
        var phone = CustomerFormPhoneTextBox.Text.Trim();
        var address = CustomerFormAddressTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Please enter the customer name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            MessageBox.Show("Please enter a valid email address.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        SaveCustomerButton.IsEnabled = false;

        if (_editingCustomerId == null)
        {
            var result = await _apiService.CreateCustomerAsync(name, email, phone, address);
            SaveCustomerButton.IsEnabled = true;

            if (result.Success)
            {
                CustomerModalOverlay.Visibility = Visibility.Collapsed;
                ShowToast($"Customer '{name}' created successfully.", false);
                CustomerSearchBox.Text = string.Empty;
                await LoadCustomersAsync();
            }
            else
            {
                ShowToast(result.Message ?? "Failed to create customer.", true);
            }
        }
        else
        {
            var result = await _apiService.UpdateCustomerAsync(_editingCustomerId.Value, name, email, phone, address);
            SaveCustomerButton.IsEnabled = true;

            if (result.Success)
            {
                CustomerModalOverlay.Visibility = Visibility.Collapsed;
                ShowToast($"Customer '{name}' updated successfully.", false);
                await LoadCustomersAsync();
            }
            else
            {
                ShowToast(result.Message ?? "Failed to update customer.", true);
            }
        }
    }

    private void OnCancelCustomerModalClicked(object sender, RoutedEventArgs e)
    {
        CustomerModalOverlay.Visibility = Visibility.Collapsed;
    }

    // =============================================================
    // TOAST NOTIFICATIONS
    // =============================================================
    private void ShowToast(string message, bool isError)
    {
        ToastMessageText.Text = message;
        if (isError)
        {
            ToastBanner.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7f1d1d"));
            ToastBanner.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ef4444"));
            ToastMessageText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fecaca"));
        }
        else
        {
            ToastBanner.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#064e3b"));
            ToastBanner.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10b981"));
            ToastMessageText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#a7f3d0"));
        }

        ToastBanner.Visibility = Visibility.Visible;
    }

    private void OnDismissToastClicked(object sender, RoutedEventArgs e)
    {
        DismissToast();
    }

    private void DismissToast()
    {
        ToastBanner.Visibility = Visibility.Collapsed;
    }
}