﻿using System.Xml.Serialization;

namespace Shoko.Server.Providers.JMMAutoUpdates
{
    [XmlType(AnonymousType = true)]
    public class Versions
    {
        /// <remarks/>
        public string serverversion { get; set; }

        /// <remarks/>
        public string desktopversion { get; set; }

        public override string ToString()
        {
            return string.Format("Server: {0} --- Desktop: {1}", serverversion, desktopversion);
        }

        public long ServerVersionAbs
        {
            get { return JMMAutoUpdatesHelper.ConvertToAbsoluteVersion(serverversion); }
        }

        public string ServerVersionFriendly
        {
            get { return serverversion; }
        }

        public long DesktopVersionAbs
        {
            get { return JMMAutoUpdatesHelper.ConvertToAbsoluteVersion(desktopversion); }
        }
    }
}