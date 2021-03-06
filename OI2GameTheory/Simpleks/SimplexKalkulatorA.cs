﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.Common;

namespace OI2GameTheory
{
    public class SimplexKalkulatorA // standardi problem LP za max
    {
        private SpremanjeUnosa podaciStrategija;
        private int diferencija;

        private DataTable pocetnaSimplexTablica = new DataTable();
        private DataTable prethodnaSimplexTablica = new DataTable(); //pomocna
        private DataTable novaSimplexTablica = new DataTable();

        public DataTable SimplexTablice = new DataTable(); //svi postupci
        public DataTable SimplexTabliceRazlomci = new DataTable(); //svi postupci
        public string Zakljucak = "";

        public List<int> indexiVodecihStupaca = new List<int>();
        public List<int> indexiVodecihRedaka = new List<int>();
        public int brojRedaka;
        public int brojStupaca;
        public string postupakIzracuna;

        public SimplexKalkulatorA(SpremanjeUnosa podaci, int minDif)
        {
            podaciStrategija = podaci;

            if (minDif <= 0)
                diferencija = Math.Abs(minDif) + 1;
            else
                diferencija = 0;

            diferencirajPodatke();

            stvoriPocetnuTablicu();
            pokreniSimplexPostupak();
        }

        private void diferencirajPodatke()
        {
            foreach(var strategija in podaciStrategija.igracB.ToList())//ne mora se i kroz strategije igraca B ići
            {
                for(int i=0; i<strategija.DobitakGubitakStrategije.Length; i++)
                {
                    strategija.DobitakGubitakStrategije[i] += diferencija;
                }
            }
        }

        private void stvoriPocetnuTablicu()
        {
            //stupci
            pocetnaSimplexTablica.Columns.Add("Cj", typeof(string));//zbog M
            pocetnaSimplexTablica.Columns.Add("Var", typeof(string));
            pocetnaSimplexTablica.Columns.Add("Kol", typeof(string));

            for(int i=0; i<podaciStrategija.igracA.Count; i++)
                pocetnaSimplexTablica.Columns.Add("x̄" + (i + 1) + "", typeof(string)); //x̄ - supstitucija za yi/v'

            for (int i = 0; i < podaciStrategija.igracB.Count; i++)
                pocetnaSimplexTablica.Columns.Add("u" + (i + 1) + "", typeof(string)); //dopunske varijable u - ovise o broju jednadzbi tj. igracu za koji se radi simplex

            for (int i = 0; i < podaciStrategija.igracB.Count; i++)
                pocetnaSimplexTablica.Columns.Add("w" + (i + 1) + "", typeof(string)); //artificijalne varijable w - ovise o broju jednadzbi tj. igracu 

            pocetnaSimplexTablica.Columns.Add("Kontrola", typeof(string));
            pocetnaSimplexTablica.Columns.Add("Rezultat", typeof(string));

            //redci
            int brojacStrategijaB = 0;
            foreach (var strategijaB in podaciStrategija.igracB)
            {
                double internKontrol = 0;
                var noviRedak = pocetnaSimplexTablica.NewRow();

                noviRedak["Cj"] = "M";
                noviRedak["Var"] = "w" + (brojacStrategijaB + 1) + "";
                noviRedak["Kol"] = 1; //sve strategije (x1+x2.. = 1)
                internKontrol += 1; //kontrola redka

                for (int j = 0; j < strategijaB.DobitakGubitakStrategije.Length; j++)
                {
                    noviRedak["x̄" + (j + 1) + ""] = strategijaB.DobitakGubitakStrategije[j];
                    internKontrol += strategijaB.DobitakGubitakStrategije[j];
                }

                //u varijable
                for (int j = 0; j < podaciStrategija.igracB.Count; j++)
                {

                    if ((j + 1) == (brojacStrategijaB + 1))
                    {
                        noviRedak["u" + (j + 1) + ""] = -1;
                        internKontrol -= 1;
                    }
                    else
                        noviRedak["u" + (j + 1) + ""] = 0;
                }

                //w varijable
                for (int j = 0; j < podaciStrategija.igracB.Count; j++)
                {

                    if ((j + 1) == (brojacStrategijaB + 1))
                    {
                        noviRedak["w" + (j + 1) + ""] = 1;
                        internKontrol += 1;
                    }
                    else
                        noviRedak["w" + (j + 1) + ""] = 0;
                }

                noviRedak["Kontrola"] = internKontrol;
                pocetnaSimplexTablica.Rows.Add(noviRedak);
                brojacStrategijaB++;
            }

            //Zj-Cj redak
            var redakZjCj = pocetnaSimplexTablica.NewRow();
            redakZjCj["Var"] = "Zj-Cj";
            redakZjCj["Kol"] = 0;

            for (int i = 0; i < podaciStrategija.igracA.Count; i++)
            {
                redakZjCj["x̄" + (i + 1) + ""] = -1;
            }

            for (int i = 0; i < podaciStrategija.igracB.Count; i++)
            {
                redakZjCj["u" + (i + 1) + ""] = 0;
                redakZjCj["w" + (i + 1) + ""] = 0;
            }

            double kontrolaRedka = 0;//kolicina
            foreach (var strategija in podaciStrategija.igracA)
                kontrolaRedka--;


            redakZjCj["Kontrola"] = kontrolaRedka;
            pocetnaSimplexTablica.Rows.Add(redakZjCj);

            //Dj redak
            var redakDj = pocetnaSimplexTablica.NewRow();
            redakDj["Var"] = "Dj";

            pocetnaSimplexTablica.Rows.Add(redakDj);

            //sumiranje stupaca za redak Dj
            for (int i = 2; i < pocetnaSimplexTablica.Columns.Count - 1; i++)//stupci //-2
            {
                double internHelp = 0;
                for (int j=0; j<pocetnaSimplexTablica.Rows.Count-2; j++)//samo redci jednadžbe //-3 inače jer još nema praznog reda
                {
                    internHelp += Convert.ToDouble(pocetnaSimplexTablica.Rows[j][i]);
                }

                pocetnaSimplexTablica.Rows[pocetnaSimplexTablica.Rows.Count - 1][i] = internHelp;
            }

            //prazan redak radi preglednosti
            var prazanRedak1 = pocetnaSimplexTablica.NewRow();
            pocetnaSimplexTablica.Rows.Add(prazanRedak1);

            SimplexTablice.Merge(pocetnaSimplexTablica);

            prethodnaSimplexTablica = pocetnaSimplexTablica;
        }

