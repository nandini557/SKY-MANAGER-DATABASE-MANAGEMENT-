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
    public partial class PaymentForm: Form
    {
        OracleConnection c;
        public int newBookingID;
        public PaymentForm()
        {
            InitializeComponent();
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
        private void PaymentForm_Load(object sender, EventArgs e)
        {
            label7.Text = "₹" + BookingDetails.totalAmount.ToString("N2");
            comboBox1.Items.Clear();
            comboBox1.Items.Add("Credit Card");
            comboBox1.Items.Add("Debit Card");
            comboBox1.Items.Add("UPI");
            comboBox1.Items.Add("Net Banking");
            comboBox1.Items.Add("Wallet");

        }
        private bool ValidatePaymentDetails()
        {
            // Basic validation
            if (string.IsNullOrEmpty(textBox1.Text) || textBox1.Text.Length < 16)
            {
                MessageBox.Show("Please enter a valid 16-digit card number.");
                return false;
            }

            if (string.IsNullOrEmpty(textBox2.Text))
            {
                MessageBox.Show("Please enter the card holder name.");
                return false;
            }

            if (!maskedTextBox1.MaskCompleted)
            {
                MessageBox.Show("Please enter a valid expiry date (MM/YY).");
                return false;
            }

            if (!maskedTextBox2.MaskCompleted)
            {
                MessageBox.Show("Please enter a valid 3-digit CVV.");
                return false;
            }

            return true;
        }
        private int CreateBooking()
        {
            connect();

            // First get the next booking ID
            OracleCommand idCmd = c.CreateCommand();
            idCmd.CommandType = CommandType.Text;
            idCmd.CommandText = "SELECT MAX(BookingID) + 1 FROM Booking";
            object result = idCmd.ExecuteScalar();
            int bookingID = result != DBNull.Value ? Convert.ToInt32(result) : 1005;

            // Create the booking
            OracleCommand cmd = c.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "INSERT INTO Booking (BookingID, UserID, FlightID, SeatNo, PaymentStatus) " +
                             "VALUES (:bookingID, :userID, :flightID, :seatNo, 'Processing')";

            cmd.Parameters.Add(new OracleParameter("bookingID", bookingID));
            cmd.Parameters.Add(new OracleParameter("userID", UserLogin.currentUserID));
            cmd.Parameters.Add(new OracleParameter("flightID", FlightResults.selectedFlightID));

            // Assign a random seat number - simplified for demo
            Random random = new Random();
            int seatNo = random.Next(1, 100);
            cmd.Parameters.Add(new OracleParameter("seatNo", seatNo));

            cmd.ExecuteNonQuery();

            c.Close();
            return bookingID;
        }
        private void CreatePayment(int bookingID)
        {
            connect();

            // Get next transaction ID
            OracleCommand idCmd = c.CreateCommand();
            idCmd.CommandType = CommandType.Text;
            idCmd.CommandText = "SELECT MAX(TransactionID) + 1 FROM Payment";
            object result = idCmd.ExecuteScalar();
            int transactionID = result != DBNull.Value ? Convert.ToInt32(result) : 3005;

            // Create the payment record
            OracleCommand cmd = c.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "INSERT INTO Payment (TransactionID, BookingID, Amount, Method, Timestamp) " +
                             "VALUES (:transactionID, :bookingID, :amount, :method, SYSDATE)";

            cmd.Parameters.Add(new OracleParameter("transactionID", transactionID));
            cmd.Parameters.Add(new OracleParameter("bookingID", bookingID));
            cmd.Parameters.Add(new OracleParameter("amount", BookingDetails.totalAmount));
            cmd.Parameters.Add(new OracleParameter("method", comboBox1.Text));

            cmd.ExecuteNonQuery();

            c.Close();
        }
        private void CreateTicket(int bookingID)
        {
            connect();

            // Get next ticket ID
            OracleCommand idCmd = c.CreateCommand();
            idCmd.CommandType = CommandType.Text;
            idCmd.CommandText = "SELECT MAX(TicketID) + 1 FROM Ticket";
            object result = idCmd.ExecuteScalar();
            int ticketID = result != DBNull.Value ? Convert.ToInt32(result) : 2005;

            // Generate ticket number
            string ticketNumber = "TKT" + DateTime.Now.ToString("yyyyMMdd") + bookingID.ToString();

            // Create the ticket record
            OracleCommand cmd = c.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "INSERT INTO Ticket (TicketID, BookingID, TicketNumber, IssueDate) " +
                             "VALUES (:ticketID, :bookingID, :ticketNumber, SYSDATE)";

            cmd.Parameters.Add(new OracleParameter("ticketID", ticketID));
            cmd.Parameters.Add(new OracleParameter("bookingID", bookingID));
            cmd.Parameters.Add(new OracleParameter("ticketNumber", ticketNumber));

            cmd.ExecuteNonQuery();

            c.Close();
        }

        private void UpdateBookingStatus(int bookingID)
        {
            connect();

            OracleCommand cmd = c.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "UPDATE Booking SET PaymentStatus = 'Paid' WHERE BookingID = :bookingID";
            cmd.Parameters.Add(new OracleParameter("bookingID", bookingID));

            cmd.ExecuteNonQuery();

            c.Close();
        }
        private void btnPay_Click(object sender, EventArgs e)
        {
            if (!ValidatePaymentDetails())
            {
                return;
            }

            try
            {
                // Create booking record
                newBookingID = CreateBooking();

                // Process payment
                CreatePayment(newBookingID);

                // Create ticket
                CreateTicket(newBookingID);

                // Update booking status to Paid
                UpdateBookingStatus(newBookingID);

                // Show confirmation
                BookingConfirmation confirmation = new BookingConfirmation();
                confirmation.BookingID = newBookingID;
                confirmation.WindowState = FormWindowState.Maximized;
                confirmation.Show();
                this.Hide();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Payment processing error: " + ex.Message);
            }
        }
        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
            BookingDetails bookingDetails = new BookingDetails();
            bookingDetails.Show();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!ValidatePaymentDetails())
            {
                return;
            }

            try
            {
                // Create booking record
                newBookingID = CreateBooking();

                // Process payment
                CreatePayment(newBookingID);

                // Create ticket
                CreateTicket(newBookingID);

                // Update booking status to Paid
                UpdateBookingStatus(newBookingID);

                // Show confirmation
                BookingConfirmation confirmation = new BookingConfirmation();
                confirmation.BookingID = newBookingID;
                confirmation.WindowState = FormWindowState.Maximized;
                confirmation.Show();
                this.Hide();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Payment processing error: " + ex.Message);
            }
        }
    }
}
