﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moneyes.UI.Services
{
    public interface IStatusMessageService
    {
        void ShowMessage(string messageText, string actionText = null, Action action = null);

        event Action<string, string?, Action?> NewMessage;
    }

    class StatusMessageService : IStatusMessageService
    {
        public event Action<string, string, Action> NewMessage;
        public void ShowMessage(string messageText, string actionText = null, Action action = null)
        {
            NewMessage?.Invoke(messageText, actionText, action);
        }
    }
}