        private (int, string) odrediVodeciStupac()
        {
            int indexStupca = 0;
            string nazivSupca = "";

            List<int> indexiNegStupacaZjCj = new List<int>(); 
            List<int> indexiPozStupacaZjCj = new List<int>();

            int brojPozitivnihDj = 0;
            for (int i = 3; i < prethodnaSimplexTablica.Columns.Count - (podaciStrategija.igracB.Count + 2); i++)//bez w varijabli
            {
                double internHelp1 = Convert.ToDouble(prethodnaSimplexTablica.Rows[prethodnaSimplexTablica.Rows.Count - 2][i].ToString());
                
                if (internHelp1 > 0.00001)//0.01 0.000001 0.001
                {
                    brojPozitivnihDj++;
                }
            }

            //koliko ima pozitivnih u Zj-Cj redku
            int brojPozitivnihZj = 0;
            for (int i = 3; i < prethodnaSimplexTablica.Columns.Count - (podaciStrategija.igracB.Count + 2); i++)//bez w varijabli
            {
                double internHelp2 = Convert.ToDouble(prethodnaSimplexTablica.Rows[prethodnaSimplexTablica.Rows.Count - 3][i].ToString());
                if (internHelp2 > 0.00001)//0.01 0.000001 0.001
                {
                    brojPozitivnihZj++;
                }
            }

            double internHelp = 0;
            //uzima indexe stupaca gdje su neg. vrijednosti u redu Zj-Cj
            for (int i = 3; i < prethodnaSimplexTablica.Columns.Count - (podaciStrategija.igracB.Count + 2); i++)
            {
                internHelp = Convert.ToDouble(prethodnaSimplexTablica.Rows[prethodnaSimplexTablica.Rows.Count - 2][i].ToString());
                if (internHelp < 0)
                    indexiNegStupacaZjCj.Add(i);
                else if (internHelp == 0)
                    continue;
                else
                    indexiPozStupacaZjCj.Add(i);
            }

            if(brojPozitivnihDj > 0)//uzmi max
            {
                List<double> vrijednostiDjReda = new List<double>();
                List<int> indexiVrijednostiDjReda = new List<int>();
                List<int> indexiVrijednostiZjReda = new List<int>();//indexi onih koji su najveci Dj za slučaj ak su isti
                int brojac = 0;
                double najveci = 0;
                for (int i = 3; i < prethodnaSimplexTablica.Columns.Count - (podaciStrategija.igracB.Count + 2); i++)
                {
                    internHelp = Convert.ToDouble(prethodnaSimplexTablica.Rows[prethodnaSimplexTablica.Rows.Count - 2][i].ToString());
                    if (internHelp > 0)
                    {
                        internHelp = Math.Abs(internHelp);
                        vrijednostiDjReda.Add(internHelp);
                        indexiVrijednostiDjReda.Add(i);
                        brojac++;

                        if (najveci < internHelp)
                        {
                            najveci = internHelp;
                            indexStupca = i;
                            nazivSupca = prethodnaSimplexTablica.Columns[i].ColumnName;
                        }
                    }
                }

                bool postojiIstaDjVrijednost = false;
                int brojacIstihDjVrijednosti = 0;
                int pomBrojac = 3;
                foreach (var vrijednost in vrijednostiDjReda)
                {
                    if (vrijednost == najveci)
                    {
                        indexiVrijednostiZjReda.Add(pomBrojac);
                        brojacIstihDjVrijednosti++;
                    }
                    pomBrojac++;
                }

                if (brojacIstihDjVrijednosti >= 2)
                    postojiIstaDjVrijednost = true;

                if (postojiIstaDjVrijednost) //max Zj koji je bliži nuli- tj max!!!!
                {
                    List<double> vrijednostiZjCjRedka = new List<double>();
                    double najveci2 = 0;
                    for (int i = 3; i < indexiVrijednostiZjReda.Count; i++)
                    {
                        internHelp = Convert.ToDouble(prethodnaSimplexTablica.Rows[prethodnaSimplexTablica.Rows.Count - 3][indexiVrijednostiZjReda[i-3]].ToString());
                        if (najveci2 < internHelp)
                        {
                            najveci2 = internHelp;
                            indexStupca = i;
                            nazivSupca = prethodnaSimplexTablica.Columns[i].ColumnName;
                        }
                    }
                }
            }
            
            
            else if(brojPozitivnihZj > 0)
            {
                //vratiti max pozitivni zjcj
                double najveci2 = 0;
                for (int i = 3; i < prethodnaSimplexTablica.Columns.Count - (podaciStrategija.igracB.Count + 2); i++)
                {
                    internHelp = Convert.ToDouble(prethodnaSimplexTablica.Rows[prethodnaSimplexTablica.Rows.Count - 3][i].ToString());

                    if (najveci2 < internHelp)
                    {
                        najveci2 = internHelp;
                        indexStupca = i;
                        nazivSupca = prethodnaSimplexTablica.Columns[i].ColumnName;
                    }
                }
            }
            return (indexStupca, nazivSupca);
        }

