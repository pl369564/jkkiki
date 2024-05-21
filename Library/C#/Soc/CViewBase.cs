using strange.extensions.context.api;
using strange.extensions.mediation.impl;
using strange.extensions.dispatcher.eventdispatcher.impl;
using strange.extensions.context.impl;
using UnityEngine;
using System;
using StrangeIoC;
using strange.extensions.mediation.api;

namespace FastDance.Views
{
    public class CViewBase : View
    {
        bool IsBindSelfMediator;

        /// <summary>
        /// 自动绑定到同名Mediator
        /// </summary>
        protected override void bubbleToContext(MonoBehaviour view, bool toAdd, bool finalTry)
        {
            if (Context.firstContext != null)
            {
                AutoBindToSelfMediator(view);
                CViewBase[] cviews = view.GetComponentsInChildren<CViewBase>(true);
                int aa = cviews.Length;
                for (int a = aa - 1; a > -1; a--)
                {
                    CViewBase item = cviews[a];
                    item.AutoBindToSelfMediator(item);
                }
            }
            base.bubbleToContext(view, toAdd, finalTry);
        }
        public void AutoBindToSelfMediator(MonoBehaviour view)
        {
            if (IsBindSelfMediator)
                return;

            Type viewType = view.GetType();
            if (viewType.Name.EndsWith("View"))
            {
                var mvcs = Context.firstContext as MVCSContext;
                if (mvcs.mediationBinder.GetBinding(viewType) != null)
                    return;
                string mediatorName = viewType.Name.Substring(0, viewType.Name.LastIndexOf("View")) + "Mediator";
                var myMediatorType = Type.GetType("FastDance.Mediators." + mediatorName);
                if (myMediatorType == null)
                    throw new MediationException(mediatorName + ":The MediatorType is NotFound", MediationExceptionType.IMPLICIT_BINDING_VIEW_TYPE_IS_NULL);
                mvcs.mediationBinder.Bind(viewType).To(myMediatorType);
                IsBindSelfMediator = true;
            }
        }

        public void BubbleToContextFinal()
        {
            if (autoRegisterWithContext && !registeredWithContext)
                base.bubbleToContext(this, true, true);
        }
    }
}
