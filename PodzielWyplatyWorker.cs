using Google.Apis.Util;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Soneta.Business;
using Soneta.Business.App;
using Soneta.Business.Db;
using Soneta.Business.UI;
using Soneta.Core;
using Soneta.Kadry;
using Soneta.Kasa;
using Soneta.Kasa.Extensions;
using Soneta.Ksiega;
using Soneta.Place;
using Soneta.Towary;
using Soneta.Types;
using Soneta.Zadania;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace A1.PodzielWyplaty
{
    public class PodzielWyplatyWorker
    {

        public List<Soneta.Place.Wyplata> wyp {  get; set; }
        public ListaPlac lp { get; set; }

        Dictionary<ElemSlownika, decimal> projekty;


        public PodzielWyplatyWorker(ListaPlac lp) 
        {
            this.wyp = lp.Wyplaty.ToList();
            this.lp = lp;
        }
        public void Podziel()
        {
            foreach (Soneta.Place.Wyplata w in wyp)
            {
                projekty = new();
                AddProjects(w);
                KasaModule km = KasaModule.GetInstance(w.Session);
                using (var t = w.Session.Logout(true))
                {
                    w.ListaPlac.Features["Podzielona"] = true;
                    foreach(Zobowiazanie p in w.Platnosci)
                    {
                        decimal pozostalaWartoscPlatnosci = p.Kwota.Value;
                        
                        for (int i = 0; i < projekty.Keys.Count; i++)
                        {
                            var key = projekty.Keys.ElementAt(i);
                            
                        
                            if (projekty[key] == decimal.Zero)
                                continue;
                            p.Opis = $"Wypłata za: {key.ToString()}";
                            if (pozostalaWartoscPlatnosci < projekty[key])
                            {
                                
                               
                                projekty[key] -= pozostalaWartoscPlatnosci;
                                pozostalaWartoscPlatnosci = decimal.Zero;
                                break;
                            }
                            else if (pozostalaWartoscPlatnosci == projekty[key])
                            {
                               
                                projekty[key] -= pozostalaWartoscPlatnosci;
                                pozostalaWartoscPlatnosci = decimal.Zero;
                                break;
                            }
                            else if (pozostalaWartoscPlatnosci > projekty[key])
                            {
                               
                                p.Kwota = projekty[key];
                                pozostalaWartoscPlatnosci -= projekty[key];
                                projekty[key] = decimal.Zero;
                                
                                while (pozostalaWartoscPlatnosci > decimal.Zero)
                                {
                                    i++;
                                    key = projekty.Keys.ElementAt(i);
                                    if (projekty[key] == decimal.Zero)
                                        break;
                                    Zobowiazanie zobowiazanie = new Zobowiazanie(w);
                                    km.Platnosci.AddRow(zobowiazanie);
                                    zobowiazanie.Podmiot = p.Podmiot;
                                    zobowiazanie.SposobZaplaty = p.SposobZaplaty;
                                    if(zobowiazanie.SposobZaplaty.Equals(SposobZaplaty.Przelew))
                                        zobowiazanie.Rachunek = p.Rachunek;
                                    zobowiazanie.EwidencjaSP = p.EwidencjaSP;
                                    zobowiazanie.Termin = p.Termin;
                                    zobowiazanie.Opis = $"Wypłata za: {key.ToString()}";
                                    if (projekty[key] < pozostalaWartoscPlatnosci)
                                    {
                                        zobowiazanie.Kwota = projekty[key];
                                        pozostalaWartoscPlatnosci -= projekty[key];
                                        projekty[key] = decimal.Zero;

                                    }
                                    else if (projekty[key] > pozostalaWartoscPlatnosci)
                                    {
                                        zobowiazanie.Kwota = pozostalaWartoscPlatnosci;
                                        projekty[key] -= pozostalaWartoscPlatnosci;
                                        pozostalaWartoscPlatnosci = decimal.Zero;
                                    }
                                    else
                                        break;
                                }
                            }   
                        }
                    }
                    
                    t.Commit();
                }
            }
        }

        public void AddProjects(Soneta.Place.Wyplata w)
        {
            ElemSlownika projekt;
           
            foreach (WypElement el in w.Elementy)
            {
                projekt = el.Definicja.Features["Projekty"] as ElemSlownika;
                try
                {
                    if (!projekty.TryAdd(projekt, el.Netto))
                    {
                        projekty[projekt] += el.Netto;
                    }
                }
                catch
                {
                    throw new Exception($"Element wynagrodzenia '{el.Nazwa}' ma nieprzypisany projekt.");
                }
            }
            foreach (var key in projekty)
                if (projekty[key.Key].Equals(decimal.Zero))
                    projekty.Remove(key.Key);
        }
    }
}