        private int odrediVodeciRedak(int indexStupca)
        {
            int indexReda = 0;

            double internHelp = 0;
            double[] rezultati = new double[prethodnaSimplexTablica.Rows.Count - 3];
            double djeljenik = 0;
            double djelitelj = 0;
            List<double> odabraniRez = new List<double>();

            for (int i = 0; i < prethodnaSimplexTablica.Rows.Count - 3; i++) // djeljenik može biti negativan, djelitelj ne
            {
                djeljenik = Convert.ToDouble(prethodnaSimplexTablica.Rows[i][2]);
                djelitelj = Convert.ToDouble(prethodnaSimplexTablica.Rows[i][indexStupca]);

                internHelp = (double)djeljenik / djelitelj;
                rezultati[i] = internHelp;
                if (djelitelj > 0)
                {
                    odabraniRez.Add(internHelp);
                    prethodnaSimplexTablica.Rows[i][prethodnaSimplexTablica.Columns.Count - 1] = Math.Round((double)internHelp, 6);
                }
            }
            SimplexTablice.Merge(prethodnaSimplexTablica);

            //double najmanji = rezultati.Where(x => x > 0).Min();
            double najmanji = odabraniRez.Min();

            int brojacIstihMin = 0;
            bool postojeIsteMinVrijednostiRez = false;
            for(int i=0; i<rezultati.Length; i++)
            {
                if (Math.Round(najmanji,4) == Math.Round(rezultati[i],4))
                    brojacIstihMin++;
            }

            int[] indexiIstihRezultata = new int[brojacIstihMin];

            if (brojacIstihMin >= 2) // u slučaju da postoje isti koji nisu minimalni
            {
                postojeIsteMinVrijednostiRez = true;
            }

            int pomocniBrojac1 = 0;
            for (int i = 0; i < rezultati.Length; i++)
            {
                if (Math.Round(najmanji, 4) == Math.Round(rezultati[i], 4))
                {
                    indexiIstihRezultata[pomocniBrojac1] = i;
                    pomocniBrojac1++;
                }
            }


            double[] degeneracija;
            if (postojeIsteMinVrijednostiRez)
            {
                for (int i = 3; i < prethodnaSimplexTablica.Columns.Count - 2; i++)//pomicanje po stupcima
                {
                    degeneracija = new double[indexiIstihRezultata.Length];

                    if (i != indexStupca)//indexStupca je vodeci stupac
                    {
                        for (int j = 0; j < indexiIstihRezultata.Length; j++)//pomicanje po redcima
                        {
                            double djeljenikIntern = Convert.ToDouble(prethodnaSimplexTablica.Rows[indexiIstihRezultata[j]][i]);
                            double djeliteljIntern = Convert.ToDouble(prethodnaSimplexTablica.Rows[indexiIstihRezultata[j]][indexStupca]);


                            if (djeliteljIntern > 0)
                                degeneracija[j] = (double)djeljenikIntern / djeliteljIntern;
                            else
                                degeneracija[j] = double.MaxValue; //Convert.ToDouble(999999 + j); /// u slučaju da je u vodećem stupcu 0 ili minus

                            //System.Windows.Forms.MessageBox.Show(degeneracija[j].ToString());
                        }

                        double najmanjiDegene = degeneracija.Min();

                        int brojacIstihUDeg = 0;
                        bool postojiJednistveniMin = true;
                        for (int d = 0; d < degeneracija.Length; d++)
                        {
                            if (najmanjiDegene == degeneracija[d])
                                brojacIstihUDeg++;
                        }

                        if (brojacIstihUDeg >= 2) 
                        {
                            postojiJednistveniMin = false;
                        }

                        if (postojiJednistveniMin)
                        {
                            double najveciRedak = degeneracija.Min();//PROMJENITI AKO NIJE DOBRA DEGENERACIJA TAK
                            for (int d = 0; d < degeneracija.Length; d++)
                            {
                                if (najveciRedak == degeneracija[d])
                                {
                                    indexReda = indexiIstihRezultata[d];
                                }
                            }
                            //return indexReda;
                            break;
                        }
                    }
                }

                return indexReda;
            }
            else
            {
                for (int i = 0; i < rezultati.Length; i++)
                {
                    if (najmanji == rezultati[i])
                        indexReda = i;
                }

                return indexReda;
            }
        }

