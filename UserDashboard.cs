using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Oracle.ManagedDataAccess.Client;

namespace SkyReserve
{
    public partial class UserDashboard: Form
    {
        OracleConnection c;
        public string userID;
        public string userName;
        public UserDashboard()
        {
            InitializeComponent();
            userID = UserLogin.currentUserID;
            userName = UserLogin.currentUserName;
            label1.Text = "Welcome, " + userName + "!";
        }
        public void connect()
        {
            c = new OracleConnection("User Id=system;Password=kushagra;Data Source=localhost:1521/xe;");

            try
            {
                c.Open();
                // Connection successful
            }
            catch (Exception ex)
            {
                MessageBox.Show("Database connection failed: " + ex.Message);
            }
        }
        private void UserDashboard_Load(object sender, EventArgs e)
        {
            LoadNotifications();
            LoadBookings();
            LoadAirports();

            // Set date picker to tomorrow by default
            dateTimePicker1.Value = DateTime.Now.AddDays(1);
            dateTimePicker1.MinDate = DateTime.Now;
        }
        private void LoadNotifications()
        {
            connect();
            OracleCommand cmd = c.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "SELECT NotificationID, Message, Timestamp FROM Notification " +
                              "WHERE UserID = :userID OR UserID IS NULL ORDER BY Timestamp DESC";
            cmd.Parameters.Add(new OracleParameter("userID", userID));

            OracleDataAdapter da = new OracleDataAdapter(cmd);
            DataSet ds = new DataSet();
            da.Fill(ds, "Notifications");
            dataGridView1.DataSource = ds.Tables["Notifications"];

            c.Close();
        }
        private void LoadBookings()
        {
            connect();
            OracleCommand cmd = c.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "SELECT b.BookingID, f.FlightID, " +
                              "(SELECT Code FROM Airport WHERE AirportID = f.SourceAirportID) as Source, " +
                              "(SELECT Code FROM Airport WHERE AirportID = f.DestinationAirportID) as Destination, " +
                              "f.DepartureTime, f.ArrivalTime, b.PaymentStatus " +
                              "FROM Booking b JOIN Flight f ON b.FlightID = f.FlightID " +
                              "WHERE b.UserID = :userID ORDER BY f.DepartureTime";
            cmd.Parameters.Add(new OracleParameter("userID", userID));

            OracleDataAdapter da = new OracleDataAdapter(cmd);
            DataSet ds = new DataSet();
            da.Fill(ds, "Bookings");
            dataGridView2.DataSource = ds.Tables["Bookings"];

            c.Close();
        }
        private void LoadAirports()
        {
            connect();
            OracleCommand cmd = c.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "SELECT AirportID, Name || ' (' || Code || ')' as DisplayName FROM Airport ORDER BY Name";

            OracleDataAdapter da = new OracleDataAdapter(cmd);
            DataTable dt = new DataTable();
            da.Fill(dt);

            comboBox1.DisplayMember = "DisplayName";
            comboBox1.ValueMember = "AirportID";
            comboBox1.DataSource = dt.Copy();

            comboBox2.DisplayMember = "DisplayName";
            comboBox2.ValueMember = "AirportID";
            comboBox2.DataSource = dt;

            c.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex == -1 || comboBox2.SelectedIndex == -1)
            {
                MessageBox.Show("Please select both departure and arrival airports.");
                return;
            }

            if (comboBox1.SelectedValue.ToString() == comboBox2.SelectedValue.ToString())
            {
                MessageBox.Show("Departure and arrival airports cannot be the same.");
                return;
            }

            string sourceAirportID = comboBox1.SelectedValue.ToString();
            string destAirportID = comboBox2.SelectedValue.ToString();
            DateTime selectedDate = dateTimePicker1.Value.Date;

            FlightSearch.sourceAirportID = sourceAirportID;
            FlightSearch.destAirportID = destAirportID;
            FlightSearch.selectedDate = selectedDate;

            FlightResults flightResults = new FlightResults();
            flightResults.Show();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            UserLogin.currentUserID = "";
            UserLogin.currentUserName = "";
            Form1 form1 = new Form1();
            form1.Show();
            this.Close();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (dataGridView2.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a booking to cancel.");
                return;
            }

            DialogResult result = MessageBox.Show("Are you sure you want to cancel this booking?", "Confirm Cancellation", MessageBoxButtons.YesNo);
            if (result != DialogResult.Yes)
                return;

            string bookingID = dataGridView2.SelectedRows[0].Cells["BOOKINGID"].Value.ToString();
            string cancelDate = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");

            try
            {
                connect();
                OracleCommand cmd = c.CreateCommand();

                // Step 0: Check if Cancellation already exists for this BookingID
                cmd.CommandText = "SELECT COUNT(*) FROM Cancellation WHERE BookingID = :bookingID";
                cmd.Parameters.Add(new OracleParameter("bookingID", bookingID));
                int count = Convert.ToInt32(cmd.ExecuteScalar());
                cmd.Parameters.Clear();

                if (count > 0)
                {
                    MessageBox.Show("This booking has already been cancelled.");
                    return;
                }

                // Step 1: Get next cancellation ID from sequence
                cmd.CommandText = "SELECT cancellation_seq.NEXTVAL FROM dual";
                decimal nextCancelID = Convert.ToDecimal(cmd.ExecuteScalar());
                cmd.Parameters.Clear();

                // Step 2: Insert into Cancellation
                cmd.CommandText = "INSERT INTO Cancellation (CancellationID, BookingID, CancellationDate, Reason, RefundAmount) " +
                                  "VALUES (:cancelID, :bookingID, TO_DATE(:cancelDate, 'DD-MM-YYYY HH24:MI:SS'), :reason, :refund)";
                cmd.Parameters.Add(new OracleParameter("cancelID", nextCancelID));
                cmd.Parameters.Add(new OracleParameter("bookingID", bookingID));
                cmd.Parameters.Add(new OracleParameter("cancelDate", cancelDate));
                cmd.Parameters.Add(new OracleParameter("reason", "User Cancelled"));
                cmd.Parameters.Add(new OracleParameter("refund", DBNull.Value));
                cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();

                // Step 3: Delete from dependent child tables first
                string[] childTables = { "Ticket", "Payment", "Meal", "Notification", "Emergency" };

                foreach (string table in childTables)
                {
                    cmd.CommandText = $"DELETE FROM {table} WHERE BookingID = :bookingID";
                    cmd.Parameters.Add(new OracleParameter("bookingID", bookingID));
                    cmd.ExecuteNonQuery();
                    cmd.Parameters.Clear();
                }

                // Step 4: Now delete from Booking
                cmd.CommandText = "DELETE FROM Booking WHERE BookingID = :bookingID";
                cmd.Parameters.Add(new OracleParameter("bookingID", bookingID));
                cmd.ExecuteNonQuery();

                MessageBox.Show("Booking cancelled successfully.");
                LoadBookings();  // Refresh UI
            }
            catch (Exception ex)
            {
                MessageBox.Show("Cancellation failed: " + ex.Message);
            }
            finally
            {
                c.Close();
            }
        }
    }
}
