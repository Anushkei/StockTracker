using StockTracker.Models;
using StockTracker.Networking;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static StockTracker.Models.Auxiliary;

namespace StockTracker
{
    public partial class MainWindow : Window
    {
        private IDatabaseAdapter Database { get; set; }
        public bool BuyFromAccount { get; private set; }

        public MainWindow()
        {
            InitializeComponent();

            this.Database = new SqlDatabaseAdapter();

            ClearTransactionRegister();
            this.RetrieveData();

            this.BuyFromAccount = true;
            this.UpdateTradePanel();
        }

        private void RetrieveData()
        {
            this.AccountsList.ItemsSource = this.Database.Select<Account>("SELECT * FROM dbo.[Account] WHERE 1 = 1;");
            this.TransactionLog.ItemsSource = this.Database.Select<TransactionLog>("SELECT * FROM dbo.[Transaction_Log] WHERE 1 = 1;");
            this.CorporationsList.ItemsSource = this.GetCorporations();
        }

        public void UpdateTradePanel()
        {
            if (this.BuyFromAccount)
            {
                tbxSellerAccount.Text = "";
                tbxSellerAccount.IsEnabled = true;
            }
            else
            {
                tbxSellerAccount.Text = "Corporation";
                tbxSellerAccount.IsEnabled = false;
            }
        }

        private bool CheckAccountValidity()
        {
            bool areValid = AreAccountNumbersValid();
            if (areValid)
            {
                lblAccountNumberError.Visibility = Visibility.Hidden;
            }
            else
            {
                lblAccountNumberError.Visibility = Visibility.Visible;
            }

            return areValid;
        }

        private bool IsAccountTextValid(string accountNum)
        {
            if (accountNum.Length != 10)
            {
                return false;
            }

            // Get the 'long' values of the account numbers.
            bool result = long.TryParse(accountNum, out long num);
            if (!result || num <= 0)
            {
                return false;
            }

            return true;
        }

        private bool AreAccountNumbersValid()
        {
            if (this.BuyFromAccount)
            {
                // Buying from another account -- examine both textboxes.
                string buyerText = tbxBuyerAccount.Text.Trim();
                string sellerText = tbxSellerAccount.Text.Trim();

                return IsAccountTextValid(buyerText) && IsAccountTextValid(sellerText) && (buyerText != sellerText);
            }
            else
            {
                // Buying from corporation -- only examine the first textbox.
                return IsAccountTextValid(tbxBuyerAccount.Text);
            }
        }

        private Account GetAccount(long accountNumber)
        {
            string query = "SELECT * FROM dbo.[Account] WHERE [AccountId] = " + accountNumber + ";";
            return this.Database.Select<Account>(query).FirstOrDefault();
        }

        private List<Corporation> GetCorporations()
        {
           return this.Database.Select<Corporation>("SELECT * FROM dbo.[Corporation] WHERE 1 = 1;").ToList();
        }

        private void ClearTransactionRegister()
        {
            tbxBuyerAccount.Text = "";
            tbxSellerAccount.Text = "";
            tbxQuantity.Text = "";
            tbxStockSymbol.Text = "";
            lblPrice.Content = DecimalToDollarString(0.00M);

            lblQuantityError.Visibility = Visibility.Hidden;
            lblStockSymbolError.Visibility = Visibility.Hidden;
            lblAccountNumberError.Visibility = Visibility.Hidden;

            this.cbBuyTypes.SelectedIndex = 0;
            this.UpdateTradePanel();
        }

        /// <summary>
        /// Yes, this function is disgustingly long. No, I don't care.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void btnMakeTrade_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // If the account numbers are invalid, return.
                bool areValid = CheckAccountValidity();
                if (!areValid)
                {
                    return;
                }

                // If the stock symbol is invalid, return.
                if (lblStockSymbolError.Visibility == Visibility.Visible)
                {
                    return;
                }

                // Try to retrieve the buyer account -- if we can't do that, return and show an error.
                Account buyer = GetAccount(long.Parse(tbxBuyerAccount.Text));
                if (buyer == null)
                {
                    lblAccountNumberError.Visibility = Visibility.Visible;
                    return;
                }

                // If the quantity is negative, return and show an error.
                long quantity = long.Parse(tbxQuantity.Text);
                if (quantity <= 0)
                {
                    lblQuantityError.Visibility = Visibility.Visible;
                    return;
                }

