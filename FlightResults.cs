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

    public partial class FlightResults: Form
    {
        OracleConnection c;
        public static string selectedFlightID = "";

        public FlightResults()
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

        private void FlightResults_Load(object sender, EventArgs e)
        {
            LoadFlights();
        }
        private void LoadFlights()
        {
            connect();
            OracleCommand cmd = c.CreateCommand();
            cmd.CommandType = CommandType.Text;

            cmd.CommandText = "SELECT f.FlightID, " +
                             "(SELECT Name FROM Airport WHERE AirportID = f.SourceAirportID) as DepartureAirport, " +
                             "(SELECT Name FROM Airport WHERE AirportID = f.DestinationAirportID) as ArrivalAirport, " +
                             "TO_CHAR(f.DepartureTime, 'DD-MON-YYYY HH24:MI') as DepartureTime, " +
                             "TO_CHAR(f.ArrivalTime, 'DD-MON-YYYY HH24:MI') as ArrivalTime, " +
                             "(f.SeatCapacity - (SELECT COUNT(*) FROM Booking WHERE FlightID = f.FlightID)) as AvailableSeats " +
                             "FROM Flight f " +
                             "WHERE f.SourceAirportID = :sourceID AND f.DestinationAirportID = :destID " +
                             "AND TRUNC(f.DepartureTime) = :depDate " +
                             "ORDER BY f.DepartureTime";

            cmd.Parameters.Add(new OracleParameter("sourceID", FlightSearch.sourceAirportID));
            cmd.Parameters.Add(new OracleParameter("destID", FlightSearch.destAirportID));
            cmd.Parameters.Add(new OracleParameter("depDate", FlightSearch.selectedDate));

            OracleDataAdapter da = new OracleDataAdapter(cmd);
            DataSet ds = new DataSet();
            da.Fill(ds, "Flights");
            dataGridView1.DataSource = ds.Tables["Flights"];

            c.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow == null)
            {
                MessageBox.Show("Please select a flight first.");
                return;
            }

            selectedFlightID = dataGridView1.CurrentRow.Cells["FlightID"].Value.ToString();

            BookingDetails bookingDetails = new BookingDetails();
            bookingDetails.WindowState = FormWindowState.Maximized;
            bookingDetails.Show();
            this.Hide();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}
