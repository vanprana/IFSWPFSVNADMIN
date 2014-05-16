using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Lync.Model.Extensibility;
using Microsoft.Lync.Model;

namespace IfsSvnClient.Classes
{
    internal class NotifierLync
    {
        private Automation _Automation;
        private List<string> inviteeList;
        private Dictionary<AutomationModalitySettings, object> _ModalitySettings;
        private AutomationModalities _ChosenMode;

        private readonly string subject = "IFS SVN Client";

        internal NotifierLync()
        {
            try
            {
                _Automation = LyncClient.GetAutomation();

                // Create a generic Dictionary object to contain conversation setting objects.
                _ModalitySettings = new Dictionary<AutomationModalitySettings, object>();
                _ModalitySettings.Add(AutomationModalitySettings.Subject, subject);
                _ModalitySettings.Add(AutomationModalitySettings.SendFirstInstantMessageImmediately, true);

                _ChosenMode = AutomationModalities.InstantMessage;
            }
            catch (Exception)
            {
                throw;
            }
        }

        internal void SendMessage(string imText)
        {
            this.SendMessage(Properties.Settings.Default.SupportPerson, imText);
        }

        internal void SendMessage(string inviteeEmail, string imText)
        {
            try
            {
                // Create a generic List object to contain a contact URI.
                // Ensure that a valid URI is added.
                inviteeList = new List<string>();
                inviteeList.Add(inviteeEmail);

                imText = subject + "! : " + imText;

                if (_ModalitySettings.ContainsKey(AutomationModalitySettings.FirstInstantMessage))
                {
                    _ModalitySettings[AutomationModalitySettings.FirstInstantMessage] = imText;
                }
                else
                {
                    _ModalitySettings.Add(AutomationModalitySettings.FirstInstantMessage, imText);
                }
                // Start the conversation.
                IAsyncResult ar = _Automation.BeginStartConversation(_ChosenMode,
                                                                     inviteeList,
                                                                     _ModalitySettings,
                                                                     null,
                                                                     null);


                //Block UI thread until conversation is started
                _Automation.EndStartConversation(ar);
            }
            catch (Exception)
            {

            }
        }
    }
}
