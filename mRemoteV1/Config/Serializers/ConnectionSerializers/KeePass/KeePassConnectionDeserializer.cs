using mRemoteNG.App;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol;
using mRemoteNG.Connection.Protocol.Http;
using mRemoteNG.Connection.Protocol.ICA;
using mRemoteNG.Connection.Protocol.RDP;
using mRemoteNG.Connection.Protocol.VNC;
using mRemoteNG.Container;
using mRemoteNG.Messages;
using mRemoteNG.Security;
using mRemoteNG.Tools;
using mRemoteNG.Tree;
using mRemoteNG.Tree.Root;
using mRemoteNG.UI.Forms;
using mRemoteNG.UI.TaskDialog;
using System;
using System.Globalization;
using System.Security;
using System.Windows.Forms;
using System.Xml;
using mRemoteNG.Config.Serializers.ConnectionSerializers.Xml;
using mRemoteNG.Credential.KeePass;
using System.Collections.Generic;

namespace mRemoteNG.Config.Serializers.Xml
{
    public class KeePassConnectionDeserializer : IDeserializer<List<KeePassEntry>, ConnectionTreeModel>
    {
        private string ConnectionFileName = "";

        private readonly RootNodeInfo _rootNodeInfo = new RootNodeInfo(RootNodeType.Connection);

      
        public KeePassConnectionDeserializer() { }

        public ConnectionTreeModel Deserialize(List<KeePassEntry> keePassList)
        {
            try
            {
                var connectionTreeModel = new ConnectionTreeModel();
                connectionTreeModel.AddRootNode(_rootNodeInfo);
                ContainerInfo containerInfo = _rootNodeInfo;
                KeePassEntry prevKeePassEntry = null;
                foreach (KeePassEntry currKeePassEntry in keePassList)
                {
                    if (prevKeePassEntry != null)
                    {
                        if (!prevKeePassEntry.ParentGroup.Equals(currKeePassEntry.ParentGroup))
                        {
                            containerInfo = new ContainerInfo();
                            containerInfo.Name = currKeePassEntry.ParentGroup;
                            _rootNodeInfo.AddChild(containerInfo);
                        }                       
                    }else
                    {
                        containerInfo = new ContainerInfo();
                        containerInfo.Name = currKeePassEntry.ParentGroup;
                        _rootNodeInfo.AddChild(containerInfo);
                    }
                    ConnectionInfo connection = GetConnectionFromKeePassEntry(currKeePassEntry);
                    containerInfo.AddChild(connection);
                    prevKeePassEntry = currKeePassEntry;

                }

                return connectionTreeModel;
            }
            catch (Exception ex)
            {
                Runtime.ConnectionsService.IsConnectionsFileLoaded = false;
                Runtime.MessageCollector.AddExceptionStackTrace(Language.strLoadFromXmlFailed, ex);
                throw;
            }
        }


