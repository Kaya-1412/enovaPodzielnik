using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Soneta.Business;
using Soneta.Business.App;
using Soneta.Core;
using Soneta.Ksiega;
using Soneta.Place;
using Soneta.Towary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Soneta.Kasa;
using System.Collections.Immutable;
using Soneta.Kasa.Extensions;
using Soneta.Types;
using Google.Apis.Util;

namespace A1.PodzielWyplaty
{
    public class PodzielWyplatyWorker
    {

        public List<Soneta.Place.Wyplata> wyp {  get; set; }

        public PodzielWyplatyWorker(ListaPlac lp) 
        {
            this.wyp = lp.Wyplaty.ToList();
        }
        public void Podziel()
        {
            foreach (Soneta.Place.Wyplata w in wyp)
            {
                ElemSlownika projekt;
               
                Dictionary<ElemSlownika, decimal> projekty = new Dictionary<ElemSlownika, decimal>();
                
                
                foreach(WypElement el in w.Elementy)
                {
                    projekt = el.Definicja.Features["Projekty"] as ElemSlownika;
                   
                    if(!projekty.TryAdd(projekt, el.Netto))
                    {
                        projekty[projekt] += el.Netto;
                    }
                }
                foreach (var key in projekty)
                    if (projekty[key.Key].Equals(decimal.Zero))
                        projekty.Remove(key.Key);

                
                KasaModule km = KasaModule.GetInstance(w.Session);
                using (var t = w.Session.Logout(true))
                {
                    w.ListaPlac.Features["Podzielona"] = true;
                    foreach(Platnosc p in w.Platnosci)
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

    }
}
