using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;
using SAPbobsCOM;
using Tie.Helpers;

namespace CamposDeUsuario
{
    public partial class Form1 : Form
    {
        List<CampoDeUsuario> Campos;
        DataTable dtImportados;
        public Form1()
        {
            InitializeComponent();
            Campos = new List<CampoDeUsuario>() { 
            new CampoDeUsuario() {Tipo = BoObjectTypes.oOrders,Text = "Documentos de Marketing",Name = "MktDocs",Lineas = true},
            new CampoDeUsuario() {Tipo = BoObjectTypes.oBusinessPartners,Text = "Socios De Negocio", Name = "SN"},
            new CampoDeUsuario() {Tipo = BoObjectTypes.oItems,Text = "Articulos", Name = "Articulos"},
            new CampoDeUsuario() {Tipo = BoObjectTypes.oItemGroups,Text = "Grupos de Articulos", Name = "GArticulos"},
            new CampoDeUsuario() {Tipo = BoObjectTypes.oUsers,Text = "Usuarios", Name = "Usuarios"},
            };
            dtImportados = new DataTable();
           // dtImportados.Columns.Clear();
            foreach(DataGridViewColumn col in dgvArchivos.Columns)
            {
                dtImportados.Columns.Add(new DataColumn(col.DataPropertyName));
            }
            dgvArchivos.DataSource = dtImportados;

        }
        private void CrearNodosBase()
        {
            tvCampos.Nodes.Clear();
            foreach (CampoDeUsuario c in Campos)
            {
                TreeNode n = new TreeNode()
                {
                    Text = c.Text,
                    Name = c.Name
                };
                if (c.Lineas)
                {
                    n.Nodes.Add(new TreeNode() { Text = "Titulo", Name = NombresDeNodo.Titulo });
                    n.Nodes.Add(new TreeNode() { Text = "Lineas", Name = NombresDeNodo.Linea });
                }
                tvCampos.Nodes.Add(n);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string[] nombres = Enum.GetNames(typeof(BoDataServerTypes));
            Array valores = Enum.GetValues(typeof(BoDataServerTypes));
            DataTable dt = new DataTable();
            dt.Columns.Add(new DataColumn("name"));
            dt.Columns.Add(new DataColumn("value") { DataType = typeof(int) }); ;

            for (int i = 0; i < valores.Length; i++)
            {
                DataRow row = dt.NewRow();
                row["name"] = nombres[i];
                row["value"] = (int)valores.GetValue(i);
                dt.Rows.Add(row);
            }
            cbVersion.DataSource = dt;
            cbVersion.ValueMember = "value";
            cbVersion.DisplayMember = "name";
            cbVersion.SelectedIndex = -1;
            //Valores por defecto
            cbVersion.SelectedValue = (int)BoDataServerTypes.dst_MSSQL2017;
            tbUser.Text = "manager";
            tbPass.Text = "sapo12";
            tbDbName.Text = "SBO_Dicer";
            tbDbUser.Text = "sa";
            tbDbPass.Text = "Passw0rd";
            tbServer.Text = Environment.MachineName;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;
            ProbarConexion();
            Cursor = Cursors.Default;
        }
        private void ProbarConexion()
        {
            try
            {
                Company comp = new Company();
                ConfigurarCompany(ref comp);
                
                comp.Disconnect();
                MessageBox.Show("Conectado existosamente");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
        public bool ConfigurarCompany(ref Company c)
        {
            c.Server = tbServer.Text;
            c.UserName = tbUser.Text;
            c.Password = tbPass.Text;
            c.DbUserName = tbDbUser.Text;
            c.DbPassword = tbDbPass.Text;
            c.DbServerType = (BoDataServerTypes)cbVersion.SelectedValue;
            c.CompanyDB = tbDbName.Text;
            if (c.Connect() != 0)
            {
                MessageBox.Show(c.GetLastErrorDescription());
                return false;
            }
            return true;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;
            Actualizar();
            Cursor = Cursors.Default;
        }

        public void Actualizar()
        {
            CrearNodosBase();
            Company comp = new Company();
            if(!ConfigurarCompany(ref comp)) return;
           
            

            UserFields uf;
            foreach(CampoDeUsuario c in Campos)
            {
                var doc = comp.GetBusinessObject(c.Tipo);
                uf = doc.UserFields;
                if (c.Lineas)
                {
                    TreeNode titulo = tvCampos.Nodes.Find(c.Name, false)[0].Nodes.Find(NombresDeNodo.Titulo, false)[0];
                    TreeNode lineas = tvCampos.Nodes.Find(c.Name, false)[0].Nodes.Find(NombresDeNodo.Linea, false)[0];
                    foreach (Field f in uf.Fields)
                    {
                        TreeNode nodo = new TreeNode()
                        {
                            Tag = $"{f.FieldID};{f.Table};{f.Name}",
                            Text = f.Name + " -> " + f.Description,
                            Name = NombresDeNodo.CamposDeUsuario
                        };
                        titulo.Nodes.Add(nodo);
                    }
                    uf = doc.Lines.UserFields;
                    foreach (Field f in uf.Fields)
                    {
                        TreeNode nodo = new TreeNode()
                        {
                            Tag = $"{f.FieldID};{f.Table};{f.Name}",
                            Text = f.Name + " -> " + f.Description,
                            Name = NombresDeNodo.CamposDeUsuario
                        };
                        lineas.Nodes.Add(nodo);
                    }
                }
                else
                {
                    TreeNode parent = tvCampos.Nodes.Find(c.Name, false)[0];
                    foreach (Field f in uf.Fields)
                    {
                        TreeNode nodo = new TreeNode()
                        {
                            Tag = $"{f.FieldID};{f.Table};{f.Name}",
                            Text = f.Name + " -> " + f.Description,
                            Name = NombresDeNodo.CamposDeUsuario
                        };
                        parent.Nodes.Add(nodo);
                    }
                }
            }

            comp.Disconnect();
            GC.Collect();

        }
        public class CampoDeUsuario
        {
            public BoObjectTypes Tipo;
            public string Text, Name;
            public bool Lineas = false;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;
            Exportar();
            Cursor = Cursors.Default;
        }
        public void Exportar()
        {
            TreeNode[] campos = tvCampos.Nodes.Find(NombresDeNodo.CamposDeUsuario, true);
            if (campos.Length == 0)
                return;
            List<TreeNode> nodosSeleccionados = new List<TreeNode>();
            foreach (TreeNode nodo in campos)
            {
                if (nodo.Checked)
                    nodosSeleccionados.Add(nodo);
            }
            if (nodosSeleccionados.Count == 0) return;
            string rutaGuardado = "";
            using(FolderBrowserDialog f = new FolderBrowserDialog())
            {
                f.Description = "Elija una carpeta para guardar los campos";
                if (f.ShowDialog() != DialogResult.OK) return;
                rutaGuardado = f.SelectedPath + "\\";
            }
            Company comp = new Company();
            if (!ConfigurarCompany(ref comp)) return;
            UserFieldsMD uf = comp.GetBusinessObject(BoObjectTypes.oUserFields);
            int exportados = 0;
            foreach(TreeNode nodo in nodosSeleccionados)
            {
                string[] datos = nodo.Tag.ToString().Split(';');
                
                uf.GetByKey(datos[1], int.Parse(datos[0]));
                uf.SaveXML(rutaGuardado + datos[2] + ".xml");
                exportados++;
            }
            MessageBox.Show($"Se exportaron correctamente {exportados} campos!");
            comp.Disconnect();
            GC.Collect();
            
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;
            BuscarArchivos();
            Cursor = Cursors.Default;
        }
        public void BuscarArchivos()
        {
            string[] archivos;
            using(OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Multiselect = true;
                ofd.Title = "Seleccione los campos de usuario";
                ofd.Filter = "Archivos xml (*.xml)|*.XML";
                if (ofd.ShowDialog() != DialogResult.OK) return;
                archivos = ofd.FileNames;
            }
            Limpiar();
            Company comp = new Company();
            if (!ConfigurarCompany(ref comp)) return;
            UserFieldsMD uf; 
            DataRow row;
            int leidos = 0;
            foreach (string archivo in archivos)
            {
                try
                {
                    uf = (UserFieldsMD)comp.GetBusinessObjectFromXML(archivo, 0);
                    row = dtImportados.NewRow();
                    row[colNombre.DataPropertyName] = uf.Name;
                    row[colDescripcion.DataPropertyName] = uf.Description;
                    row[colRuta.DataPropertyName] = archivo;
                    dtImportados.Rows.Add(row);
                    leidos++;
                }
                catch (Exception)
                {

                   
                }
            }
            MessageBox.Show($"Se leyeron correctamente {leidos} campos");
            comp.Disconnect();
            GC.Collect();
        }

        public void ImportarASap()
        {
            if (dtImportados.Rows.Count == 0) return;
            Company comp = new Company();
            if(!ConfigurarCompany(ref comp)) return;
            UserFieldsMD uf;
            int importados = 0;
            foreach(DataRow row in dtImportados.Rows)
            {
                try
                {
                    uf = comp.GetBusinessObjectFromXML(row[colRuta.DataPropertyName].ToString(), 0);
                    uf.Name = row[colNombre.DataPropertyName].ToString();
                    uf.Description = row[colDescripcion.DataPropertyName].ToString();
                    if(uf.Add() != 0)
                        row[colEstatus.DataPropertyName] = comp.GetLastErrorDescription();
                    else
                    {
                        row[colEstatus.DataPropertyName] = "Importado Correctamente";
                        importados++;
                    }
                    
                    
                }
                catch (Exception ex)
                {
                    row[colEstatus.DataPropertyName] = ex.Message;
                }
            }
            MessageBox.Show($"Se importaron correctamente {importados} campos");
            comp.Disconnect();
            GC.Collect();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;
            ImportarASap();
            Cursor = Cursors.Default;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            Limpiar();
        }
        public void Limpiar()
        {
            dtImportados.Rows.Clear();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            CargarTablasDeUsuario();
        }
        private DataTable executeQuery(string query)
        {
            DataTable dt = new DataTable();
            SqlConnectionStringBuilder b = new SqlConnectionStringBuilder();
            b.InitialCatalog = tbDbName.Text;
            b.UserID = tbDbUser.Text;
            b.Password = tbDbPass.Text;
            b.DataSource = tbServer.Text;
            using (SqlConnection conn = new SqlConnection(b.ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    sda.Fill(dt);
                GC.Collect();
            }
            return dt;
        }
        private void CargarTablasDeUsuario()
        {
            Cursor = Cursors.WaitCursor;
            try
            {
                tvTablas.Nodes.Clear();
                string q = "select TableName,Descr from OUTB";

                foreach (DataRow r in executeQuery(q).Rows)
                {
                    tvTablas.Nodes.Add(new TreeNode()
                    {
                        Text = $"{r["TableName"]}->{r["Descr"]}",
                        Tag = r["TableName"]
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            Cursor = Cursors.Default;
            
        }

        private void button8_Click(object sender, EventArgs e)
        {
            ExportarTablas();
        }
        private void ExportarTablas()
        {
            try
            {
                List<string> tablasSeleccionadas = new List<string>();
                foreach (TreeNode n in tvTablas.Nodes)
                {
                    if (n.Checked)
                        tablasSeleccionadas.Add(n.Tag.ToString());
                }
                if (tablasSeleccionadas.Count == 0) throw new Exception("No se selecconó nada");
                string rutaGuardado = "";
                using (FolderBrowserDialog f = new FolderBrowserDialog())
                {
                    f.Description = "Elija una carpeta para guardar las tablas";
                    if (f.ShowDialog() != DialogResult.OK) return;
                    rutaGuardado = f.SelectedPath + "\\";
                }
                Cursor = Cursors.WaitCursor;
                Company comp = new Company();
                if (!ConfigurarCompany(ref comp)) throw new Exception(comp.GetLastErrorDescription());
                UserTablesMD tabla = comp.GetBusinessObject(BoObjectTypes.oUserTables);
                
            
                foreach (string t in tablasSeleccionadas)
                {
                    tabla.GetByKey(t);
                    tabla.SaveXML($"{rutaGuardado}{t}.xml");
                    if (chkInclirDatos.Checked)
                    {
                        Utils.WriteStringToFile(
                            obenerDatosDeTabla(t),
                            $"{rutaGuardado}{t}_data.txt",
                            true
                            );
                    } 
                    
                }
                comp.Disconnect();
                MessageBox.Show("Proceso terminado");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            Cursor = Cursors.Default;
        }
        private string obenerDatosDeTabla(string nombreTabla) {
            string data = "";
            try
            {
                
                string query = $"select * from [@{nombreTabla}]";
                DataTable dt = executeQuery(query);
                if (dt.Rows.Count == 0) return "";
                List<string> columnas = new List<string>();
                string cols = "";
                 foreach (DataColumn c in dt.Columns)
                {
                    cols += $"[{c.ColumnName}],";
                }
                cols = cols.Trim(',');
                
                data = $"INSERT INTO [@{nombreTabla}]({cols}) VALUES ";
                string values;
                foreach (DataRow r in dt.Rows)
                {
                    values = "";
                    foreach(DataColumn c in dt.Columns)
                    {
                        values += $"'{r[c]}',";
                    }
                    values = values.Trim(',');
                    data += $"({values}),";
                }
                data = data.Trim(',');
                
            }
            catch (Exception)
            {

             
            }
            return data;

        }

        private void button11_Click(object sender, EventArgs e)
        {

        }
        private void buscarArchivosTablas()
        {
            string[] archivos;
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Multiselect = true;
                ofd.Title = "Seleccione las tablas";
                ofd.Filter = "Archivos xml (*.xml)|*.xml|Archivos de texto (*.txt)|*.txt";
                if (ofd.ShowDialog() != DialogResult.OK) return;
                archivos = ofd.FileNames;
            }
            limpiarTablas();
            Company comp = new Company();
            if (!ConfigurarCompany(ref comp)) return;
            UserTablesMD t;

            int i;
            int c = 0;
            foreach (string archivo in archivos)
            {
                try
                {
                    i = dgvTablas.Rows.Add();
                    if (Path.GetExtension(archivo) == "xml")
                    {
                        t = comp.GetBusinessObjectFromXML(archivo, 0);
                        dgvTablas.Rows[i].Cells[colNombreTabla.Name].Value = t.TableName;
                        dgvTablas.Rows[i].Cells[colRutaTabla.Name].Value = archivo;
                        c++;
                    }else
                    {
                        dgvTablas.Rows[i].Cells[colNombreTabla.Name].Value = Path.GetFileName(archivo);
                        dgvTablas.Rows[i].Cells[colRutaTabla.Name].Value = archivo;
                    }
                    
                }
                catch (Exception)
                {


                }
            }
            MessageBox.Show($"Se leyeron correctamente {c} Tablas");
            comp.Disconnect();
            GC.Collect();
        }
        private void limpiarTablas()
        {
            dgvTablas.Rows.Clear();
            dgvTablas.Refresh();
        }
        private void button10_Click(object sender, EventArgs e)
        {

        }
    }

    public static class NombresDeNodo
    {
        public static readonly string
            Titulo = "Titulo",
            Linea = "Lineas",
            CamposDeUsuario = "cdu";



    }

    
}