        private double odrediStozerniElement(int indexStupca, int indexReda)
        {
            double stozerniElement = Convert.ToDouble(prethodnaSimplexTablica.Rows[indexReda][indexStupca]);
            //SimplexTablice.Merge(prethodnaSimplexTablica);
            return stozerniElement;
        }

        private void izracunajElementeVodecegRedka(int indexStupca, int indexRedka, double stozerniElement, string nazivVodSupca)
        {
            for(int i=2; i<prethodnaSimplexTablica.Columns.Count-1; i++)
            {
                double internHelp = Convert.ToDouble(prethodnaSimplexTablica.Rows[indexRedka][i]);
                novaSimplexTablica.Rows[indexRedka][i] = Math.Round((double) internHelp / (double) stozerniElement, 6);
            }

            novaSimplexTablica.Rows[indexRedka][0] = 1; //vrijednost Cj = 1
            novaSimplexTablica.Rows[indexRedka][1] = nazivVodSupca; //vrijednost var

            for(int i=0; i<prethodnaSimplexTablica.Rows.Count-1; i++)// 0 po stupcu
            {
                if (i != indexRedka)
                    novaSimplexTablica.Rows[i][indexStupca] = 0;
            }
        }

        private void simplexAlgoritam(int indexStupca, int indexRedka, double stozerniElement)
        {
            //od index 2 do count-2
            for (int i = 2; i < novaSimplexTablica.Columns.Count - 1; i++)//stupci
            {
                postupakIzracuna += novaSimplexTablica.Columns[i].ColumnName + Environment.NewLine + Environment.NewLine;

                if (i != indexStupca)
                {
                    for (int j = 0; j < novaSimplexTablica.Rows.Count - 1; j++)//redci
                    {
                        if(j != indexRedka)
                        {
                            //double internHelp = (double) (Convert.ToDouble(prethodnaSimplexTablica.Rows[j][i].ToString()) - ((double)((double)(Convert.ToDouble(prethodnaSimplexTablica.Rows[indexRedka][i].ToString()) / (double) stozerniElement) * Convert.ToDouble(prethodnaSimplexTablica.Rows[j][indexStupca].ToString()))));
                            //novaSimplexTablica.Rows[j][i] = Math.Round((double)internHelp, 6);

                            
                            //broj1 - (broj2/stozerni) * broj3 = internHelp - ISPIS POSTUPKA
                            double broj1 = (double)(Convert.ToDouble(prethodnaSimplexTablica.Rows[j][i].ToString()));
                            double broj2 = (double)(Convert.ToDouble(prethodnaSimplexTablica.Rows[indexRedka][i].ToString()) / (double)stozerniElement);
                            double broj3 = (double)Convert.ToDouble(prethodnaSimplexTablica.Rows[j][indexStupca].ToString());                         

                            double internHelp = broj1 - (broj2 * broj3);

                            if ((internHelp >= -0.00001) && (internHelp <= 0.00001))
                            {
                                internHelp = 0;
                            }

                            novaSimplexTablica.Rows[j][i] = Math.Round((double)internHelp, 6);

                            
                            internHelp = Math.Round((double)internHelp, 6);
                            string broj1Razlomak;
                            string broj2Razlomak;
                            string broj3Razlomak;
                            string rezultat;

                            if ((broj1 % 1) != 0)
                                broj1Razlomak = RealToFraction(broj1, 0.0001).N + "/" + RealToFraction(broj1, 0.0001).D;
                            else
                                broj1Razlomak = broj1.ToString();

                            if ((broj2 % 1) != 0)
                               broj2Razlomak = RealToFraction(broj2, 0.0001).N + "/" + RealToFraction(broj2, 0.0001).D;
                            else
                               broj2Razlomak = broj2.ToString();

                            if ((broj3 % 1) != 0)
                               broj3Razlomak = RealToFraction(broj3, 0.0001).N + "/" + RealToFraction(broj3, 0.0001).D;
                            else
                               broj3Razlomak = broj3.ToString();
                          
                            if ((internHelp % 1) != 0)
                                rezultat = RealToFraction(internHelp, 0.0001).N + "/" + RealToFraction(internHelp, 0.0001).D;
                            else
                                rezultat = internHelp.ToString();

                            postupakIzracuna += broj1Razlomak + " - (" + broj2Razlomak + " * " + broj3Razlomak + ") = " + rezultat + Environment.NewLine + Environment.NewLine;
                            
                        }
                    }
                }
            }
        }