                // Create generic lists to hold all the records to be updated/deleted/created at the end.
                List<IDataModel> recordsToUpdate = new List<IDataModel>();
                List<IDataModel> recordsToCreate = new List<IDataModel>();
                List<IDataModel> recordsToDelete = new List<IDataModel>();

                var corporation = GetCorporations().Where(c => c.StockSymbol == tbxStockSymbol.Text).FirstOrDefault();
                if (corporation == null)
                {
                    lblStockSymbolError.Visibility = Visibility.Visible;
                    return;
                }

                var buyerOwnershipRecord = GetOwnershipRecord(buyer.AccountId, corporation.StockSymbol);
                decimal finalPrice = corporation.StockPrice * (decimal)quantity;

                if (buyer.CashBalance < finalPrice)
                {
                    // Buyer doesn't have the cash-balance required to buy.
                    throw new InvalidOperationException("The buyer account does not have the required cash balance to make that purchase.");
                }

                long sellerAccountId = -1;
                if (BuyFromAccount)
                {
                    Account seller = GetAccount(long.Parse(tbxSellerAccount.Text));

                    if (seller == null)
                    {
                        lblAccountNumberError.Visibility = Visibility.Visible;
                        return;
                    }

                    sellerAccountId = seller.AccountId;
                    var sellerOwnershipRecord = GetOwnershipRecord(seller.AccountId, corporation.StockSymbol);

                    // Ensure buyer has adequate funds; ensure seller has adequate stocks.
                    if (sellerOwnershipRecord == null || sellerOwnershipRecord.Quantity < quantity)
                    {
                        // Seller either doesn't own the stock, or doesn't own enough.
                        throw new InvalidOperationException("Seller cannot sell this stock, or this amount of this stock.");
                    }

                    sellerOwnershipRecord.Quantity -= quantity;

                    if (sellerOwnershipRecord.Quantity == 0)
                    {
                        // Delete the ownership record if the person now owns 0 shares.
                        recordsToDelete.Add(sellerOwnershipRecord);
                    }
                    else
                    {
                        recordsToUpdate.Add(sellerOwnershipRecord);
                    }

                    seller.CashBalance += finalPrice;
                    recordsToUpdate.Add(seller);
                }
                else
                {
                    // A company is selling stock: so there's no seller account ID, but that's fine.
                    sellerAccountId = -1;

                    if (corporation.SharesOwned < quantity)
                    {
                        throw new InvalidOperationException("The company cannot sell that many shares.");
                    }

                    // Remove the shares from the company.
                    corporation.SharesOwned -= quantity;
                    recordsToUpdate.Add(corporation);
                }

                // If the buyer doesn't have an ownership record already, create a new one; otherwise, update the existing one.
                if (buyerOwnershipRecord == null)
                {
                    // Create a NEW ownership record.
                    buyerOwnershipRecord = new OwnershipRecord
                    {
                        AccountId = buyer.AccountId,
                        StockSymbol = corporation.StockSymbol,
                        Quantity = quantity
                    };

                    recordsToCreate.Add(buyerOwnershipRecord);
                }
                else
                {
                    // Already existing ownership record, so add in the quantity.
                    buyerOwnershipRecord.Quantity += quantity;
                    recordsToUpdate.Add(buyerOwnershipRecord);
                }

                buyer.CashBalance -= finalPrice;
                recordsToUpdate.Add(buyer);

                // A new TransactionLog record is always created.
                recordsToCreate.Add(
                    new TransactionLog()
                    {
                        Quantity = quantity,
                        BuyerAccountId = buyer.AccountId,
                        SellerAccountId = sellerAccountId,
                        StockPrice = corporation.StockPrice,
                        StockSymbol = corporation.StockSymbol,
                    });

                // Update the database, then retrieve all data to refresh the view.
                this.Database.Insert(recordsToCreate);
                this.Database.Update(recordsToUpdate);
                this.Database.Delete(recordsToDelete);

