﻿using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;
using System.Diagnostics;
using System.Timers;
using System.IO.Ports;
using System.Data.SqlClient;
using System.Net;
using System.IO.Compression;

namespace Escaner_WindowsFormsApp
{
    public partial class escaner : Form
    {
        VideoCaptureDevice frame;
        FilterInfoCollection Devices;
        System.Timers.Timer t;
        int contador;

        public escaner()
        {
            InitializeComponent();
        }

        void Start_cam() {
            Devices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            frame = new VideoCaptureDevice(Devices[1].MonikerString);
            frame.NewFrame += new AForge.Video.NewFrameEventHandler(NewFrame_event);
            frame.Start();
        }
        String output;
        String path;

        void NewFrame_event(object send, NewFrameEventArgs e) {
            try {
                pictureBox1.Image = (Image)e.Frame.Clone();
                //Cambio de imagen Espejo
                //pictureBox1.Image.RotateFlip(RotateFlipType.Rotate180FlipY);

            } catch (Exception ex) {
                Console.WriteLine("Se produjo un error." + ex);
            }
        }

        private void Escaner_Load(object sender, EventArgs e)
        {

            //Habilitamos los campos
            textNombre.Enabled = false;
            textEdad.Enabled = false;
            textRH.Enabled = false;
            textCorreo.Enabled = false;
            pictureBox1.Enabled = false;
            button3.Enabled = false;
            button2.Enabled = false;
            button4.Enabled = false;

            //Verificamos el puerto asignado
            SqlConnection con = new SqlConnection("Data Source =.; Initial Catalog = escanner3D; Integrated Security = True");
            con.Open();

            //Consulta a la BD
            string query = "SELECT * from tb_port";
            SqlCommand cmd = new SqlCommand(query, con);
            //Obtenemos el valor de la consulta
            SqlDataReader reader = cmd.ExecuteReader();



            while (reader.Read())
            {
                string rutaAsignado = reader["browser_folder"].ToString();
                serialPort1.PortName = reader["number_port"].ToString();
                output = rutaAsignado;
                break;
            }
            
        }

       

        private void Button1_Click(object sender, EventArgs e)
        {
                Start_cam();
        }

        private void PacientesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Hide();
            registro_paciente ventanaRegistro = new registro_paciente();
            ventanaRegistro.Show();
        }