        int brojIteracija = 2;
        int help = 1;
        private void pokreniSimplexPostupak()
        {
            brojRedaka = prethodnaSimplexTablica.Rows.Count;
            brojStupaca = prethodnaSimplexTablica.Columns.Count;

            int indexStupca = odrediVodeciStupac().Item1;
            string nazivVodecegStupca = odrediVodeciStupac().Item2;
            indexiVodecihStupaca.Add(indexStupca);

            int indexRedka = odrediVodeciRedak(indexStupca);
            indexiVodecihRedaka.Add(indexRedka);

            double stozerniElement = odrediStozerniElement(indexStupca, indexRedka);

            novaSimplexTablica = prethodnaSimplexTablica.Clone();//da naslijedi strukturu samo

            for(int i=0; i<prethodnaSimplexTablica.Rows.Count; i++)//dodavanje redaka u novu
            {
                var noviRedak = novaSimplexTablica.NewRow();
                novaSimplexTablica.Rows.Add(noviRedak);
            }

            for (int i = 0; i < prethodnaSimplexTablica.Rows.Count-1; i++)//prepisivanje statike
            {
                if(i != indexRedka)
                {
                    novaSimplexTablica.Rows[i][0] = prethodnaSimplexTablica.Rows[i][0];//Cj stupac
                    novaSimplexTablica.Rows[i][1] = prethodnaSimplexTablica.Rows[i][1];//Var stupac
                }
            }

            izracunajElementeVodecegRedka(indexStupca, indexRedka, stozerniElement, nazivVodecegStupca);

            //postupakIzracuna += "Postupak izračuna za igrača B: " + Environment.NewLine;
            if (help == 1) // za prikaz postupka izracunavanja 
            {
                postupakIzracuna += "Postupak izračuna za igrača A: " + Environment.NewLine;
                postupakIzracuna += "--------------------1. ITERACIJA--------------------" + Environment.NewLine + Environment.NewLine;
                help++;
            }

            simplexAlgoritam(indexStupca, indexRedka, stozerniElement);

            SimplexTablice.Merge(novaSimplexTablica); //prije if-a obavezno

            
            //koliko ima pozitivnih u Dj redku
            int brojPozitivnihDj = 0;
            for (int i = 3; i < novaSimplexTablica.Columns.Count - (podaciStrategija.igracB.Count + 2); i++)//bez w varijabli
            {
                double internHelp = Convert.ToDouble(novaSimplexTablica.Rows[novaSimplexTablica.Rows.Count - 2][i].ToString());
                if (internHelp > 0)//// 0.001
                {
                    brojPozitivnihDj++;
                }
            }

            //koliko ima pozitivnih u Zj-Cj redku
            int brojPozitivnihZj = 0;
            for (int i = 3; i < novaSimplexTablica.Columns.Count - (podaciStrategija.igracB.Count + 2); i++)//bez w varijabli
            {
                double internHelp = Convert.ToDouble(novaSimplexTablica.Rows[novaSimplexTablica.Rows.Count - 3][i].ToString());
                if (internHelp > 0)////0.001
                {
                    brojPozitivnihZj++;
                }
            }

            List<string> varijableW = new List<string>();
            for (int i = 0; i < podaciStrategija.igracB.Count; i++)
                varijableW.Add("w" + (i + 1) + "");

            List<string> varijableTrenutne = new List<string>();
            for(int i=0; i<novaSimplexTablica.Rows.Count-2; i++)
                varijableTrenutne.Add(novaSimplexTablica.Rows[i][1].ToString());

            bool postojiWJos = false;

            foreach(var varijabla in varijableW)
            {
                if (varijableTrenutne.Contains(varijabla))
                    postojiWJos = true;
            }

            
            if (postojiWJos || brojPozitivnihDj > 0 || brojPozitivnihZj > 0) 
            {
                prethodnaSimplexTablica = novaSimplexTablica;
                prethodnaSimplexTablica = new DataTable();
                prethodnaSimplexTablica = novaSimplexTablica.Copy();//da naslijedi strukturu samo
                novaSimplexTablica = new DataTable();

                postupakIzracuna += "--------------------" + brojIteracija + ". ITERACIJA--------------------" + Environment.NewLine + Environment.NewLine;
                brojIteracija++;

                pokreniSimplexPostupak();
            }
            else
            {
                //pisanje iteracija tako da je pregledno i jasnije
                int interHelp = SimplexTablice.Rows.Count-1;
                int brojRedovaIteracije = (novaSimplexTablica.Rows.Count*2)-1;
                int brojRedova = (novaSimplexTablica.Rows.Count * 2);
                int brojIteracija = 1;
                for (int i=0; i< interHelp; i++)
                {
                    if (i == brojRedovaIteracije)
                    {
                        SimplexTablice.Rows[i][1] ="Tablica "+ brojIteracija+". iteracije";
                        brojRedovaIteracije += brojRedova;
                        brojIteracija++;
                    }

                }

                KalkulatorZakljuckaA zakljucak = new KalkulatorZakljuckaA(novaSimplexTablica, podaciStrategija, diferencija);
                Zakljucak = zakljucak.DohvatiZakljucak();
                postupakIzracuna += zakljucak.DohvatiPostupakZakljucka();

                //pretvaranje decimalni u razlomke
                SimplexTabliceRazlomci = SimplexTablice.Copy();
                SimplexTablice = new DataTable();
                PretvoriURazlomke();
           }
        }