                this.RetrieveData();
                this.ClearTransactionRegister();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString(), "Error");
            }
        }

        private OwnershipRecord GetOwnershipRecord(long accountId, string stockSymbol)
        { 
            var query = string.Format("SELECT * FROM dbo.[Ownership_Record] WHERE [AccountId] = {0} AND [StockSymbol] = {1};",
                accountId, GetSingleQuotes(stockSymbol));
            return Database.Select<OwnershipRecord>(query).FirstOrDefault();
        }

        private void btnCancelTrade_Click(object sender, EventArgs e)
        {
            ClearTransactionRegister();
        }

        private void tbxQuantity_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (ulong.TryParse(tbxQuantity.Text, out ulong throwaway))
                {
                    lblQuantityError.Visibility = Visibility.Hidden;
                }
                else
                {
                    lblQuantityError.Visibility = Visibility.Visible;
                }

                SetTransactionPrice();
            }
            catch (Exception ex)
            {

            }
        }

        private void tbxStockSymbol_TextChanged(object sender, TextChangedEventArgs e)
        {
            SetTransactionPrice();
        }

        private void SetTransactionPrice()
        {
            string symbol = tbxStockSymbol.Text.Trim();

            var corporations = this.GetCorporations();
            Corporation stock = corporations.Where(s => s.StockSymbol == symbol).FirstOrDefault();

            decimal price = 0.00M;
            if (stock != null)
            {
                price = stock.StockPrice;
                lblStockSymbolError.Visibility = Visibility.Hidden;
            }
            else
            {
                lblStockSymbolError.Visibility = Visibility.Visible;
            }

            ulong.TryParse(tbxQuantity.Text, out ulong quantity);
            decimal finalPrice = (decimal)quantity * price;
            lblPrice.Content = DecimalToDollarString(finalPrice);
        }

        private void AccountsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.AccountsList.SelectedItem is Account account)
            {

                // Start by clearing the assets list and account value field.
                this.AssetsList.ItemsSource = new List<OwnershipRecord>();
                lblAccountValue.Content = DecimalToDollarString(0.00M);

                string query = "SELECT * FROM dbo.[Ownership_Record] WHERE [AccountId] = " + account.AccountId + ";";
                var assetsList = this.Database.Select<OwnershipRecord>(query);

                // Update the corporations list.
                var corporations = this.GetCorporations();
                this.CorporationsList.ItemsSource = corporations;

                decimal accountValue = account.CashBalance;

                // Loop through each equity and calculate its current value.
                foreach (OwnershipRecord record in assetsList)
                {
                    Corporation corp = corporations.Where(c => c.StockSymbol.Equals(record.StockSymbol)).FirstOrDefault();

                    if (corp != null)
                    {
                        record.SetStockPrice(corp.StockPrice);
                    }

                    accountValue += record.CurrentValue;
                }

                this.AssetsList.ItemsSource = assetsList;
                this.lblAccountValue.Content = DecimalToDollarString(accountValue);
            }
        }

        private void tbxSellerAccount_TextChanged(object sender, TextChangedEventArgs e)
        {
            CheckAccountValidity();
        }

        private void tbxBuyerAccount_TextChanged(object sender, TextChangedEventArgs e)
        {
            CheckAccountValidity();
        }

        private void btnSell_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.cbBuyTypes.SelectedIndex = 0;
                this.UpdateTradePanel();

                if (AccountsList.SelectedItem is Account account)
                {
                    this.tbxSellerAccount.Text = account.AccountId.ToString();

                    if (AssetsList.SelectedItem is OwnershipRecord record)
                    {
                        this.tbxStockSymbol.Text = record.StockSymbol;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private void AccountsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                btnSetBuyer_Click(sender, e);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private void CorporationsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                btnTrade_Click(sender, e);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private void btnTrade_Click(object sender, EventArgs e)
        {
            try
            {
                this.cbBuyTypes.SelectedIndex = 1;
                this.UpdateTradePanel();

                if (CorporationsList.SelectedItem is Corporation corp)
                {
                    this.tbxStockSymbol.Text = corp.StockSymbol;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private void btnSetBuyer_Click(object sender, EventArgs e)
        {
            try
            {
                if (AccountsList.SelectedItem is Account account)
                {
                    this.tbxBuyerAccount.Text = account.AccountId.ToString();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }


        private void cbBuyTypes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (this.IsLoaded)
                {
                    if (cbBuyTypes.SelectedItem.ToString().Contains("Account"))
                    {
                        this.BuyFromAccount = true;
                    }
                    else
                    {
                        this.BuyFromAccount = false;
                    }

                    this.UpdateTradePanel();
                }
            }
            catch (NullReferenceException nx)
            {

            }
        }
    }
}
