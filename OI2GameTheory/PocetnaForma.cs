﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OI2GameTheory
{
    public partial class PocetnaForma : Form
    {
        private SpremanjeUnosa uneseniDobiciGubitci = null;
        public PocetnaForma()
        {
            InitializeComponent();
        }

        private void btnGenerirajMatricu_Click(object sender, EventArgs e)
        {
            if(string.IsNullOrEmpty(txtStrA.Text) || string.IsNullOrEmpty(txtStrB.Text))
            {
                MessageBox.Show("Unesite broj strategija svakog igrača!", "Pažnja", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                try
                {
                    int brojStrategijaA = int.Parse(txtStrA.Text);
                    int brojStrategijaB = int.Parse(txtStrB.Text);

                    //crtanje matrice
                    CrtanjeMatrice matrica = new CrtanjeMatrice(brojStrategijaA, brojStrategijaB);
                    dgvMatrica.DataSource = matrica.NacrtajMatricu();

                    if(dgvMatrica.Rows.Count > 0 && dgvMatrica.Columns.Count > 0)
                    {
                        btnSimplex.Enabled = true;
                        btnModelZadatka.Enabled = true;
                    }

                    //za izgled tablice - tako da je cijela popunjena
                    foreach (DataGridViewRow red in dgvMatrica.Rows)
                    {
                        red.Height = (dgvMatrica.ClientRectangle.Height - dgvMatrica.ColumnHeadersHeight) / dgvMatrica.Rows.Count;
                    }

                    //isključena modifikacija prvog stupca
                    dgvMatrica.Columns[0].ReadOnly = true;

                    foreach (DataGridViewColumn stupac in dgvMatrica.Columns)
                    {
                        stupac.SortMode = DataGridViewColumnSortMode.NotSortable;
                    }
                }
                catch
                {
                    MessageBox.Show("Unesite cijele brojeve!", "Pažnja", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }
        private SimplexForma formaSimplexMetode;

        private void btnSimplex_Click(object sender, EventArgs e)
        {
           try
           {
                if(rbIgracA.Checked == true)
                {
                    uneseniDobiciGubitci = new SpremanjeUnosa(dgvMatrica);
                
                    //provjera postojanja sedla
                    SedloDominacija provjeraSedla = new SedloDominacija(uneseniDobiciGubitci);

                    Tuple<bool, int, int> postojanjeSedla = provjeraSedla.ProvjeriSedlo();
                    bool postojiSedlo = postojanjeSedla.Item1;
                    int rezultatIgre = postojanjeSedla.Item2;

                    if (postojiSedlo)
                    {
                        MessageBox.Show("Postoji sedlo!\nVrijednost ove igre iznosi: " + rezultatIgre, "Kraj igre!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        ProtuprirodnaKontradiktornaIgra protuprirodnost = new ProtuprirodnaKontradiktornaIgra(new SpremanjeUnosa(dgvMatrica));
                        int vrstaIgre = protuprirodnost.ProvjeriProtuprirodnost();
                        if (vrstaIgre == 0)
                        {
                            provjeraSedla.ukloniDominantneStrategije(); //provjera dal postoje dominantne i duplikatne strategije te ih eliminira
                            /*
                            string uklonjeneStrategije = provjeraSedla.IspisUklonjenihStrategijaIgracaA();
                            if (!String.IsNullOrEmpty(uklonjeneStrategije))
                                MessageBox.Show(uklonjeneStrategije);
                            */

                            Tuple<bool, int, int> postojanjeSedlaIntern = provjeraSedla.ProvjeriSedlo();
                            bool postojiSedloIntern = postojanjeSedlaIntern.Item1;
                            int rezultatIgreIntern = postojanjeSedlaIntern.Item2;

                            if (postojiSedloIntern)
                            {
                                MessageBox.Show("Postoji sedlo nakon uklanjanja dominantnih strategija!\nVrijednost ove igre iznosi: " + rezultatIgreIntern, "Kraj igre!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            else
                            {
                                //simplex metoda 
                                SimplexKalkulatorA smplxCalcMI = new SimplexKalkulatorA(provjeraSedla.uneseniPodaci, postojanjeSedlaIntern.Item3); //šalju se strategije bez onih dominantnih
                                formaSimplexMetode = new SimplexForma(smplxCalcMI.SimplexTabliceRazlomci, smplxCalcMI.Zakljucak, smplxCalcMI.indexiVodecihStupaca, smplxCalcMI.indexiVodecihRedaka, smplxCalcMI.brojRedaka, smplxCalcMI.brojStupaca, smplxCalcMI.postupakIzracuna);
                                formaSimplexMetode.ShowDialog();
                            }
                        }
                        else if (vrstaIgre == 1) 
                        {
                            MessageBox.Show("Unesena je protuprirodna igra!\nNe uklanjam dominantne strategije.");

                            provjeraSedla.ukloniDuplikatneStrategije();
                            /*
                            string uklonjeneStrategije = provjeraSedla.IspisUklonjenihDuplikatnihA();
                            if (!String.IsNullOrEmpty(uklonjeneStrategije))
                                MessageBox.Show(uklonjeneStrategije);
                            */
                            SimplexKalkulatorA smplxCalcPI = new SimplexKalkulatorA(provjeraSedla.uneseniPodaci, provjeraSedla.ProvjeriSedlo().Item3);

                            formaSimplexMetode = new SimplexForma(smplxCalcPI.SimplexTabliceRazlomci, smplxCalcPI.Zakljucak, smplxCalcPI.indexiVodecihStupaca, smplxCalcPI.indexiVodecihRedaka, smplxCalcPI.brojRedaka, smplxCalcPI.brojStupaca, smplxCalcPI.postupakIzracuna);
                            formaSimplexMetode.ShowDialog();
                        }
                        else//kontradiktorna
                        {
                            MessageBox.Show("Unesena je kontradiktorna igra!\nNe uklanjam dominantne strategije.");//kontradiktorna nastaje nakon uklanjanja strategija svođenjem jednog igrača na samo 1 strategiju
                            
                            provjeraSedla.ukloniDuplikatneStrategije();
                            /*
                            string uklonjeneStrategije = provjeraSedla.IspisUklonjenihDuplikatnihA();
                            if (!String.IsNullOrEmpty(uklonjeneStrategije))
                                MessageBox.Show(uklonjeneStrategije);
                            */                             
                            SimplexKalkulatorA smplxCalcKI = new SimplexKalkulatorA(provjeraSedla.uneseniPodaci, provjeraSedla.ProvjeriSedlo().Item3);

                            formaSimplexMetode = new SimplexForma(smplxCalcKI.SimplexTabliceRazlomci, smplxCalcKI.Zakljucak, smplxCalcKI.indexiVodecihStupaca, smplxCalcKI.indexiVodecihRedaka, smplxCalcKI.brojRedaka, smplxCalcKI.brojStupaca, smplxCalcKI.postupakIzracuna);
                            formaSimplexMetode.ShowDialog();
                        }
                    } 
                }

                else //igracB.Check == true;
                {
                    uneseniDobiciGubitci = new SpremanjeUnosa(dgvMatrica);

                    //provjera postojanja sedla
                    SedloDominacija provjeraSedla = new SedloDominacija(uneseniDobiciGubitci);

                    Tuple<bool, int, int> postojanjeSedla = provjeraSedla.ProvjeriSedlo();
                    bool postojiSedlo = postojanjeSedla.Item1;
                    int rezultatIgre = postojanjeSedla.Item2;

                    if (postojiSedlo)
                    {
                        MessageBox.Show("Postoji sedlo!\nVrijednost ove igre iznosi: " + rezultatIgre, "Kraj igre!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        ProtuprirodnaKontradiktornaIgra protuprirodnost = new ProtuprirodnaKontradiktornaIgra(new SpremanjeUnosa(dgvMatrica));

                        int vrstaIgre = protuprirodnost.ProvjeriProtuprirodnost();

                        if (vrstaIgre == 0)
                        {
                            provjeraSedla.ukloniDominantneStrategije(); //provjera dal postoje dominantnih strategija te ih eliminira
                            /*
                            string uklonjeneStrategije = provjeraSedla.IspisUklonjenihStrategijaIgracaB();
                            if (!String.IsNullOrEmpty(uklonjeneStrategije))
                                MessageBox.Show(uklonjeneStrategije);
                            */
                            Tuple<bool, int, int> postojanjeSedlaIntern = provjeraSedla.ProvjeriSedlo();
                            bool postojiSedloIntern = postojanjeSedlaIntern.Item1;
                            int rezultatIgreIntern = postojanjeSedlaIntern.Item2;

                            if (postojiSedloIntern)
                            {
                                MessageBox.Show("Postoji sedlo nakon uklanjanja dominantnih strategija!\nVrijednost ove igre iznosi: " + rezultatIgreIntern, "Kraj igre!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            else
                            {
                                //simplex metoda 
                                SimplexKalkulatorB smplxCalcMI = new SimplexKalkulatorB(provjeraSedla.uneseniPodaci, postojanjeSedlaIntern.Item3); //šalju se strategije bez onih dominantnih
                                formaSimplexMetode = new SimplexForma(smplxCalcMI.SimplexTabliceRazlomci, smplxCalcMI.Zakljucak, smplxCalcMI.indexiVodecihStupaca, smplxCalcMI.indexiVodecihRedaka, smplxCalcMI.brojRedaka, smplxCalcMI.brojStupaca, smplxCalcMI.postupakIzracuna);
                                formaSimplexMetode.ShowDialog();
                            }
                        }
                        else if (vrstaIgre == 1)
                        {

                            MessageBox.Show("Unesena je protuprirodna igra!\nNe uklanjam dominantne strategije.");
                            provjeraSedla.ukloniDuplikatneStrategije();
                            /*
                            string uklonjeneStrategije = provjeraSedla.IspisUklonjenihDuplikatnihB();
                            if (!String.IsNullOrEmpty(uklonjeneStrategije))
                                MessageBox.Show(uklonjeneStrategije);
                            */
                            SimplexKalkulatorB smplxCalcPI = new SimplexKalkulatorB(provjeraSedla.uneseniPodaci, provjeraSedla.ProvjeriSedlo().Item3);

                            formaSimplexMetode = new SimplexForma(smplxCalcPI.SimplexTabliceRazlomci, smplxCalcPI.Zakljucak, smplxCalcPI.indexiVodecihStupaca, smplxCalcPI.indexiVodecihRedaka, smplxCalcPI.brojRedaka, smplxCalcPI.brojStupaca, smplxCalcPI.postupakIzracuna);
                            formaSimplexMetode.ShowDialog();
                        }
                        else//kontradiktorna
                        {
                            MessageBox.Show("Unesena je kontradiktorna igra!\nNe uklanjam dominantne strategije.");//kontradiktorna nastaje nakon uklanjanja strategija svođenjem jednog igrača na samo 1 strategiju

                            provjeraSedla.ukloniDuplikatneStrategije();
                            /*
                            string uklonjeneStrategije = provjeraSedla.IspisUklonjenihDuplikatnihB();
                            if (!String.IsNullOrEmpty(uklonjeneStrategije))
                                MessageBox.Show(uklonjeneStrategije);
                            */
                            SimplexKalkulatorB smplxCalcKI = new SimplexKalkulatorB(provjeraSedla.uneseniPodaci, provjeraSedla.ProvjeriSedlo().Item3);

                            formaSimplexMetode = new SimplexForma(smplxCalcKI.SimplexTabliceRazlomci, smplxCalcKI.Zakljucak, smplxCalcKI.indexiVodecihStupaca, smplxCalcKI.indexiVodecihRedaka, smplxCalcKI.brojRedaka, smplxCalcKI.brojStupaca, smplxCalcKI.postupakIzracuna);
                            formaSimplexMetode.ShowDialog();
                        }
                    }
                }
       
            }
            catch
            {
               MessageBox.Show("Unesite gubitke i dobitke strategija pojedinih igrača!", "Pažnja", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }         
        }
        private void btnModelZadatka_Click(object sender, EventArgs e)
        {
            try
            {
                if(rbIgracA.Checked == true)
                {
                    uneseniDobiciGubitci = new SpremanjeUnosa(dgvMatrica);

                    //provjera postojanja sedla
                    SedloDominacija provjeraSedla = new SedloDominacija(uneseniDobiciGubitci);
              
                    ProtuprirodnaKontradiktornaIgra protuprirodnost = new ProtuprirodnaKontradiktornaIgra(new SpremanjeUnosa(dgvMatrica));
                    IzgradnjaModelaA modelZadatka;
                    int vrstaIgre = protuprirodnost.ProvjeriProtuprirodnost();

                    if (vrstaIgre == 0)
                    {
                        provjeraSedla.ukloniDominantneStrategije();
                        MatricnaIgra matricnaIgra = new MatricnaIgra(protuprirodnost.uneseniPodaci);

                        string uklonjeneStrategije = provjeraSedla.IspisUklonjenihStrategijaIgracaA();
                        uklonjeneStrategije += matricnaIgra.IspisMatricneIgre();//prikaz matricne igre

                        modelZadatka = new IzgradnjaModelaA(provjeraSedla.uneseniPodaci, provjeraSedla.ProvjeriSedlo().Item3);

                        FormaModela modelMI = new FormaModela(uklonjeneStrategije, modelZadatka.DohvatiZapisModela());
                        modelMI.ShowDialog();
                    }
                    else if(vrstaIgre == 1) // izračun po 3 kriterija
                    {
                        provjeraSedla.ukloniDuplikatneStrategije();

                        MatricnaIgra matricnaIgra = new MatricnaIgra(protuprirodnost.uneseniPodaci);

                        KriterijiProtuprirodnosti kriteriji = new KriterijiProtuprirodnosti(protuprirodnost.uneseniPodaci, 1); //rjesavanje po kriterijima

                        string uklonjeneStrategije = "Unesena igra je protuprirodna ili postaje protuprirodna igra nakon uklanjanja strategija:" + protuprirodnost.IspisUklonjenihStrategijaIgraca() + matricnaIgra.IspisMatricneIgre() + Environment.NewLine +kriteriji.IspisiVrijednostiKriterija() + Environment.NewLine + "Kod izrade modela ne uklanjam dominantne strategije. ";
                        uklonjeneStrategije += provjeraSedla.IspisUklonjenihDuplikatnihA();

                        matricnaIgra = new MatricnaIgra(provjeraSedla.uneseniPodaci);
                        uklonjeneStrategije += matricnaIgra.IspisMatricneIgre();

                        modelZadatka = new IzgradnjaModelaA(provjeraSedla.uneseniPodaci, provjeraSedla.ProvjeriSedlo().Item3);

                        FormaModela modelPI = new FormaModela(uklonjeneStrategije, modelZadatka.DohvatiZapisModela());
                        modelPI.ShowDialog();
                    }
                    else
                    {
                        provjeraSedla.ukloniDuplikatneStrategije();

                        MatricnaIgra matricnaIgra = new MatricnaIgra(protuprirodnost.uneseniPodaci);

                        string uklonjeneStrategije = "Unesena igra je kontradiktorna ili postaje kontradiktorna igra nakon uklanjanja strategija:" + protuprirodnost.IspisUklonjenihStrategijaIgraca() + matricnaIgra.IspisMatricneIgre() + Environment.NewLine + Environment.NewLine + "Kod izrade modela ne uklanjam dominantne strategije. ";
                        uklonjeneStrategije += provjeraSedla.IspisUklonjenihDuplikatnihA();

                        matricnaIgra = new MatricnaIgra(provjeraSedla.uneseniPodaci);
                        uklonjeneStrategije += matricnaIgra.IspisMatricneIgre();

                        modelZadatka = new IzgradnjaModelaA(provjeraSedla.uneseniPodaci, provjeraSedla.ProvjeriSedlo().Item3);

                        FormaModela modelKI = new FormaModela(uklonjeneStrategije, modelZadatka.DohvatiZapisModela());
                        modelKI.ShowDialog();
                    }
                }
                else //igracB.Check == true;
                {
                    uneseniDobiciGubitci = new SpremanjeUnosa(dgvMatrica);

                    //provjera postojanja sedla
                    SedloDominacija provjeraSedla = new SedloDominacija(uneseniDobiciGubitci);

                    ProtuprirodnaKontradiktornaIgra protuprirodnost = new ProtuprirodnaKontradiktornaIgra(new SpremanjeUnosa(dgvMatrica));
                    IzgradnjaModelaB modelZadatka;
                    int vrstaIgre = protuprirodnost.ProvjeriProtuprirodnost();

                    if (vrstaIgre == 0)
                    {
                        provjeraSedla.ukloniDominantneStrategije();
                        MatricnaIgra matricnaIgra = new MatricnaIgra(protuprirodnost.uneseniPodaci);

                        string uklonjeneStrategije = provjeraSedla.IspisUklonjenihStrategijaIgracaB();
                        uklonjeneStrategije += matricnaIgra.IspisMatricneIgre();

                        modelZadatka = new IzgradnjaModelaB(provjeraSedla.uneseniPodaci, provjeraSedla.ProvjeriSedlo().Item3);

                        FormaModela modelMI = new FormaModela(uklonjeneStrategije, modelZadatka.DohvatiZapisModela());
                        modelMI.ShowDialog();
                    }
                    else if (vrstaIgre == 1) // izračun po 3 kriterija
                    {
                        provjeraSedla.ukloniDuplikatneStrategije();

                        MatricnaIgra matricnaIgra = new MatricnaIgra(protuprirodnost.uneseniPodaci);
                        KriterijiProtuprirodnosti kriteriji = new KriterijiProtuprirodnosti(protuprirodnost.uneseniPodaci, 2); //rjesavanje po kriterijima

                        string uklonjeneStrategije = "Unesena igra je protuprirodna ili postaje protuprirodna igra nakon uklanjanja strategija" + protuprirodnost.IspisUklonjenihStrategijaIgraca() + matricnaIgra.IspisMatricneIgre() + Environment.NewLine + kriteriji.IspisiVrijednostiKriterija() + Environment.NewLine + "Kod izrade modela ne uklanjam dominantne strategije. ";
                        uklonjeneStrategije += provjeraSedla.IspisUklonjenihDuplikatnihB();

                        matricnaIgra = new MatricnaIgra(provjeraSedla.uneseniPodaci);
                        uklonjeneStrategije += matricnaIgra.IspisMatricneIgre();

                        modelZadatka = new IzgradnjaModelaB(provjeraSedla.uneseniPodaci, provjeraSedla.ProvjeriSedlo().Item3);

                        FormaModela modelPI = new FormaModela(uklonjeneStrategije, modelZadatka.DohvatiZapisModela());
                        modelPI.ShowDialog();
                    }
                    else
                    {
                        provjeraSedla.ukloniDuplikatneStrategije();

                        MatricnaIgra matricnaIgra = new MatricnaIgra(protuprirodnost.uneseniPodaci);

                        string uklonjeneStrategije = "Unesena igra je kontradiktorna ili postaje kontradiktorna igra nakon uklanjanja strategija:" + protuprirodnost.IspisUklonjenihStrategijaIgraca() + matricnaIgra.IspisMatricneIgre() + Environment.NewLine + Environment.NewLine + "Kod izrade modela ne uklanjam dominantne strategije. ";
                        uklonjeneStrategije += provjeraSedla.IspisUklonjenihDuplikatnihB();

                        matricnaIgra = new MatricnaIgra(provjeraSedla.uneseniPodaci);
                        uklonjeneStrategije += matricnaIgra.IspisMatricneIgre();

                        modelZadatka = new IzgradnjaModelaB(provjeraSedla.uneseniPodaci, provjeraSedla.ProvjeriSedlo().Item3);

                        FormaModela modelKI = new FormaModela(uklonjeneStrategije, modelZadatka.DohvatiZapisModela());
                        modelKI.ShowDialog();
                    }
                }
            }
            catch
            {
                MessageBox.Show("Unesite gubitke i dobitke strategija pojedinih igrača!", "Pažnja", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }      
        }


        //SITNICE
        private void txtStrA_MouseClick(object sender, MouseEventArgs e)
        {
            txtStrA.SelectionStart = 0;
        }

        private void txtStrB_MouseClick(object sender, MouseEventArgs e)
        {
            txtStrB.SelectionStart = 0;
        }

        private void dgvMatrica_SizeChanged(object sender, EventArgs e)
        {
            //za izgled tablice - tako da je cijela popunjena
            foreach (DataGridViewRow red in dgvMatrica.Rows)
            {
                red.Height = (dgvMatrica.ClientRectangle.Height - dgvMatrica.ColumnHeadersHeight) / dgvMatrica.Rows.Count;
            }
        }

        private void dgvMatrica_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            if (e.Exception != null && e.Context == DataGridViewDataErrorContexts.Commit)
            {
                MessageBox.Show("Pazite na unos!");
            }
        }
    }
}