        private void PretvoriURazlomke()
        {
            for(int i = 2; i<SimplexTabliceRazlomci.Columns.Count; i++) //stupci
            {
                for(int j=0; j<SimplexTabliceRazlomci.Rows.Count-1; j++)//redci
                {
                    double broj = 0;
                    bool praznaCelija = false;
                    if (string.IsNullOrEmpty(SimplexTabliceRazlomci.Rows[j][i].ToString()))
                        praznaCelija = true;
                    else
                        broj = Convert.ToDouble(SimplexTabliceRazlomci.Rows[j][i]); 

                    if (!praznaCelija)
                    {
                        if ((broj - Math.Floor(broj)).ToString() == "0,999999")
                            SimplexTabliceRazlomci.Rows[j][i] = Math.Ceiling(broj);

                        else if ((broj % 1) != 0)
                            SimplexTabliceRazlomci.Rows[j][i] = RealToFraction(broj, 0.0001).N + "/" + RealToFraction(broj, 0.0001).D;
                        
                        if(j == SimplexTabliceRazlomci.Rows.Count - 2)
                            if ((broj < -0.0000000001) || (broj < 0.0000000001))
                                SimplexTabliceRazlomci.Rows[j][i] = 0;
                        
                    }
                }
            }
        }

        //PRETVARANJE double U RAZLOMKE
        public struct Fraction
        {
            public Fraction(int n, int d)
            {
                N = n;
                D = d;
            }

