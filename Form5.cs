using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Npgsql;
using System.IO;
using System.Drawing.Imaging;

namespace StudentApplication
{
    public partial class Form5 : Form
    {
        string sql; int sisa; string unim; string uidcard; string name; string fak;
        NpgsqlConnection conndb;
        NpgsqlCommand command;
        string connstring;
        private void tampil()
        {
            try
            {
                conndb.Open();
            }
            catch { }
            sql = "select * from student";
            command = new NpgsqlCommand(sql, conndb);
            DataSet ds = new DataSet();
            NpgsqlDataAdapter da = new NpgsqlDataAdapter(command);
            da.Fill(ds, "student");
        }
        private void konek()
        {
            connstring = @"Server=localhost;Port=5433;Userid=postgres;Password=meimei123;Database=test_db";
            conndb = new NpgsqlConnection(connstring);
        }
        int jumlah1, jumlah2, jumlah3; int price;
        
        public Form5(string nim, string nama, string uid, string fp, int balance)
        {
            InitializeComponent();
            name = nama; fak = fp;
            string[] namar = nama.Split(' ');
            hello.Text = "Hello " + namar[0] + ", What would you shop for today?";
            curbal.Text = "Rp. " + balance.ToString();
            sisa = balance; unim = nim; uidcard = uid;
            
        }

        private void min1_Click(object sender, EventArgs e)
        {
            jumlah1 = int.Parse(jml1.Text.ToString());
            int a = 0;
            if (jumlah1 > 1)
            {
                jumlah1 = jumlah1 - 1;
                a=1;
            }
            jml1.Text = jumlah1.ToString();
            if (brg1.Checked == true && a==1)
            {
                price = price - 22000;
                harga.Text = "Rp. " + price;
            }
        }

        private void plus1_Click(object sender, EventArgs e)
        {
            jumlah1 = int.Parse(jml1.Text.ToString());
            jumlah1 = jumlah1 + 1;
            jml1.Text = jumlah1.ToString();
            if (brg1.Checked == true)
            {
                price = price + 22000;
                harga.Text = "Rp. " + price;
            }
        }

        private void min2_Click(object sender, EventArgs e)
        {
            jumlah2 = int.Parse(jml2.Text.ToString());
            int a = 0;
            if (jumlah2 > 1)
            {
                jumlah2 = jumlah2 - 1;
                a = 1;
            }
            jml2.Text = jumlah2.ToString();
            if (brg2.Checked == true && a == 1)
            {
                price = price - 35000;
                harga.Text = "Rp. " + price;
            }
        }

        private void plus2_Click(object sender, EventArgs e)
        {
            jumlah2 = int.Parse(jml2.Text.ToString());
            jumlah2++;
            jml2.Text = jumlah2.ToString();
            if (brg2.Checked == true)
            {
                price = price + 35000;
                harga.Text = "Rp. " + price;
            }
        }

        private void min3_Click(object sender, EventArgs e)
        {
            jumlah3 = int.Parse(jml3.Text.ToString());
            int a = 0;
            if (jumlah3 > 1)
            {
                jumlah3 = jumlah3 - 1;
                a = 1;
            }
            jml3.Text = jumlah3.ToString();
            if (brg3.Checked == true && a == 1)
            {
                price = price - 28000;
                harga.Text = "Rp. " + price;
            }
        }

        private void plus3_Click(object sender, EventArgs e)
        {
            jumlah3 = int.Parse(jml3.Text.ToString());
            jumlah3 = jumlah3 + 1;
            jml3.Text = jumlah3.ToString();
            if (brg3.Checked == true)
            {
                price = price + 28000;
                harga.Text = "Rp. " + price;
            }
        }

        private void brg1_CheckedChanged(object sender, EventArgs e)
        {
            if (brg1.Checked == true)
            {
                price = price + (22000 * int.Parse(jml1.Text.ToString()));
            }
            if(brg1.Checked == false)
            {
                price = price - (22000 * int.Parse(jml1.Text.ToString()));
            }
            harga.Text = "Rp. " + price;
        }

        private void brg2_CheckedChanged(object sender, EventArgs e)
        {
            if (brg2.Checked == true)
            {
                price = price + (35000 * int.Parse(jml2.Text.ToString()));
            }
            if (brg2.Checked == false)
            {
                price = price - (35000 * int.Parse(jml2.Text.ToString()));
            }
            harga.Text = "Rp. " + price;
        }

        private void brg3_CheckedChanged(object sender, EventArgs e)
        {
            if (brg3.Checked == true)
            {
                price = price + (28000 * int.Parse(jml3.Text.ToString()));
            }
            if (brg3.Checked == false)
            {
                price = price - (28000 * int.Parse(jml3.Text.ToString()));
            }
            harga.Text = "Rp. " + price;
        }

        private void pictureBox4_Click(object sender, EventArgs e)
        {
            if (sisa < price)
            {
                MessageBox.Show("Not enough balance. Top-up your card first");
            }
            else
            {
                sisa = sisa - price;
                curbal.Text = "Rp. " + sisa;
                konek(); tampil();
                sql = "update student SET balance ='" + sisa + "' WHERE nim='" + unim + "'";
                try
                {
                    command = new NpgsqlCommand(sql, conndb);
                    command.ExecuteNonQuery();
                }
                catch { }
                MessageBox.Show("Shopping success. Your balance now is Rp. "+sisa);
            }
        }

        private void pictureBox6_Click(object sender, EventArgs e)
        {
            this.Hide();
            Form4 frm4 = new Form4(name, unim, fak, uidcard);
            frm4.Show();
        }

      
    }
}
