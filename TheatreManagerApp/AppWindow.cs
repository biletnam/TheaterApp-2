﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheatreManagerApp;
using System.Data.OleDb;
using System.Linq;
using System.Text.RegularExpressions;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;

//  Notes
//  If you add or delete columns in the DB make sure to check that the update functions are working (StockTableSearchByProdName)
//  New Columns Should get added automatically so if you do not want info displayed make sure to adjust SQL statements appropriately
//  TODO:
//  Beautify the application 
//  Add images for movies and additional properties
//  Ability to add new movies to the schedule?
//  Ability to add new movies to the DB?
//  More interaction with the DB would be good

namespace TheatreManagerApp
{
    public partial class AppWindow : Form
    {
        private BindingSource bindingSource1 = new BindingSource();
        private BindingSource bindingSource2 = new BindingSource();
        List<string> MenuList = new List<string>();
        List<string> StockList = new List<string>();
        DataTable StockTable = new DataTable();

        public AppWindow()
        {
            InitializeComponent();
            LoadStockTable();
            dtpStart.Value = DateTime.Now;
            dtpEnd.Value = DateTime.Now.AddDays(1);

            LoadListBoxes();
            LoadCenterPanel();
            LoadPrices();
            GetSchedule();
        }

        private void LoadCenterPanel()
        {
            dataGridView1.DataSource = bindingSource1;
            GetSchedule();
        }
        private void LoadPrices()
        {
            try
            {
                OleDbConnection connection = Utility.GetOleDBConnection();
                connection.Open();
                string Query = "SELECT  * FROM Box_Office BO;";
                OleDbCommand cmd = new OleDbCommand(Query, connection);
                DataTable BO = new DataTable();
                BO.Load(cmd.ExecuteReader());
                connection.Close();
                kidPrice.Text = "$" + BO.Rows[0][1].ToString();
                adultPrice.Text = "$" + BO.Rows[0][2].ToString();
                seniorPrice.Text = "$" + BO.Rows[0][3].ToString();
                matPrice.Text = "$" + BO.Rows[0][4].ToString();

            }
            catch (Exception ex)
            {
                MessageBox.Show("Oops, error: " + ex.Message + ex.StackTrace);
            }
        }
        private void GetSchedule()
        {
            try
            {

                OleDbConnection connection = Utility.GetOleDBConnection();
                String Start = dtpStart.Value.ToShortDateString();
                String End = dtpEnd.Value.ToShortDateString();
                string query =
                    "SELECT C.Date, S.Time, M.Title, M.Rating " +
                    "FROM Calendar C " +
                    "JOIN Schedule S " +
                        "ON C.Cal_Id = S.Calendar_Id " +
                    "JOIN Movie M " +
                        "ON M.Movie_Id = S.Movie_Id " +
                    "WHERE '" + Start + "' < C.Date AND C.Date < '" + End + "' ; ";


                DataTable table = new DataTable();
                using (connection)
                {
                    OleDbCommand cmd = new OleDbCommand(query, connection);
                    connection.Open();
                    OleDbDataAdapter adapter = new OleDbDataAdapter(cmd);
                    adapter.Fill(table);
                }
                connection.Close();
                // Populate a new data table and bind it to the BindingSource.
                bindingSource1.DataSource = table;


                foreach (DataGridViewColumn col in dataGridView1.Columns)
                {
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                }
            }
            catch (OleDbException ex)
            {
                MessageBox.Show("Oops, error: " + ex.Message + ex.StackTrace);
            }
        }
        private void LoadStockTable()
        {
            StockTable.Clear();
            try
            {
                //connect to the database 
                OleDbConnection connection = Utility.GetOleDBConnection();

                //query the database
                string query = "SELECT * FROM Menu M ";
                OleDbCommand cmd = new OleDbCommand(query, connection);
                connection.Open();
                OleDbDataAdapter adapter = new OleDbDataAdapter(cmd);
                adapter.Fill(StockTable);
                connection.Close();

            }
            catch (Exception ex)
            {
                MessageBox.Show("Oops, error: " + ex.Message + ex.StackTrace);
            }

            //reload menu list
        }
        private void LoadListBoxes()
        {
            MenuList.Clear();
            StockList.Clear();
            foreach (DataRow row in StockTable.Rows)
            {
                int quantity = Convert.ToInt32(row.ItemArray[3]);
                if (quantity < 50)
                {
                    MenuList.Add(row["Prod_Name"].ToString());
                    //MessageBox.Show(row["Prod_Name"].ToString() + " " + quantity.ToString() + " in MenuList");
                    StockList.Add(row["Prod_Name"].ToString());
                    //MessageBox.Show(row["Prod_Name"].ToString() + " " + quantity.ToString() + " in StockList");
                }
                else
                {
                    MenuList.Add(row["Prod_Name"].ToString());
                    //MessageBox.Show(row["Prod_Name"].ToString() + " " + quantity.ToString() + " in MenuList");
                }
            }

            Stock_List.DataSource = null;
            Stock_List.DataSource = this.StockList;
            Menu_List.DataSource = null;
            Menu_List.DataSource = this.MenuList;
        }
        private void AddBtn_Click(object sender, EventArgs e)
        {
            //The add button was clicked
            AddPopUp addPopUp = new AddPopUp();
            addPopUp.ShowDialog();

            //Reload database after adding new products
            LoadStockTable();
            LoadListBoxes();

            //If we just added an item, ensure the remove and edit buttons are enabled
            RemoveBtn.Enabled = true;
            EditBtn.Enabled = true;
        }
        private void RemoveBtn_Click(object sender, EventArgs e)
        {
            //The remove button was clicked
            int selectedIndex = Menu_List.SelectedIndex;
            string productName = Menu_List.SelectedItem.ToString();

            try
            {
                //Remove selected item in the list
                MenuList.RemoveAt(selectedIndex);

                OleDbConnection connection = Utility.GetOleDBConnection();
                connection.Open();
                string Query = "DELETE FROM Menu WHERE '" + productName + "' LIKE Prod_Name; ";
                OleDbCommand cmd = new OleDbCommand(Query, connection);
                cmd.ExecuteNonQuery();

                LoadStockTable();
                LoadListBoxes();
            }
            catch
            {
            }

            Menu_List.DataSource = null;
            Menu_List.DataSource = MenuList;

            //If the list is empty disable the remove and edit buttons
            if (Menu_List.Items.Count == 0)
            {
                RemoveBtn.Enabled = false;
                EditBtn.Enabled = false;
            }
        }
        private void EditBtn_Click(object sender, EventArgs e)
        {
            string productName = Menu_List.SelectedItem.ToString();
            int index = StockTableSearchProdName(productName);
            int prodNum = Menu_List.SelectedIndex;

            string quantity = StockTable.Rows[index][3].ToString();
            string price = StockTable.Rows[index][1].ToString();

            //The edit button was clicked
            EditPopUp editPopUp = new EditPopUp(productName, quantity, price);
            editPopUp.ShowDialog();

            //Reload database after editing products
            LoadStockTable();
            LoadListBoxes();

        }
        private void dtpEnd_ValueChanged(object sender, EventArgs e)
        {
            GetSchedule();

        }
        private void dtpStart_ValueChanged(object sender, EventArgs e)
        {
            GetSchedule();

        }
        private void EditPriceBtn_Click(object sender, EventArgs e)
        {
            //The edit price button was clicked
            PriceEditPopUp editPrice = new PriceEditPopUp(kidPrice.Text, adultPrice.Text, seniorPrice.Text, matPrice.Text);
            editPrice.ShowDialog();

            //Reload database after editing products
            LoadPrices();
        }
        private void DeleteLowStockBtn_Click(object sender, EventArgs e)
        {
            //The remove button was clicked
            int selectedIndex = Stock_List.SelectedIndex;
            string productName = Stock_List.SelectedItem.ToString();

            try
            {
                //Remove selected item in the list
                StockList.RemoveAt(selectedIndex);

                OleDbConnection connection = Utility.GetOleDBConnection();
                connection.Open();
                string Query = "DELETE FROM Menu WHERE '" + productName + "' LIKE Prod_Name; ";
                OleDbCommand cmd = new OleDbCommand(Query, connection);
                cmd.ExecuteNonQuery();

                LoadStockTable();
                LoadListBoxes();
            }
            catch
            {
            }

            Stock_List.DataSource = null;
            Stock_List.DataSource = StockList;

            //If the list is empty disable the remove and edit buttons
            if (Stock_List.Items.Count == 0)
            {
                DeleteLowStockBtn.Enabled = false;
                RestockBtn.Enabled = false;
            }
        }
        private void RestockBtn_Click(object sender, EventArgs e)
        {
            string productName = Stock_List.SelectedItem.ToString();
            int index = StockTableSearchProdName(productName);
            int prodNum = Stock_List.SelectedIndex;

            string quantity = StockTable.Rows[index][3].ToString();
            string price = StockTable.Rows[index][1].ToString();

            //The edit button was clicked
            EditPopUp editPopUp = new EditPopUp(productName, quantity, price);
            editPopUp.ShowDialog();

            //Reload database after editing products
            LoadStockTable();
            LoadListBoxes();
        }
        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentCell == null) { 
                // do some stuff?
            }
            else
            {
                List<string> list = new List<string>();
                int index = dataGridView1.CurrentCell.RowIndex;
                foreach (DataGridViewColumn col in dataGridView1.Columns)
                {
                    list.Add(col.HeaderText + ": ");

                }
                for (int i = 0; i < list.Count; i++)
                {

                    DataGridViewRow row = dataGridView1.Rows[index];
                    list[i] = list[i] + row.Cells[i].Value.ToString();

                }
                bindingSource2.DataSource = list;
                ContextBox.DataSource = null;
                ContextBox.DataSource = bindingSource2;

                {

                    //Image_Movie_Product.Load(getImageAddress(getProductIdFromName(list[2])));
                }
            }
        }
        private void Menu_List_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Menu_List.SelectedItem == null)
            {

                //Do some stuff?
            }
            else
            {
                //Set index to row index of item in listbox
                string value = Menu_List.SelectedItem.ToString();
                List<string> list = new List<string>();
                int index = StockTableSearchProdName(value);

                foreach (DataColumn col in StockTable.Columns)
                {
                    list.Add(col.ColumnName + ": ");
                }
                for (int i = 0; i < list.Count; i++)
                {
                    list[i] = list[i] + StockTable.Rows[index][i].ToString();
                }
                bindingSource2.DataSource = list;
                ContextBox.DataSource = null;
                ContextBox.DataSource = bindingSource2;
            }
        }
        private void Stock_List_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Stock_List.SelectedItem == null) {
                //Do some stuff?
            }
            else
            {
                //Set index to row index of item in listbox
                string value = Stock_List.SelectedItem.ToString();
                List<string> list = new List<string>();
                int index = StockTableSearchProdName(value);

                foreach (DataColumn col in StockTable.Columns)
                {
                    list.Add(col.ColumnName + ": ");
                }
                for (int i = 0; i < list.Count; i++)
                {
                    list[i] = list[i] + StockTable.Rows[index][i].ToString();
                }
                bindingSource2.DataSource = list;
                ContextBox.DataSource = null;
                ContextBox.DataSource = bindingSource2;
            }
        }

        private int StockTableSearchProdName(string value)
        {
            for (int j = 0; j < StockTable.Rows.Count; j++)
            {

                if (StockTable.Rows[j][2].ToString() == value)
                {
                    return j;
                }

            }
            return 0;
        }

        private string getImageAddress(int id)
        {
            try
            {
                OleDbConnection connection = Utility.GetOleDBConnection();
                string query = "Select M.ImageURL From Movie M Where M.Movie_Id = " + id + " ;";
                OleDbCommand cmd = new OleDbCommand(query, connection);
                connection.Open();
                DataSet ds = new DataSet();
                OleDbDataAdapter da = new OleDbDataAdapter(cmd);
                da.Fill(ds);
                string address = ds.Tables["Movie"].Rows[0][0].ToString();
                return address;
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Oops, error: " + ex.Message + ex.StackTrace);
            }

            return "";

        }

        private int getProductIdFromName(string name)
        {



            return 2;
        }

          private void ContextBox_SelectedIndexChanged(object sender, EventArgs e)
          {

          }

          private void TicketPricesLb_Click(object sender, EventArgs e)
          {

          }
     }
}
