using AltOne.KSM.UI;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Org.BouncyCastle.Asn1.X509;
using Soneta.Business;
using Soneta.Business.UI;
using Soneta.Core.Extensions;
using Soneta.Kadry;
using Soneta.KadryPlace;
using Soneta.Core;
using System;
using A1.PodzielWyplaty;
using Soneta.Place;

[assembly: Worker(typeof(PodzielWyplatyWorkerUI), typeof(ListaPlac))]

namespace AltOne.KSM.UI
{
    public class PodzielWyplatyWorkerUI
    {
        /*
        [Context]
        public PodzielWyplatyWorkerParams @params
        {
            get;
            set;
        }
        */
        [Context]
        public ListaPlac lp {get; set;}

        [Action("Podziel wypłaty", Mode = ActionMode.SingleSession | ActionMode.ConfirmSave | ActionMode.Progress,
            Target = ActionTarget.Menu | ActionTarget.ToolbarWithText | ActionTarget.LocalMenu)]
        
        public object PodzielWyplaty(){

            if (lp == null){
                throw new ArgumentNullException();
            }
            return new MessageBoxInformation() {
                Text = "Czy podzielić wypłaty?",
                YesHandler = () => {
                   
                    PodzielWyplatyWorker pww = new(lp);
                    pww.Podziel();
                   
                    return "Pomyślnie podzielono wypłaty";
                },
                NoHandler = () => "Operacja przerwana"
            };
        }
    }

    /*public class PodzielWyplatyWorkerParams : ContextBase
    {
        public PodzielWyplatyWorkerParams(Context context) : base(context)
        {
        }
    }
    */
}