        private ConnectionInfo GetConnectionFromKeePassEntry(KeePassEntry keePassEntry)
        {
            var connectionId = keePassEntry.Uuid;
            var connectionInfo = new ConnectionInfo(connectionId);

            try
            {
                connectionInfo.Name = keePassEntry.Name;
                connectionInfo.Description = "";
                connectionInfo.Hostname = keePassEntry.StringFields["mRemote:Host"];
                connectionInfo.Protocol = (ProtocolType) Enum.Parse(typeof(ProtocolType), keePassEntry.StringFields["mRemote:Protocol"]);
                connectionInfo.Port = Int16.Parse(keePassEntry.StringFields["mRemote:Port"]);
                //connectionInfo.DisplayWallpaper = xmlnode.GetAttributeAsBool("DisplayWallpaper");
                //connectionInfo.DisplayThemes = xmlnode.GetAttributeAsBool("DisplayThemes");
                //connectionInfo.CacheBitmaps = xmlnode.GetAttributeAsBool("CacheBitmaps");

                //if (_confVersion < 1.1) //1.0 - 0.1
                //{
                //    connectionInfo.Resolution = xmlnode.GetAttributeAsBool("Fullscreen")
                //        ? RDPResolutions.Fullscreen
                //        : RDPResolutions.FitToWindow;
                //}
                /*
                if (!Runtime.UseCredentialManager || _confVersion <= 2.6) // 0.2 - 2.6
                {
#pragma warning disable 618
                    connectionInfo.Username = xmlnode.GetAttributeAsString("Username");
                    connectionInfo.Password = _decryptor.Decrypt(xmlnode.GetAttributeAsString("Password"));
                    connectionInfo.Domain = xmlnode.GetAttributeAsString("Domain");
#pragma warning restore 618
                }
            }

            if (_confVersion >= 0.3)
            {
                if (_confVersion < 0.7)
                {
                    if (xmlnode.GetAttributeAsBool("UseVNC"))
                    {
                        connectionInfo.Protocol = ProtocolType.VNC;
                        connectionInfo.Port = xmlnode.GetAttributeAsInt("VNCPort");
                    }
                    else
                    {
                        connectionInfo.Protocol = ProtocolType.RDP;
                    }
                }
            }
            else
            {
                connectionInfo.Port = (int)RdpProtocol6.Defaults.Port;
                connectionInfo.Protocol = ProtocolType.RDP;
            }

            if (_confVersion >= 0.4)
            {
                if (_confVersion < 0.7)
                {
                    connectionInfo.Port = xmlnode.GetAttributeAsBool("UseVNC")
                        ? xmlnode.GetAttributeAsInt("VNCPort")
                        : xmlnode.GetAttributeAsInt("RDPPort");
                }

                connectionInfo.UseConsoleSession = xmlnode.GetAttributeAsBool("ConnectToConsole");
            }
            else
            {
                if (_confVersion < 0.7)
                {
                    if (xmlnode.GetAttributeAsBool("UseVNC"))
                        connectionInfo.Port = (int)ProtocolVNC.Defaults.Port;
                    else
                        connectionInfo.Port = (int)RdpProtocol6.Defaults.Port;
                }

                connectionInfo.UseConsoleSession = false;
            }

            if (_confVersion >= 0.5)
            {
                connectionInfo.RedirectDiskDrives = xmlnode.GetAttributeAsBool("RedirectDiskDrives");
                connectionInfo.RedirectPrinters = xmlnode.GetAttributeAsBool("RedirectPrinters");
                connectionInfo.RedirectPorts = xmlnode.GetAttributeAsBool("RedirectPorts");
                connectionInfo.RedirectSmartCards = xmlnode.GetAttributeAsBool("RedirectSmartCards");
            }
            else
            {
                connectionInfo.RedirectDiskDrives = false;
                connectionInfo.RedirectPrinters = false;
                connectionInfo.RedirectPorts = false;
                connectionInfo.RedirectSmartCards = false;
            }

            if (_confVersion >= 0.7)
            {
                connectionInfo.Protocol = xmlnode.GetAttributeAsEnum<ProtocolType>("Protocol");
                connectionInfo.Port = xmlnode.GetAttributeAsInt("Port");
            }

            if (_confVersion >= 1.0)
            {
                connectionInfo.RedirectKeys = xmlnode.GetAttributeAsBool("RedirectKeys");
            }

            if (_confVersion >= 1.2)
            {
                connectionInfo.PuttySession = xmlnode.GetAttributeAsString("PuttySession");
            }

            if (_confVersion >= 1.3)
            {
                connectionInfo.Colors = xmlnode.GetAttributeAsEnum<RDPColors>("Colors");
                connectionInfo.Resolution = xmlnode.GetAttributeAsEnum<RDPResolutions>("Resolution");
                connectionInfo.RedirectSound = xmlnode.GetAttributeAsEnum<RDPSounds>("RedirectSound");
                connectionInfo.RedirectAudioCapture = xmlnode.GetAttributeAsBool("RedirectAudioCapture");
            }
            else
            {
                switch (xmlnode.GetAttributeAsInt("Colors"))
                {
                    case 0:
                        connectionInfo.Colors = RDPColors.Colors256;
                        break;
                    case 1:
                        connectionInfo.Colors = RDPColors.Colors16Bit;
                        break;
                    case 2:
                        connectionInfo.Colors = RDPColors.Colors24Bit;
                        break;
                    case 3:
                        connectionInfo.Colors = RDPColors.Colors32Bit;
                        break;
                    // ReSharper disable once RedundantCaseLabel
                    case 4:
                    default:
                        connectionInfo.Colors = RDPColors.Colors15Bit;
                        break;
                }

                connectionInfo.RedirectSound = xmlnode.GetAttributeAsEnum<RDPSounds>("RedirectSound");
                connectionInfo.RedirectAudioCapture = xmlnode.GetAttributeAsBool("RedirectAudioCapture");
            }

            if (_confVersion >= 1.3)
            {
                connectionInfo.Inheritance.CacheBitmaps = xmlnode.GetAttributeAsBool("InheritCacheBitmaps");
                connectionInfo.Inheritance.Colors = xmlnode.GetAttributeAsBool("InheritColors");
                connectionInfo.Inheritance.Description = xmlnode.GetAttributeAsBool("InheritDescription");
                connectionInfo.Inheritance.DisplayThemes = xmlnode.GetAttributeAsBool("InheritDisplayThemes");
                connectionInfo.Inheritance.DisplayWallpaper = xmlnode.GetAttributeAsBool("InheritDisplayWallpaper");
                connectionInfo.Inheritance.Icon = xmlnode.GetAttributeAsBool("InheritIcon");
                connectionInfo.Inheritance.Panel = xmlnode.GetAttributeAsBool("InheritPanel");
                connectionInfo.Inheritance.Port = xmlnode.GetAttributeAsBool("InheritPort");
                connectionInfo.Inheritance.Protocol = xmlnode.GetAttributeAsBool("InheritProtocol");
                connectionInfo.Inheritance.PuttySession = xmlnode.GetAttributeAsBool("InheritPuttySession");
                connectionInfo.Inheritance.RedirectDiskDrives =
                    xmlnode.GetAttributeAsBool("InheritRedirectDiskDrives");
                connectionInfo.Inheritance.RedirectKeys = xmlnode.GetAttributeAsBool("InheritRedirectKeys");
                connectionInfo.Inheritance.RedirectPorts = xmlnode.GetAttributeAsBool("InheritRedirectPorts");
                connectionInfo.Inheritance.RedirectPrinters = xmlnode.GetAttributeAsBool("InheritRedirectPrinters");
                connectionInfo.Inheritance.RedirectSmartCards =
                    xmlnode.GetAttributeAsBool("InheritRedirectSmartCards");
                connectionInfo.Inheritance.RedirectSound = xmlnode.GetAttributeAsBool("InheritRedirectSound");
                connectionInfo.Inheritance.RedirectAudioCapture = xmlnode.GetAttributeAsBool("RedirectAudioCapture");
                connectionInfo.Inheritance.Resolution = xmlnode.GetAttributeAsBool("InheritResolution");
                connectionInfo.Inheritance.UseConsoleSession =
                    xmlnode.GetAttributeAsBool("InheritUseConsoleSession");

                if (!Runtime.UseCredentialManager || _confVersion <= 2.6) // 1.3 - 2.6
                {
                    connectionInfo.Inheritance.Domain = xmlnode.GetAttributeAsBool("InheritDomain");
                    connectionInfo.Inheritance.Password = xmlnode.GetAttributeAsBool("InheritPassword");
                    connectionInfo.Inheritance.Username = xmlnode.GetAttributeAsBool("InheritUsername");
                }

                connectionInfo.Icon = xmlnode.GetAttributeAsString("Icon");
                connectionInfo.Panel = xmlnode.GetAttributeAsString("Panel");
            }
            else
            {
                if (xmlnode.GetAttributeAsBool("Inherit"))
                    connectionInfo.Inheritance.TurnOnInheritanceCompletely();
                connectionInfo.Icon = xmlnode.GetAttributeAsString("Icon").Replace(".ico", "");
                connectionInfo.Panel = Language.strGeneral;
            }

            if (_confVersion >= 1.5)
            {
                connectionInfo.PleaseConnect = xmlnode.GetAttributeAsBool("Connected");
            }

            if (_confVersion >= 1.6)
            {
                connectionInfo.ICAEncryptionStrength =
                    xmlnode.GetAttributeAsEnum<IcaProtocol.EncryptionStrength>("ICAEncryptionStrength");
                connectionInfo.Inheritance.ICAEncryptionStrength =
                    xmlnode.GetAttributeAsBool("InheritICAEncryptionStrength");
                connectionInfo.PreExtApp = xmlnode.GetAttributeAsString("PreExtApp");
                connectionInfo.PostExtApp = xmlnode.GetAttributeAsString("PostExtApp");
                connectionInfo.Inheritance.PreExtApp = xmlnode.GetAttributeAsBool("InheritPreExtApp");
                connectionInfo.Inheritance.PostExtApp = xmlnode.GetAttributeAsBool("InheritPostExtApp");
            }

            if (_confVersion >= 1.7)
            {
                connectionInfo.VNCCompression =
                    xmlnode.GetAttributeAsEnum<ProtocolVNC.Compression>("VNCCompression");
                connectionInfo.VNCEncoding = xmlnode.GetAttributeAsEnum<ProtocolVNC.Encoding>("VNCEncoding");
                connectionInfo.VNCAuthMode = xmlnode.GetAttributeAsEnum<ProtocolVNC.AuthMode>("VNCAuthMode");
                connectionInfo.VNCProxyType = xmlnode.GetAttributeAsEnum<ProtocolVNC.ProxyType>("VNCProxyType");
                connectionInfo.VNCProxyIP = xmlnode.GetAttributeAsString("VNCProxyIP");
                connectionInfo.VNCProxyPort = xmlnode.GetAttributeAsInt("VNCProxyPort");
                connectionInfo.VNCProxyUsername = xmlnode.GetAttributeAsString("VNCProxyUsername");
                connectionInfo.VNCProxyPassword =
                    _decryptor.Decrypt(xmlnode.GetAttributeAsString("VNCProxyPassword"));
                connectionInfo.VNCColors = xmlnode.GetAttributeAsEnum<ProtocolVNC.Colors>("VNCColors");
                connectionInfo.VNCSmartSizeMode =
                    xmlnode.GetAttributeAsEnum<ProtocolVNC.SmartSizeMode>("VNCSmartSizeMode");
                connectionInfo.VNCViewOnly = xmlnode.GetAttributeAsBool("VNCViewOnly");
                connectionInfo.Inheritance.VNCCompression = xmlnode.GetAttributeAsBool("InheritVNCCompression");
                connectionInfo.Inheritance.VNCEncoding = xmlnode.GetAttributeAsBool("InheritVNCEncoding");
                connectionInfo.Inheritance.VNCAuthMode = xmlnode.GetAttributeAsBool("InheritVNCAuthMode");
                connectionInfo.Inheritance.VNCProxyType = xmlnode.GetAttributeAsBool("InheritVNCProxyType");
                connectionInfo.Inheritance.VNCProxyIP = xmlnode.GetAttributeAsBool("InheritVNCProxyIP");
                connectionInfo.Inheritance.VNCProxyPort = xmlnode.GetAttributeAsBool("InheritVNCProxyPort");
                connectionInfo.Inheritance.VNCProxyUsername = xmlnode.GetAttributeAsBool("InheritVNCProxyUsername");
                connectionInfo.Inheritance.VNCProxyPassword = xmlnode.GetAttributeAsBool("InheritVNCProxyPassword");
                connectionInfo.Inheritance.VNCColors = xmlnode.GetAttributeAsBool("InheritVNCColors");
                connectionInfo.Inheritance.VNCSmartSizeMode = xmlnode.GetAttributeAsBool("InheritVNCSmartSizeMode");
                connectionInfo.Inheritance.VNCViewOnly = xmlnode.GetAttributeAsBool("InheritVNCViewOnly");
            }

            if (_confVersion >= 1.8)
            {
                connectionInfo.RDPAuthenticationLevel =
                    xmlnode.GetAttributeAsEnum<AuthenticationLevel>("RDPAuthenticationLevel");
                connectionInfo.Inheritance.RDPAuthenticationLevel =
                    xmlnode.GetAttributeAsBool("InheritRDPAuthenticationLevel");
            }

            if (_confVersion >= 1.9)
            {
                connectionInfo.RenderingEngine =
                    xmlnode.GetAttributeAsEnum<HTTPBase.RenderingEngine>("RenderingEngine");
                connectionInfo.MacAddress = xmlnode.GetAttributeAsString("MacAddress");
                connectionInfo.Inheritance.RenderingEngine = xmlnode.GetAttributeAsBool("InheritRenderingEngine");
                connectionInfo.Inheritance.MacAddress = xmlnode.GetAttributeAsBool("InheritMacAddress");
            }

            if (_confVersion >= 2.0)
            {
                connectionInfo.UserField = xmlnode.GetAttributeAsString("UserField");
                connectionInfo.Inheritance.UserField = xmlnode.GetAttributeAsBool("InheritUserField");
            }

            if (_confVersion >= 2.1)
            {
                connectionInfo.ExtApp = xmlnode.GetAttributeAsString("ExtApp");
                connectionInfo.Inheritance.ExtApp = xmlnode.GetAttributeAsBool("InheritExtApp");
            }

            if (_confVersion >= 2.2)
            {
                // Get settings
                connectionInfo.RDGatewayUsageMethod =
                    xmlnode.GetAttributeAsEnum<RDGatewayUsageMethod>("RDGatewayUsageMethod");
                connectionInfo.RDGatewayHostname = xmlnode.GetAttributeAsString("RDGatewayHostname");
                connectionInfo.RDGatewayUseConnectionCredentials =
                    xmlnode.GetAttributeAsEnum<RDGatewayUseConnectionCredentials>(
                                                                                              "RDGatewayUseConnectionCredentials");
                connectionInfo.RDGatewayUsername = xmlnode.GetAttributeAsString("RDGatewayUsername");
                connectionInfo.RDGatewayPassword =
                    _decryptor.Decrypt(xmlnode.GetAttributeAsString("RDGatewayPassword"));
                connectionInfo.RDGatewayDomain = xmlnode.GetAttributeAsString("RDGatewayDomain");

                // Get inheritance settings
                connectionInfo.Inheritance.RDGatewayUsageMethod =
                    xmlnode.GetAttributeAsBool("InheritRDGatewayUsageMethod");
                connectionInfo.Inheritance.RDGatewayHostname =
                    xmlnode.GetAttributeAsBool("InheritRDGatewayHostname");
                connectionInfo.Inheritance.RDGatewayUseConnectionCredentials =
                    xmlnode.GetAttributeAsBool("InheritRDGatewayUseConnectionCredentials");
                connectionInfo.Inheritance.RDGatewayUsername =
                    xmlnode.GetAttributeAsBool("InheritRDGatewayUsername");
                connectionInfo.Inheritance.RDGatewayPassword =
                    xmlnode.GetAttributeAsBool("InheritRDGatewayPassword");
                connectionInfo.Inheritance.RDGatewayDomain = xmlnode.GetAttributeAsBool("InheritRDGatewayDomain");
            }

            if (_confVersion >= 2.3)
            {
                // Get settings
                connectionInfo.EnableFontSmoothing = xmlnode.GetAttributeAsBool("EnableFontSmoothing");
                connectionInfo.EnableDesktopComposition = xmlnode.GetAttributeAsBool("EnableDesktopComposition");

                // Get inheritance settings
                connectionInfo.Inheritance.EnableFontSmoothing =
                    xmlnode.GetAttributeAsBool("InheritEnableFontSmoothing");
                connectionInfo.Inheritance.EnableDesktopComposition =
                    xmlnode.GetAttributeAsBool("InheritEnableDesktopComposition");
            }

            if (_confVersion >= 2.4)
            {
                connectionInfo.UseCredSsp = xmlnode.GetAttributeAsBool("UseCredSsp");
                connectionInfo.Inheritance.UseCredSsp = xmlnode.GetAttributeAsBool("InheritUseCredSsp");
            }

            if (_confVersion >= 2.5)
            {
                connectionInfo.LoadBalanceInfo = xmlnode.GetAttributeAsString("LoadBalanceInfo");
                connectionInfo.AutomaticResize = xmlnode.GetAttributeAsBool("AutomaticResize");
                connectionInfo.Inheritance.LoadBalanceInfo = xmlnode.GetAttributeAsBool("InheritLoadBalanceInfo");
                connectionInfo.Inheritance.AutomaticResize = xmlnode.GetAttributeAsBool("InheritAutomaticResize");
            }

            if (_confVersion >= 2.6)
            {
                connectionInfo.SoundQuality =
                    xmlnode.GetAttributeAsEnum<RDPSoundQuality>("SoundQuality");
                connectionInfo.Inheritance.SoundQuality = xmlnode.GetAttributeAsBool("InheritSoundQuality");
                connectionInfo.RDPMinutesToIdleTimeout = xmlnode.GetAttributeAsInt("RDPMinutesToIdleTimeout");
                connectionInfo.Inheritance.RDPMinutesToIdleTimeout =
                    xmlnode.GetAttributeAsBool("InheritRDPMinutesToIdleTimeout");
                connectionInfo.RDPAlertIdleTimeout = xmlnode.GetAttributeAsBool("RDPAlertIdleTimeout");
                connectionInfo.Inheritance.RDPAlertIdleTimeout =
                    xmlnode.GetAttributeAsBool("InheritRDPAlertIdleTimeout");
            }

            if (_confVersion >= 2.7)
            {
                connectionInfo.RedirectClipboard = xmlnode.GetAttributeAsBool("RedirectClipboard");
                connectionInfo.Favorite = xmlnode.GetAttributeAsBool("Favorite");
                connectionInfo.UseVmId = xmlnode.GetAttributeAsBool("UseVmId");
                connectionInfo.VmId = xmlnode.GetAttributeAsString("VmId");
                connectionInfo.UseEnhancedMode = xmlnode.GetAttributeAsBool("UseEnhancedMode");
                connectionInfo.Inheritance.RedirectClipboard = xmlnode.GetAttributeAsBool("InheritRedirectClipboard");
                connectionInfo.Inheritance.Favorite = xmlnode.GetAttributeAsBool("InheritFavorite");
                connectionInfo.RdpVersion = xmlnode.GetAttributeAsEnum("RdpVersion", RdpVersion.Highest);
                connectionInfo.Inheritance.RdpVersion = xmlnode.GetAttributeAsBool("InheritRdpVersion");
                connectionInfo.Inheritance.UseVmId = xmlnode.GetAttributeAsBool("InheritUseVmId");
                connectionInfo.Inheritance.VmId = xmlnode.GetAttributeAsBool("InheritVmId");
                connectionInfo.Inheritance.UseEnhancedMode = xmlnode.GetAttributeAsBool("InheritUseEnhancedMode");
            }*/
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddMessage(MessageClass.ErrorMsg,
                    string.Format(Language.strGetConnectionInfoFromXmlFailed,
                        connectionInfo.Name, ConnectionFileName, ex.Message));
            }
            
            return connectionInfo;
        }
    }
}