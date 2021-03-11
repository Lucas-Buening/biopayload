// -------------------------------------------------------------
// FirmataRC - Version 1.0 Copyright richard.prinz@min.at 2013
// This code is under Creative Commons License V3.0
// See:
// http://creativecommons.org/licenses/by/3.0/
// http://creativecommons.org/licenses/by/3.0/at/
//
// You are allowed to use and modify this code (private and 
// commercial) as long as you reference the origin of it in
// any end user documentation, EULA's etc.

#region Usings
using System;
using System.Linq;
using System.Reflection;
using Sharpduino.Creators;
using Sharpduino.Handlers;
using Sharpduino.Messages;
using Sharpduino.Messages.Send;
using Sharpduino.SerialProviders;
using Sharpduino.Base;
using System.Text.RegularExpressions;
using System.Text;
using System.Collections;
#endregion

namespace RcControl.Base
{
    public abstract class FirmataRCBase : FirmataEmptyBase
    {
        #region Constants
        // look for message creators only in namespaces which have 
        // .*.Creators.* in their names
        private readonly Regex rxCreators = new Regex(@"^.*\.Creators(?:\.[^\.]+)*$",
                                                    RegexOptions.Singleline | RegexOptions.Compiled);

        // look for message handlers only in namespaces which have 
        // .*.Handlers.* in their names
        private readonly Regex rxHandlers = new Regex(@"^.*\.Handlers(?:\.[^\.]+)*$",
                                                    RegexOptions.Singleline | RegexOptions.Compiled);

        // look for message definitions only in namespaces which have 
        // .*.Messages.* in their names
        private readonly Regex rxMessages = new Regex(@"^.*\.Messages(?:\.[^\.]+)*$",
                                                    RegexOptions.Singleline | RegexOptions.Compiled);
        #endregion

        #region Variables
        private ArrayList a = new ArrayList(1024);
        #endregion

        #region Events
        #region DataReceived
        public delegate void DataReceivedEventHandler(object sender, EventArgs e);
        public event DataReceivedEventHandler DataReceived;
        protected virtual void OnDataReceived(EventArgs e)
        {
            if (this.DataReceived != null)
                DataReceived(this, e);
        }
        #endregion
        #endregion

        #region Ctor / Dtor
        protected FirmataRCBase(ISerialProvider serialProvider)
            : base(serialProvider)
        {
            AddMessageHandlers();
            AddMessageCreators();

            Provider.DataReceived +=
                new EventHandler<Sharpduino.EventArguments.DataReceivedEventArgs>(Provider_DataReceived);
        }
        #endregion

        #region Properties
        #region Data
        public byte[] Data
        {
            get { return a.ToArray(typeof(byte)) as byte[]; }
        }
        #endregion
        #endregion

        #region Event Handler
        #region Provider_DataReceived
        void Provider_DataReceived(object sender, Sharpduino.EventArguments.DataReceivedEventArgs e)
        {
            a.AddRange(e.BytesReceived.ToArray<byte>());
            OnDataReceived(new EventArgs());
        }
        #endregion
        #endregion

        #region Public Methods
        #region FlushData
        public void FlushData()
        {
            a.Clear();
        }
        #endregion
        #endregion

        #region Private Methods
        #region AddMessageHandlers
        private void AddMessageHandlers()
        {
            var messageHandlers = (from a in AppDomain.CurrentDomain.GetAssemblies()
                                   from t in a.GetTypes()
                                   where t.IsClass && !t.IsAbstract &&
                                         t.Namespace != null &&
                                         rxHandlers.IsMatch(t.Namespace) &&
                                         t.GetInterfaces().Any(x =>
                                             x == typeof(IMessageHandler))
                                   select t).ToList();

            // Create an instance for each type we found and add it to the AvailableHandlers
            messageHandlers.ForEach(t =>
                AvailableHandlers.Add((IMessageHandler)Activator.CreateInstance(t, MessageBroker)));
        }
        #endregion
        #region AddMessageCreators
        private void AddMessageCreators()
        {
            // try to find creators in all loaded assemblies
            var messageCreators = (from a in AppDomain.CurrentDomain.GetAssemblies()
                                   from t in a.GetTypes()
                                   where t.IsClass &&
                                         !t.IsAbstract &&
                                         t.Namespace != null &&
                                         rxCreators.IsMatch(t.Namespace) &&
                                         t.BaseType.GetGenericArguments()[0] != typeof(StaticMessage) &&
                                         t.GetInterfaces().Any(x =>
                                            x.GetGenericTypeDefinition() == typeof(IMessageCreator<>))
                                   select t).ToList();

            // Create an instance for each type we found and add it to the MessageCreators with 
            // the Message Type that it creates as a key
            messageCreators.ForEach(t =>
                MessageCreators[t.BaseType.GetGenericArguments()[0]] = (IMessageCreator)Activator.CreateInstance(t));

            // This is the special case for the static message creator
            StaticMessageCreator staticMessageCreator = new StaticMessageCreator();

            // try to find StaticMessage creators in all loaded assemblies
            var staticMessages = (from a in AppDomain.CurrentDomain.GetAssemblies()
                                  from t in a.GetTypes()
                                  where t.IsClass &&
                                        t.BaseType == typeof(StaticMessage) &&
                                        t.Namespace != null &&
                                        rxMessages.IsMatch(t.Namespace)
                                  select t).ToList();

            // Add them to the MessageCreators dictionary
            staticMessages.ForEach(t =>
                MessageCreators[t] = staticMessageCreator);
        }
        #endregion
        #endregion
    }
}