        private void InicioToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Hide();
            Home ventanaHome = new Home();
            ventanaHome.Show();
        }

        private void MenuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void Button4_Click(object sender, EventArgs e)
        {
            frame.Stop();
            pictureBox1.Image = null;
        }


        private void Button3_Click(object sender, EventArgs e)
        {
            //Declaramos el valor que enviaremos al puerto
            string valorA = "a";

            //La rutina a dura 152 segundos
            try
            {
                //Creamos las carpeta
                path = output + "\\" + textCedula.Text;

                if (Directory.Exists(path))
                {
                    MessageBox.Show("Ya existe una carpeta para este paciente");
                    return;
                }
                else
                {
                    // Try to create the directory.
                    DirectoryInfo di = Directory.CreateDirectory(path);
                    //MessageBox.Show("The directory was created successfully at);

                    // Delete the directory.
                    //di.Delete();
                    //Console.WriteLine("The directory was deleted successfully.");

                    contador = 0;

                    if (output != "" && pictureBox1.Image != null)
                    {
                        //Abrimos el puerto
                        serialPort1.Open();
                        //Enviamos el valor al puerto
                        serialPort1.Write(valorA);

                        //Creamos la carpeta en el servidor
                        WebRequest request = WebRequest.Create("ftp://cloud007.solusoftware.com/" + textCedula.Text);
                        request.Method = WebRequestMethods.Ftp.MakeDirectory;
                        request.Credentials = new NetworkCredential("didacoru", "a8q@8F@Z");
                        try
                        {
                            request.GetResponse();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Error" + ex);
                        }

                        //Creamos el timer para la rutina de tomar imagenes
                        t = new System.Timers.Timer();
                        t.Interval = 1000;
                        t.Elapsed += OnTimeEvent;
                        t.Start();
                    }
                }
                serialPort1.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error" + ex);
            }
        }

        private void OnTimeEvent(object sender, System.Timers.ElapsedEventArgs e)
        {
            Invoke(new Action(() =>
            {

                Invoke(new Action(() =>
                {

                    contador += 1;

                    if (contador < 152)
                    {
                        pictureBox1.Image.Save(path + "\\Imagen_" + contador + "_" + textCedula.Text + ".png");

                        //Subimos al servidor las imagenes
                        FtpWebRequest ftpReq = (FtpWebRequest)WebRequest.Create("ftp://cloud007.solusoftware.com/" + textCedula.Text + "\\Imagen_" + contador + "_" + textCedula.Text + ".png");

                        ftpReq.UseBinary = true;
                        ftpReq.Method = WebRequestMethods.Ftp.UploadFile;
                        ftpReq.Credentials = new NetworkCredential("didacoru", "a8q@8F@Z");

                        //byte[] b = File.ReadAllBytes(@"C:\prueba\111\target.zip");
                        byte[] b = File.ReadAllBytes(path + "\\Imagen_" + contador + "_" + textCedula.Text + ".png");
                        ftpReq.ContentLength = b.Length;
                        using (Stream s = ftpReq.GetRequestStream())
                        {
                            s.Write(b, 0, b.Length);
                        }

                        FtpWebResponse ftpResp = (FtpWebResponse)ftpReq.GetResponse();

                        if (ftpResp != null)
                        {
                            if (ftpResp.StatusDescription.StartsWith("226"))
                            {
                                Console.WriteLine("File Uploaded.");
                            }

                        }
                    }
                    else
                    {
                        t.Stop();
                    }
                }));
            }));
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            //Declaramos el valor que enviaremos al puerto
            string valorB = "b";

            //La rutina a dura 33 segundos
            try
            {
                //Creamos las carpeta
                path = output + "\\" + textCedula.Text;

                if (Directory.Exists(path))
                {
                    MessageBox.Show("Ya existe una carpeta para este paciente");
                    return;
                }
                else
                {
                    // Try to create the directory.
                    DirectoryInfo di = Directory.CreateDirectory(path);
                    //MessageBox.Show("The directory was created successfully at);

                    // Delete the directory.
                    //di.Delete();
                    //Console.WriteLine("The directory was deleted successfully.");

                    contador = 0;

                    if (output != "" && pictureBox1.Image != null)
                    {
                        //Abrimos el puerto
                        serialPort1.Open();
                        //Enviamos el valor al puerto
                        serialPort1.Write(valorB);

                        //Creamos la carpeta en el servidor
                        WebRequest request = WebRequest.Create("ftp://cloud007.solusoftware.com/" + textCedula.Text);
                        request.Method = WebRequestMethods.Ftp.MakeDirectory;
                        request.Credentials = new NetworkCredential("didacoru", "a8q@8F@Z");
                        try
                        {
                            request.GetResponse();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Error" + ex);
                        }

                        //Creamos el timer para la rutina de tomar imagenes
                        t = new System.Timers.Timer();
                        t.Interval = 1000;
                        t.Elapsed += OnTimeEvent2;
                        t.Start();
                    }
                }
                serialPort1.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error" + ex);
            }
        }

        private void OnTimeEvent2(object sender, System.Timers.ElapsedEventArgs e)
        {
            Invoke(new Action(() =>
            {
                contador += 1;

                if (contador < 33)
                {
                    pictureBox1.Image.Save(path + "\\Imagen_" + contador + "_" + textCedula.Text + ".png");

                    //Subimos al servidor las imagenes
                    FtpWebRequest ftpReq = (FtpWebRequest)WebRequest.Create("ftp://cloud007.solusoftware.com/" + textCedula.Text  + "\\Imagen_" + contador + "_" + textCedula.Text + ".png");

                    ftpReq.UseBinary = true;
                    ftpReq.Method = WebRequestMethods.Ftp.UploadFile;
                    ftpReq.Credentials = new NetworkCredential("didacoru", "a8q@8F@Z");

                    //byte[] b = File.ReadAllBytes(@"C:\prueba\111\target.zip");
                    byte[] b = File.ReadAllBytes(path + "\\Imagen_" + contador + "_" + textCedula.Text + ".png");
                    ftpReq.ContentLength = b.Length;
                    using (Stream s = ftpReq.GetRequestStream())
                    {
                        s.Write(b, 0, b.Length);
                    }

                    FtpWebResponse ftpResp = (FtpWebResponse)ftpReq.GetResponse();

                    if (ftpResp != null)
                    {
                        if (ftpResp.StatusDescription.StartsWith("226"))
                        {
                            Console.WriteLine("File Uploaded.");
                        }

                    }
                }
                else
                {
                    t.Stop();
                }
            }));
        }

        
        private void Button5_Click(object sender, EventArgs e)
        {
            if (textCedula.Text != "")
            {
                SqlConnection con = new SqlConnection("Data Source =.; Initial Catalog = escanner3D; Integrated Security = True");
                con.Open();

                //Consulta a la BD
                string query = "select * from tb_pacientes where cedula_paciente ='" + textCedula.Text + "'";


                SqlCommand cmd = new SqlCommand(query, con);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    //Declaramos dos variables que van a contener la consulta del usuario y password

                    textNombre.Text = reader[1].ToString();
                    textCedula.Text = reader[2].ToString();
                    textEdad.Text = reader[3].ToString();
                    textRH.Text = reader[4].ToString();
                    textCorreo.Text = reader[5].ToString();

                    //Habilitamos los campos
                    textNombre.Enabled = true;
                    textCedula.Enabled = true;
                    textEdad.Enabled = true;
                    textRH.Enabled = true;
                    textCorreo.Enabled = true;
                    button3.Enabled = true;
                    button2.Enabled = true;
                    button4.Enabled = true;
                    Start_cam();

                }
            }
        }

        private void HistorialPacienteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Hide();
            consultar ventanaConsulta = new consultar();
            ventanaConsulta.Show();
        }

        private void EscanearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Hide();
            escaner ventanaEscaner = new escaner();
            ventanaEscaner.Show();
        }

        private void ConfiguraciónToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Hide();
            configuraciones ventanaConfiguraciones = new configuraciones();
            ventanaConfiguraciones.Show();
        }
    }
}