            public int N { get; private set; }
            public int D { get; private set; }
        }

        public Fraction RealToFraction(double value, double accuracy)
        {
            if (accuracy <= 0.0 || accuracy >= 1.0)
            {
                throw new ArgumentOutOfRangeException("accuracy", "Must be > 0 and < 1.");
            }

            int sign = Math.Sign(value);

            if (sign == -1)
            {
                value = Math.Abs(value);
            }

            // Accuracy is the maximum relative error; convert to absolute maxError
            double maxError = sign == 0 ? accuracy : value * accuracy;

            int n = (int)Math.Floor(value);
            value -= n;

            if (value < maxError)
            {
                return new Fraction(sign * n, 1);
            }

            if (1 - maxError < value)
            {
                return new Fraction(sign * (n + 1), 1);
            }

            // The lower fraction is 0/1
            int lower_n = 0;
            int lower_d = 1;

            // The upper fraction is 1/1
            int upper_n = 1;
            int upper_d = 1;

            while (true)
            {
                // The middle fraction is (lower_n + upper_n) / (lower_d + upper_d)
                int middle_n = lower_n + upper_n;
                int middle_d = lower_d + upper_d;

                if (middle_d * (value + maxError) < middle_n)
                {
                    // real + error < middle : middle is our new upper
                    Seek(ref upper_n, ref upper_d, lower_n, lower_d, (un, ud) => (lower_d + ud) * (value + maxError) < (lower_n + un));
                }
                else if (middle_n < (value - maxError) * middle_d)
                {
                    // middle < real - error : middle is our new lower
                    Seek(ref lower_n, ref lower_d, upper_n, upper_d, (ln, ld) => (ln + upper_n) < (value - maxError) * (ld + upper_d));
                }
                else
                {
                    // Middle is our best fraction
                    return new Fraction((n * middle_d + middle_n) * sign, middle_d);
                }
            }
        }

        private void Seek(ref int a, ref int b, int ainc, int binc, Func<int, int, bool> f)
        {
            a += ainc;
            b += binc;

            if (f(a, b))
            {
                int weight = 1;

                do
                {
                    weight *= 2;
                    a += ainc * weight;
                    b += binc * weight;
                }
                while (f(a, b));

                do
                {
                    weight /= 2;

                    int adec = ainc * weight;
                    int bdec = binc * weight;

                    if (!f(a - adec, b - bdec))
                    {
                        a -= adec;
                        b -= bdec;
                    }
                }
                while (weight > 1);
            }
        }
    }
}
