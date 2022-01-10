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
    public partial class Form2 : Form
    {
        string sql;
        NpgsqlConnection conndb;
        NpgsqlCommand command;
        string connstring;
        string sourcepath;
        string input; string tmpStr;
        byte[] photoByteArray;

        public Form2()
        {
            InitializeComponent();
            konek();
            tampil();
        }

        public int retCode, hContext, hCard, Protocol;
        public bool connActive = false; string uid = "";
        public bool autoDet;
        public byte[] SendBuff = new byte[263];
        public byte[] RecvBuff = new byte[263];

        public int SendLen, RecvLen, nBytesRet, reqType, Aprotocol, dwProtocol, cbPciLength;
        public ModWinsCard.SCARD_READERSTATE RdrState;
        public ModWinsCard.SCARD_IO_REQUEST pioSendRequest;

        private void displayOut(int errType, int retVal, string PrintText)
        {
            switch (errType)
            {
                case 0:
                    break;
                case 1:
                    PrintText = ModWinsCard.GetScardErrMsg(retVal);
                    break;
                case 2:
                    PrintText = "<" + PrintText;
                    break;
                case 3:
                    PrintText = "-->" + PrintText;
                    break;
            }
            pesan.Items.Add(PrintText);
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog();
            open.Filter = "Image Files(*.jpg; *.jpeg; *.gif; *.bmp; *.png)|*.jpg; *.jpeg; *.gif; *.bmp; *.png";
            if (open.ShowDialog() == DialogResult.OK)
            {
                pictureBox1.Image = new Bitmap(open.FileName);
                pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                sourcepath = open.FileName;
                photoByteArray = File.ReadAllBytes(open.FileName);
                input = BitConverter.ToString(photoByteArray, 0, (int)photoByteArray.Length).Replace("-", "");
            }
        }

        private void GetUID()
        {
            //Command APDU = CLA INS P1 P2 Le
            //               FF  CA  00 00 00
            SendBuff[0] = 0xFF;//cla
            SendBuff[1] = 0xCA;//ins
            SendBuff[2] = 0x00;
            SendBuff[3] = 0x00;
            SendBuff[4] = 0x00;

            SendLen = 5;
            RecvLen = 255;
            SendAPDUandDisplay(2);
        }
       

        private void add()
        {
            sql = "insert into student values ('" + textBox1.Text + "','" + textBox2.Text + "','" + textBox3.Text + "', '" + input + "', '" + uid + "', 100000)";
            command = new NpgsqlCommand(sql, conndb);
            command.ExecuteNonQuery();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Hide();
            Form1 frm1 = new Form1();
            frm1.Show();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (textBox1.Text == "" || textBox2.Text == "" || textBox3.Text == "" || pictureBox1.Image == null)
                {
                    MessageBox.Show("Silahkan isi semua form");
                    return;
                }
                else
                {
                    initia();
                    String Str = textBox1.Text + "#" + textBox2.Text + "#" + textBox3.Text + "*";
                    int jml_blok;
                    int modulo = Str.Length % 16;
                    if (modulo == 0)
                    {
                        jml_blok = Str.Length / 16;
                    }
                    else
                    {
                        jml_blok = Str.Length / 16 + 1;
                    }
                    int selisih = 16 - modulo;

                    tmpStr = "";
                    int temp = 8;
                    for (int i = 0; i < jml_blok; i++)
                    {
                        if ((temp + 1) % 4 == 0)
                        {
                            temp = temp + 1;
                        }
                        if ((temp % 4 == 0))
                        {
                            auten(temp);
                        }
                        if (modulo != 0 && i == jml_blok - 1)
                        {
                            tmpStr = Str.Substring(i * 16, modulo);
                        }
                        else
                        {
                            tmpStr = Str.Substring(i * 16, 16);
                        }

                        ClearBuffers();
                        SendBuff[0] = 0xFF;                                     // CLA
                        SendBuff[1] = 0xD6;                                     // INS
                        SendBuff[2] = 0x00;
                        SendBuff[3] = (byte)temp;                         // P2 : Starting Block No.
                        SendBuff[4] = (byte)16; 

                        for (int indx = 0; indx <= (tmpStr).Length - 1; indx++)
                        {
                            SendBuff[indx + 5] = (byte)tmpStr[indx];
                        }
                        SendLen = SendBuff[4] + 5;
                        RecvLen = 0x02;
                        retCode = SendAPDUandDisplay(2);

                        if (retCode != ModWinsCard.SCARD_S_SUCCESS)
                        {
                            return;
                        }
                        temp += 1;
                    }
                    GetUID();
                    uid = uid.Substring(0, 8).Trim();
                    add();
                    MessageBox.Show("Student Card has been registered");
                }
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show("Error: NIM sudah terdaftar");
            }
        }
        private void tampil()
        {
            conndb.Open();
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

        private void initia()
        {
            int indx;
            int pcchReaders = 0;
            string rName = "";

            // 1. Establish Context
            retCode = ModWinsCard.SCardEstablishContext(ModWinsCard.SCARD_SCOPE_USER, 0, 0, ref hContext);

            if (retCode != ModWinsCard.SCARD_S_SUCCESS)
            {
                displayOut(1, retCode, "");
                return;

            }

            // 2. List PC/SC card readers installed in the system
            retCode = ModWinsCard.SCardListReaders(this.hContext, null, null, ref pcchReaders);
            if (retCode != ModWinsCard.SCARD_S_SUCCESS)
            {

                displayOut(1, retCode, "");

                return;
            }

            conn();

            byte[] ReadersList = new byte[pcchReaders];

            // Fill reader list
            retCode = ModWinsCard.SCardListReaders(this.hContext, null, ReadersList, ref pcchReaders);

            if (retCode != ModWinsCard.SCARD_S_SUCCESS)
            {
                pesan.Items.Add("SCardListReaders Error: " + ModWinsCard.GetScardErrMsg(retCode));
                pesan.SelectedIndex = pesan.Items.Count - 1;
                return;
            }
            else
            {
                displayOut(0, 0, " ");
            }

            rName = "";
            indx = 0;

            // Convert reader buffer to string
            while (ReadersList[indx] != 0)
            {
                while (ReadersList[indx] != 0)
                {
                    rName = rName + (char)ReadersList[indx];
                    indx = indx + 1;
                }
                // Add reader name to list
                reader.Items.Add(rName);
                rName = "";
                indx = indx + 1;
            }

            if (reader.Items.Count > 0)
            {
                reader.SelectedIndex = 1;
            }
            indx = 1;
            return;
        }

        private void conn()
        {
            retCode = ModWinsCard.SCardDisconnect(hCard, ModWinsCard.SCARD_UNPOWER_CARD);
            if (connActive)
            {

                retCode = ModWinsCard.SCardDisconnect(hCard, ModWinsCard.SCARD_UNPOWER_CARD);

            }

            // Shared Connection
            retCode = ModWinsCard.SCardConnect(hContext, reader.Text, ModWinsCard.SCARD_SHARE_EXCLUSIVE, ModWinsCard.SCARD_PROTOCOL_T1, ref hCard, ref Protocol);
            if (retCode == ModWinsCard.SCARD_S_SUCCESS)
            {

                displayOut(0, 0, "Successful connection to " + reader.Text);
                connActive = true;
            }

            else
            {

                displayOut(0, 0, ModWinsCard.GetScardErrMsg(retCode));
                MessageBox.Show("Sorry. Please insert your card first");
                return;

            }


            
        }

        private void ClearBuffers()
        {
            long indx;

            for (indx = 0; indx <= 262; indx++)
            {
                RecvBuff[indx] = 0;
                SendBuff[indx] = 0;
            }

        }
        private int SendAPDUandDisplay(int reqType)
        {
            int indx;
            string tmpStr;

            pioSendRequest.dwProtocol = Aprotocol;
            pioSendRequest.cbPciLength = 8;

            // Display Apdu In
            tmpStr = "";
            for (indx = 0; indx <= SendLen - 1; indx++)
            {

                tmpStr = tmpStr + " " + string.Format("{0:X2}", SendBuff[indx]);

            }

            displayOut(2, 0, tmpStr);
            retCode = ModWinsCard.SCardTransmit(hCard, ref pioSendRequest, ref SendBuff[0], SendLen, ref pioSendRequest, ref RecvBuff[0], ref RecvLen);

            if (retCode != ModWinsCard.SCARD_S_SUCCESS)
            {
                displayOut(1, retCode, "");
                return retCode;
            }

            else
            {
                tmpStr = "";
                switch (reqType)
                {

                    case 0:
                        for (indx = (RecvLen - 2); indx <= (RecvLen - 1); indx++)
                        {

                            tmpStr = tmpStr + " " + string.Format("{0:X2}", RecvBuff[indx]);
                        }


                        if ((tmpStr).Trim() != "90 00")
                        {
                            displayOut(4, 0, "Return bytes are not acceptable.");
                        }
                        break;

                    case 1:

                        for (indx = (RecvLen - 2); indx <= (RecvLen - 1); indx++)
                        {
                            tmpStr = tmpStr + string.Format("{0:X2}", RecvBuff[indx]);
                        }


                        if (tmpStr.Trim() != "90 00")
                        {

                            tmpStr = tmpStr + " " + string.Format("{0:X2}", RecvBuff[indx]);
                        }

                        else
                        {

                            tmpStr = "ATR : ";
                            for (indx = 0; indx <= (RecvLen - 3); indx++)
                            {

                                tmpStr = tmpStr + " " + string.Format("{0:X2}", RecvBuff[indx]);

                            }

                        }

                        break;

                    case 2:

                        for (indx = 0; indx <= (RecvLen - 1); indx++)
                        {

                            tmpStr = tmpStr + "" + string.Format("{0:X2}", RecvBuff[indx]);

                        }
                        uid = tmpStr;
                       
                        break;

                }

                displayOut(3, 0, tmpStr.Trim());

            }
            return retCode;
        }
        private void auten(int a)
        {
            ClearBuffers();
            SendBuff[0] = 0xFF;                      // CLA
            SendBuff[1] = 0x82;                      // INS
            SendBuff[2] = 0x20;                 // P1 : Non volatile memory
            SendBuff[3] = byte.Parse("1", System.Globalization.NumberStyles.HexNumber);    // P2 : Memory location
            SendBuff[4] = 0x06;                     // P3
            for (int k = 5; k < 11; k++)
            {
                SendBuff[k] = byte.Parse("ff", System.Globalization.NumberStyles.HexNumber); // Key 1-6 value
            }

            SendLen = 11;
            RecvLen = 2;
            retCode = SendAPDUandDisplay(0);
            if (retCode != ModWinsCard.SCARD_S_SUCCESS)
            {
                return;
            }

            ClearBuffers();
            SendBuff[0] = 0xFF;                                       // CLA
            SendBuff[2] = 0x00;                                       // P1: same for all source types
            SendBuff[1] = 0x86;                                       // INS: for stored key input
            SendBuff[3] = 0x00;                                       // P2: for stored key input
            SendBuff[4] = 0x05;                                       // P3: for stored key input
            SendBuff[5] = 0x01;                                       // Byte 1: version number
            SendBuff[6] = 0x00;                                       // Byte 2
            SendBuff[7] = (byte)a;  //block number : 0-63             // Byte 3: sectore no. for stored key input
            SendBuff[8] = 0x60;                                       // Byte 4 : Key B for stored key input
            SendBuff[9] = (byte)1;  //index key reader            // Byte 5 : Session key for non-volatile memory
            SendLen = 0x0A;
            RecvLen = 0x02;
            retCode = SendAPDUandDisplay(0);
            if (retCode != ModWinsCard.SCARD_S_SUCCESS)
            {
                return;
            }
        }
    }
}
